CREATE TABLE [dbo].[AzureUsageRecords] (
    [AccountId]               INT              NOT NULL,
    [AccountName]             VARCHAR (50)     NOT NULL,
    [AccountOwnerEmail]       VARCHAR (50)     NOT NULL,
    [AdditionalInfo]          VARCHAR (512)    NOT NULL,
    [ChargesBilledSeparately] BIT              NOT NULL,
    [ConsumedQuantity]        DECIMAL (28, 16) NOT NULL,
    [ConsumedService]         VARCHAR (50)     NOT NULL,
    [ConsumedServiceId]       INT              NOT NULL,
    [Cost]                    DECIMAL (28, 22) NOT NULL,
    [CostCenter]              VARCHAR (50)     NOT NULL,
    [Date]                    DATE             NOT NULL,
    [DepartmentId]            INT              NOT NULL,
    [DepartmentName]          VARCHAR (50)     NOT NULL,
    [InstanceId]              VARCHAR (512)    NOT NULL,
    [Location]                VARCHAR (50)     NOT NULL,
    [MeterCategory]           VARCHAR (100)     NOT NULL,
    [MeterId]                 UNIQUEIDENTIFIER NOT NULL,
    [MeterName]               VARCHAR (100)    NOT NULL,
    [MeterRegion]             VARCHAR (50)     NOT NULL,
    [MeterSubCategory]        VARCHAR (100)     NOT NULL,
    [OfferId]                 VARCHAR (50)     NOT NULL,
    [PartNumber]              VARCHAR (50)     NOT NULL,
    [Product]                 VARCHAR (150)    NOT NULL,
    [ProductId]               INT              NOT NULL,
    [ResourceGroup]           VARCHAR (100)    NOT NULL,
    [ResourceGuid]            UNIQUEIDENTIFIER NOT NULL,
    [ResourceLocation]        VARCHAR (50)     NOT NULL,
    [ResourceLocationId]      INT              NOT NULL,
    [ResourceRate]            DECIMAL (28, 16) NOT NULL,
    [ServiceAdministratorId]  VARCHAR (50)     NOT NULL,
    [ServiceInfo1]            VARCHAR (50)     NOT NULL,
    [ServiceInfo2]            VARCHAR (50)     NOT NULL,
    [ServiceName]             VARCHAR (50)     NOT NULL,
    [ServiceTier]             VARCHAR (100)    NOT NULL,
    [StoreServiceIdentifier]  VARCHAR (50)     NOT NULL,
    [SubscriptionGuid]        UNIQUEIDENTIFIER NOT NULL,
    [SubscriptionId]          BIGINT           NOT NULL,
    [SubscriptionName]        VARCHAR (100)    NOT NULL,
    [Tags]                    NVARCHAR (MAX)   NULL,
    [UnitOfMeasure]           VARCHAR (50)     NOT NULL,
    [RecordId]                UNIQUEIDENTIFIER CONSTRAINT [DF_AzureUsageRecords_RecordId] DEFAULT (newsequentialid()) ROWGUIDCOL NOT NULL,
    CONSTRAINT [PK_dbo.AzureUsageRecords] PRIMARY KEY NONCLUSTERED ([RecordId] ASC)
);






GO
-- Copyright © Tomasz Jastrzębski 2019
CREATE trigger [dbo].[AzureUsageRecords_InsertTrigger]
   on dbo.AzureUsageRecords
   instead of insert
as

set nocount on;

insert into dbo.AzureUsageRecords
-- insert all the columns besides Tags
      (AccountId,AccountName,AccountOwnerEmail,AdditionalInfo,ChargesBilledSeparately,ConsumedQuantity,ConsumedService,ConsumedServiceId,Cost,CostCenter,[Date],DepartmentId,DepartmentName,InstanceId,[Location],MeterCategory,MeterId,MeterName,MeterRegion,MeterSubCategory,OfferId,PartNumber,Product,ProductId,ResourceGroup,ResourceGuid,ResourceLocation,ResourceLocationId,ResourceRate,ServiceAdministratorId,ServiceInfo1,ServiceInfo2,ServiceName,ServiceTier,StoreServiceIdentifier,SubscriptionGuid,SubscriptionId,SubscriptionName,UnitOfMeasure,RecordId)
select AccountId,AccountName,AccountOwnerEmail,AdditionalInfo,ChargesBilledSeparately,ConsumedQuantity,ConsumedService,ConsumedServiceId,Cost,CostCenter,[Date],DepartmentId,DepartmentName,InstanceId,[Location],MeterCategory,MeterId,MeterName,MeterRegion,MeterSubCategory,OfferId,PartNumber,Product,ProductId,ResourceGroup,ResourceGuid,ResourceLocation,ResourceLocationId,ResourceRate,ServiceAdministratorId,ServiceInfo1,ServiceInfo2,ServiceName,ServiceTier,StoreServiceIdentifier,SubscriptionGuid,SubscriptionId,SubscriptionName,UnitOfMeasure,RecordId
from inserted

-- insert tags
insert into dbo.AzureUsageTags
select RecordId, name, value
from inserted cross apply openjson(Tags) with (name nvarchar(512), value nvarchar(256))
where Tags is not null and Tags != ''

GO
CREATE CLUSTERED INDEX IX_AzureUsageRecords_Date
    ON dbo.AzureUsageRecords(Date ASC);

GO
CREATE NONCLUSTERED INDEX IX_AzureUsageRecords_Meter
    ON dbo.AzureUsageRecords(MeterId ASC, MeterName ASC, MeterCategory ASC, MeterSubcategory ASC, MeterRegion ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_Department-Account-Subscription]
    ON [dbo].[AzureUsageRecords]([SubscriptionGuid] ASC, [DepartmentId] ASC, [AccountId] ASC)
    INCLUDE([SubscriptionName], [DepartmentName], [AccountName], [ResourceGroup]);


GO
CREATE NONCLUSTERED INDEX [IX_AzureUsageRecords_Location]
    ON [dbo].[AzureUsageRecords]([Location] ASC);

