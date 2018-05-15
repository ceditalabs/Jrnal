using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Cedita.Labs.Jrnal.Db;
using Chic;
using Chic.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cedita.Labs.Jrnal
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Database Connection
            services.AddTransient<IDbConnection>(db => new SqlConnection(Configuration.GetConnectionString("Default")));
            // Chic Generic Repositories
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<IDatabaseProvisioner, SqlProvisioner>();

            services.AddScoped<ChildAotInsertionService>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IDatabaseProvisioner dbProvisioner)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            dbProvisioner.AddStepsFromAssemblyResources(typeof(Startup).Assembly);
            dbProvisioner.Provision();

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
