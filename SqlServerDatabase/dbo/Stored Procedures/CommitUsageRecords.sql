-- Copyright © Tomasz Jastrzębski 2019
CREATE procedure [dbo].[CommitUsageRecords]
	@DateFrom date,
	@DateTo date
as
begin
	set nocount on;

	delete from dbo.AzureUsageRecords with(tablock)
	where [Date] between @DateFrom and @DateTo
	
	set nocount off;

	insert into dbo.AzureUsageRecords with(tablock)
          (AccountId,AccountName,AccountOwnerEmail,AdditionalInfo,ChargesBilledSeparately,ConsumedQuantity,ConsumedService,ConsumedServiceId,Cost,CostCenter,[Date],DepartmentId,DepartmentName,InstanceId,[Location],MeterCategory,MeterId,MeterName,MeterRegion,MeterSubCategory,OfferId,PartNumber,Product,ProductId,ResourceGroup,ResourceGuid,ResourceLocation,ResourceLocationId,ResourceRate,ServiceAdministratorId,ServiceInfo1,ServiceInfo2,ServiceName,ServiceTier,StoreServiceIdentifier,SubscriptionGuid,SubscriptionId,SubscriptionName,Tags,UnitOfMeasure)
	SELECT AccountId,AccountName,AccountOwnerEmail,AdditionalInfo,ChargesBilledSeparately,ConsumedQuantity,ConsumedService,ConsumedServiceId,Cost,CostCenter,[Date],DepartmentId,DepartmentName,InstanceId,[Location],MeterCategory,MeterId,MeterName,MeterRegion,MeterSubCategory,OfferId,PartNumber,Product,ProductId,ResourceGroup,ResourceGuid,ResourceLocation,ResourceLocationId,ResourceRate,ServiceAdministratorId,ServiceInfo1,ServiceInfo2,ServiceName,ServiceTier,StoreServiceIdentifier,SubscriptionGuid,SubscriptionId,SubscriptionName,Tags,UnitOfMeasure
	from dbo.AzureUsageRecords_Stage with(nolock)
	where [Date] between @DateFrom and @DateTo
end
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[CommitUsageRecords] TO [aer-writer]
    AS [dbo];

