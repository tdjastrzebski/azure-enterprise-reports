CREATE TABLE [dbo].[AzureUsageTags] (
    [RecordId] UNIQUEIDENTIFIER NOT NULL,
    [TagName]  NVARCHAR (442)   NOT NULL,
    [TagValue] NVARCHAR (256)   NOT NULL,
    CONSTRAINT [PK_dbo.AzureUsageTags] PRIMARY KEY CLUSTERED ([RecordId] ASC, [TagName] ASC),
    CONSTRAINT [AzureUsageTags_RecordId_FK] FOREIGN KEY ([RecordId]) REFERENCES [dbo].[AzureUsageRecords] ([RecordId]) ON DELETE CASCADE
);






GO
CREATE NONCLUSTERED INDEX [IX_AzureUsageTags_Name]
    ON [dbo].[AzureUsageTags]([TagName] ASC)
    INCLUDE([TagValue]);



