using Chic.Abstractions;
using Chic.Constraints;
using Coda.Data.Sql;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Cedita.Labs.Jrnal.Db
{
    public class ChildAotInsertionService
    {
        private readonly IDbConnection db;
        private readonly IServiceProvider services;
        public ChildAotInsertionService(IServiceProvider services, IDbConnection db)
        {
            this.db = db;
            this.services = services;
        }

        public async Task InsertWithChildrenAsync<TModel>(IEnumerable<TModel> models, bool withMap = true, int tmpLevel = 0, SqlTransaction txn = null)
            where TModel : IKeyedEntity, IHaveAotMarker
        {
            if (withMap)
            {
                Console.WriteLine($"{DateTime.Now} AOT Marker Creation");
                curAot = 0;
                ConfigureAotMarkers(models);
                curAot = 0;
                Console.WriteLine($"{DateTime.Now} AOT Marker Creation Completed");
            }

            Console.WriteLine($"{DateTime.Now} Commit level {tmpLevel} executing ({typeof(TModel)}) w/ {models.Count()} models");

            if (typeof(TModel) == typeof(Db.Models.SingleRendering))
            {
                var repo = services.GetRequiredService<IRepository<Models.SingleRendering>>();
                await repo.InsertManyAsync((IEnumerable<Cedita.Labs.Jrnal.Db.Models.SingleRendering>)models);
                return;
            }

            if (db.State != ConnectionState.Open)
            {
                db.Open();
            }

            if (txn == null)
            {
                txn = db.BeginTransaction() as SqlTransaction;
            }
            try
            {
                if (typeof(TModel) == typeof(Db.Models.Event))
                {
                    await db.ExecuteAsync($@"CREATE TABLE #Tmp{tmpLevel} ([Id] INT NULL, [ApplicationId] INT NOT NULL,
    [Timestamp] DATETIMEOFFSET NOT NULL,
    [Level] NVARCHAR(20) NOT NULL,
    [MessageTemplate] NVARCHAR(MAX) NOT NULL,
    [HasException] BIT,
    [AotInsertionMarker] INT)", transaction: txn);

                    using (var sqlBulkCopy = new SqlBulkCopier<TModel>((SqlConnection)db, $"#Tmp{tmpLevel}", false, txn))
                    {
                        foreach (var row in models)
                        {
                            sqlBulkCopy.AddRow(row);
                        }

                        await sqlBulkCopy.WriteToServerAsync();
                    }

                    var returns = await db.QueryAsync<(int, int)>($@"INSERT INTO [Events] (ApplicationId, Timestamp, Level, MessageTemplate, HasException, AotInsertionMarker)
OUTPUT INSERTED.Id, INSERTED.AotInsertionMarker
SELECT ApplicationId, Timestamp, Level, MessageTemplate, HasException, AotInsertionMarker FROM #Tmp{tmpLevel}", transaction: txn);
                    var dictReturns = returns.ToDictionary(m => m.Item2, m => m.Item1);

                    foreach (var model in models)
                    {
                        var dbEvent = model as Models.Event;
                        foreach (var child in dbEvent.Properties) child.EventId = dictReturns[dbEvent.AotInsertionMarker];
                        foreach (var child in dbEvent.RenderingGroups) child.EventId = dictReturns[dbEvent.AotInsertionMarker];
                    }

                    var obj = (IEnumerable<Models.Event>)(models);
                    await InsertWithChildrenAsync(obj.SelectMany(m => m.Properties), false, 1+tmpLevel, txn);
                }
                else if (typeof(TModel) == typeof(Db.Models.Property))
                {
                    await db.ExecuteAsync($@"CREATE TABLE #Tmp{tmpLevel} ([Id] INT, [EventId] INT,
		[ParentPropertyId] INT NULL,
		[Name] NVARCHAR(100),
		[Value] NVARCHAR(MAX) NULL,
		[AotInsertionMarker] INT, [AotParentMarker] INT)", transaction: txn);

                    using (var sqlBulkCopy = new SqlBulkCopier<TModel>((SqlConnection)db, $"#Tmp{tmpLevel}", false, txn))
                    {
                        foreach (var row in models)
                        {
                            sqlBulkCopy.AddRow(row);
                        }

                        await sqlBulkCopy.WriteToServerAsync();
                    }

                    var returns = await db.QueryAsync<(int, int)>($@"INSERT INTO [Properties] (EventId, ParentPropertyId, Name, Value, AotInsertionMarker)
OUTPUT INSERTED.Id, INSERTED.AotInsertionMarker
SELECT EventId, ParentPropertyId, Name, Value, AotInsertionMarker FROM #Tmp{tmpLevel}", transaction: txn);
                    var dictReturns = returns.ToDictionary(m => m.Item2, m => m.Item1);

                    foreach (var model in models)
                    {
                        var dbProperty = model as Models.Property;
                        foreach (var child in dbProperty.Children)
                        {
                            child.EventId = dbProperty.EventId;
                            child.ParentPropertyId = dictReturns[dbProperty.AotInsertionMarker];
                        }
                    }

                    var obj = (IEnumerable<Models.Property>)(models);
                    var children = obj.SelectMany(m => m.Children);
                    if (children.Any())
                    {
                        await InsertWithChildrenAsync(obj.SelectMany(m => m.Children), false, 1+tmpLevel, txn);
                    }
                }
                else if (typeof(TModel) == typeof(Db.Models.RenderingGroup))
                {
                    await db.ExecuteAsync($@"CREATE TABLE #Tmp{tmpLevel} ([Id] INT NULL, [EventId] INT,
		[Name] NVARCHAR(100) NOT NULL,
		[AotInsertionMarker] INT, [AotParentMarker] INT NULL)", transaction: txn);

                    using (var sqlBulkCopy = new SqlBulkCopier<TModel>((SqlConnection)db, $"#Tmp{tmpLevel}", false, txn))
                    {
                        foreach (var row in models)
                        {
                            sqlBulkCopy.AddRow(row);
                        }

                        await sqlBulkCopy.WriteToServerAsync();
                    }

                    var returns = await db.QueryAsync<(int, int)>($@"INSERT INTO [RenderingGroups] (EventId, Name, AotInsertionMarker)
OUTPUT INSERTED.Id, INSERTED.AotInsertionMarker
SELECT EventId, Name, AotInsertionMarker FROM #Tmp{tmpLevel}", transaction: txn);
                    var dictReturns = returns.ToDictionary(m => m.Item2, m => m.Item1);

                    foreach (var model in models)
                    {
                        var dbRenderingGroup = model as Models.RenderingGroup;
                        foreach (var child in dbRenderingGroup.Renderings) child.RenderingGroupId = dictReturns[dbRenderingGroup.AotInsertionMarker];
                    }

                    var obj = (IEnumerable<Models.Property>)(models);
                    await InsertWithChildrenAsync(obj.SelectMany(m => m.Children), false, 1+tmpLevel, txn);
                }

                await db.ExecuteAsync($"DROP TABLE IF EXISTS #Tmp{tmpLevel}", transaction: txn);

                if (tmpLevel == 0)
                {
                    Console.WriteLine($"{DateTime.Now} Commit level 0 transaction completion");
                    txn.Commit();
                    Console.WriteLine($"{DateTime.Now} DB Transaction committed");
                }
            } catch
            {
                if (tmpLevel != 0)
                    throw;

                txn.Rollback();
            }
            Console.WriteLine($"{DateTime.Now} Commit level {tmpLevel} executed");


            if (tmpLevel == 0)
            {
                txn.Dispose();
            }
        }

        private int curAot = 0;
        private void ConfigureAotMarkers<TModel>(IEnumerable<TModel> models)
            where TModel : IKeyedEntity, IHaveAotMarker
        {
            foreach (var topLevelModel in models)
            {
                // For each child, we set the AotParentMarker to the current AotInsertionMarker
                if (topLevelModel is Db.Models.Event)
                {
                    var dbEvent = topLevelModel as Db.Models.Event;
                    foreach (var child in dbEvent.Properties)
                    {
                        child.AotParentMarker = curAot;
                    }
                    foreach(var child in dbEvent.RenderingGroups)
                    {
                        child.AotParentMarker = curAot;
                    }
                    ConfigureAotMarkers(dbEvent.Properties);
                    ConfigureAotMarkers(dbEvent.RenderingGroups);
                } else if (topLevelModel is Db.Models.Property)
                {
                    var dbProperty = topLevelModel as Db.Models.Property;
                    foreach (var child in dbProperty.Children)
                    {
                        child.AotParentMarker = curAot;
                    }
                    ConfigureAotMarkers((topLevelModel as Db.Models.Property).Children);
                } else if (topLevelModel is Db.Models.RenderingGroup)
                {
                    var dbRenderingGroup = topLevelModel as Db.Models.RenderingGroup;
                    foreach (var child in dbRenderingGroup.Renderings)
                    {
                        child.AotParentMarker = curAot;
                    }
                }

                topLevelModel.AotInsertionMarker = curAot++;
            }
        }
    }
}
