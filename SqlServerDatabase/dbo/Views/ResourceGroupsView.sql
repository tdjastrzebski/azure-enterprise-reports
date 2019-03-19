-- Copyright © Tomasz Jastrzębski 2019
CREATE view [dbo].[ResourceGroupsView]
as
select ResourceGroup, SubscriptionGuid, ResourceGroupId = dbo.ResourceGroupId(DepartmentId, AccountId, SubscriptionGuid, ResourceGroup)
from (
	select DepartmentId, AccountId, SubscriptionGuid, ResourceGroup, RowNumber = row_number() over (partition by DepartmentId, AccountId, SubscriptionGuid, ResourceGroup collate SQL_Latin1_General_CP1_CI_AS order by ResourceGroup collate SQL_Latin1_General_CP1_CS_AS asc)
	from (
		select distinct DepartmentId, AccountId,  SubscriptionGuid, ResourceGroup collate SQL_Latin1_General_CP1_CS_AS ResourceGroup
		from dbo.AzureUsageRecords
	) d
) e where RowNumber = 1