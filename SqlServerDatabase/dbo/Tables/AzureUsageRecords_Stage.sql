CREATE TABLE [dbo].[AzureUsageRecords_Stage] (
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
    [MeterCategory]           VARCHAR (50)     NOT NULL,
    [MeterId]                 UNIQUEIDENTIFIER NOT NULL,
    [MeterName]               VARCHAR (100)    NOT NULL,
    [MeterRegion]             VARCHAR (50)     NOT NULL,
    [MeterSubCategory]        VARCHAR (50)     NOT NULL,
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
    [Tags]                    NVARCHAR (MAX)   NOT NULL,
    [UnitOfMeasure]           VARCHAR (50)     NOT NULL
);








GO
CREATE CLUSTERED INDEX [IX_AzureUsageRecords_Stage]
    ON [dbo].[AzureUsageRecords_Stage]([Date] ASC);

