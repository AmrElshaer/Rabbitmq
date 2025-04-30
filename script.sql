IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [inbox_messages] (
    [Id] uniqueidentifier NOT NULL,
    [ServiceName] nvarchar(200) NOT NULL,
    [IsProcessed] bit NOT NULL,
    [RowVersion] rowversion NOT NULL,
    [EventType] nvarchar(256) NOT NULL,
    [Payload] varbinary(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ProcessedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [Error] nvarchar(500) NULL,
    CONSTRAINT [PK_inbox_messages] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Orders] (
    [Id] uniqueidentifier NOT NULL,
    [Number] nvarchar(max) NOT NULL,
    [CustomerName] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [outbox_messages] (
    [id] uniqueidentifier NOT NULL,
    [retry_count] int NOT NULL DEFAULT 0,
    [event_type] nvarchar(256) NOT NULL,
    [payload] varbinary(max) NOT NULL,
    [created_at] datetime2 NOT NULL,
    [processed_at] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [status] nvarchar(20) NOT NULL,
    [Error] nvarchar(500) NULL,
    CONSTRAINT [pk_outbox_messages] PRIMARY KEY ([id])
);
GO

CREATE TABLE [processed_messages] (
    [Id] uniqueidentifier NOT NULL,
    [ProcessedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_processed_messages] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [inbox_subscribers] (
    [Id] uniqueidentifier NOT NULL,
    [SubscriberName] nvarchar(256) NOT NULL,
    [MessageId] uniqueidentifier NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Attempts] int NOT NULL DEFAULT 0,
    [LastAttemptedAt] datetime2 NULL,
    [Error] nvarchar(500) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_inbox_subscribers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_inbox_subscribers_inbox_messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [inbox_messages] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_inbox_messages_Id] ON [inbox_messages] ([Id]);
GO

CREATE UNIQUE INDEX [IX_inbox_subscribers_MessageId_SubscriberName] ON [inbox_subscribers] ([MessageId], [SubscriberName]);
GO

CREATE INDEX [IX_inbox_subscribers_Status] ON [inbox_subscribers] ([Status]);
GO

CREATE INDEX [ix_outbox_messages_status_retry_created] ON [outbox_messages] ([status], [retry_count], [created_at]) INCLUDE ([id], [event_type], [payload]) WHERE status IN ('Pending', 'Failed');
GO

CREATE INDEX [ix_outbox_messages_unprocessed] ON [outbox_messages] ([created_at]) INCLUDE ([id], [event_type], [payload]) WHERE processed_at IS NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250429171820_Initial', N'8.0.8');
GO

COMMIT;
GO

