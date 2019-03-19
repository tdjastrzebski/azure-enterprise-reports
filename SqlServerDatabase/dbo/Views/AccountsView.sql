-- Copyright © Tomasz Jastrzębski 2019
create view dbo.AccountsView
as
select AccountId, AccountName
from (
	select AccountId, AccountName, max([Date]) LastDate, RowNumber = row_number() over (partition by AccountId order by max([Date]) desc)
	from dbo.AzureUsageRecords
	group by AccountId, AccountName
) a
where RowNumber = 1