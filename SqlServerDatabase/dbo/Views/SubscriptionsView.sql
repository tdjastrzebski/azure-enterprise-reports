-- Copyright © Tomasz Jastrzębski 2019
CREATE view [dbo].[SubscriptionsView]
as
-- include only the latest subscription name to avoid duplicate SubscriptionGuids
select SubscriptionGuid, SubscriptionName
from (
	select SubscriptionGuid, SubscriptionName, max([Date]) LastDate,
		RowNumber = row_number() over (partition by SubscriptionGuid order by max([Date]) desc)
	from dbo.AzureUsageRecords
	group by SubscriptionGuid, SubscriptionName
) d
where RowNumber = 1