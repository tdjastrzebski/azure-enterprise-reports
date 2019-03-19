-- Copyright © Tomasz Jastrzębski 2019
CREATE view [dbo].[DepartmentAccountSubscriptionResourceGroupView]
as
select c.DepartmentId, d.DepartmentName, c.AccountId, a.AccountName, s.SubscriptionName, c.SubscriptionGuid, c.ResourceGroupId, g.ResourceGroup
from (
	select DepartmentId, AccountId, SubscriptionGuid, ResourceGroupId = dbo.ResourceGroupId(b.DepartmentId, b.AccountId, b.SubscriptionGuid, b.ResourceGroup)
	from (select distinct DepartmentId, AccountId, SubscriptionGuid, ResourceGroup from dbo.AzureUsageRecords r) b
)c
left join dbo.SubscriptionsView s on s.SubscriptionGuid = c.SubscriptionGuid
left join dbo.DepartmentsView d on d.DepartmentId = c.DepartmentId
left join dbo.AccountsView a on a.AccountId = c.AccountId
left join dbo.ResourceGroupsView g on g.ResourceGroupId = c.ResourceGroupId