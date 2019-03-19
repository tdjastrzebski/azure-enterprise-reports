-- Copyright © Tomasz Jastrzębski 2019
CREATE view [dbo].[AzureUsageRecordsView]
as
select RecordId,
	ResourceGroupId = dbo.ResourceGroupId(DepartmentId, AccountId, SubscriptionGuid, ResourceGroup),
	NewMeterId =  dbo.NewMeterId(MeterCategory, MeterSubcategory, MeterName),
	[Date],
	[Location],
	InstanceId, Product,
	Cost, ConsumedQuantity, UnitOfMeasure
from dbo.AzureUsageRecords