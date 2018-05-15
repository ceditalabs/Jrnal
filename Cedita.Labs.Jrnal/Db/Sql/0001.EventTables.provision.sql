-- Protect from already being ran
IF ((SELECT 1 FROM [dbo].[_MigrationHistory] WHERE [MigrationName] = N'EventTables') IS NULL)
BEGIN

	CREATE TABLE [dbo].[Applications] (
		[Id] INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
		[Name] VARCHAR(50) NOT NULL,
		[ApiKey] NVARCHAR(50) NOT NULL

		CONSTRAINT [IX_Tables_Name] UNIQUE([Name]),
		CONSTRAINT [IX_Tables_ApiKey] UNIQUE([ApiKey])
	);
	
	CREATE TABLE [dbo].[Events] (
		[Id] INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
		[ApplicationId] INT NOT NULL CONSTRAINT [FK_Events_ApplicationId] FOREIGN KEY REFERENCES [dbo].[Applications] (Id) INDEX [IX_Events_ApplicationId] NONCLUSTERED,
		[Timestamp] DATETIMEOFFSET NOT NULL,
		[Level] NVARCHAR(20) NOT NULL,
		[MessageTemplate] NVARCHAR(MAX) NOT NULL,
		[HasException] BIT,
		[AotInsertionMarker] INT,
	);

	CREATE TABLE [dbo].[Properties] (
		[Id] INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
		[EventId] INT NOT NULL CONSTRAINT [FK_Properties_EventId] FOREIGN KEY REFERENCES [dbo].[Events] (Id) INDEX [IX_Properties_EventId] NONCLUSTERED,
		[ParentPropertyId] INT NULL CONSTRAINT [FK_Properties_PropertyId] FOREIGN KEY REFERENCES [dbo].[Properties] (Id),
		[Name] NVARCHAR(100) NOT NULL,
		[Value] NVARCHAR(MAX) NULL,
		[AotInsertionMarker] INT,
	);

	CREATE TABLE [dbo].[RenderingGroups] (
		[Id] INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
		[EventId] INT NOT NULL CONSTRAINT [FK_RenderingGroups_EventId] FOREIGN KEY REFERENCES [dbo].[Events] (Id) INDEX [IX_RenderingGroups_EventId] NONCLUSTERED,
		[Name] NVARCHAR(100) NOT NULL,
		[AotInsertionMarker] INT,
	);

	CREATE TABLE [dbo].[Renderings] (
		[Id] INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
		[RenderingGroupId] INT NOT NULL CONSTRAINT [FK_Renderings_RenderingGroupId] FOREIGN KEY REFERENCES [dbo].[Events] (Id) INDEX [IX_Renderings_RenderingGroupId] NONCLUSTERED,
		[Format] NVARCHAR(100) NOT NULL,
		[Rendering] NVARCHAR(MAX) NULL
	);

	INSERT INTO [dbo].[_MigrationHistory] ([MigrationName], [DateApplied]) VALUES (N'EventTables', CURRENT_TIMESTAMP);
END;