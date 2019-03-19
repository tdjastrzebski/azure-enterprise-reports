-- Copyright © Tomasz Jastrzębski 2019
CREATE view [dbo].[LocationsView]
as
select [Location]
from (
	select [Location], RowNumber = row_number() over (partition by [Location] collate SQL_Latin1_General_CP1_CI_AS order by [Location] collate SQL_Latin1_General_CP1_CS_AS asc)
	from (
		select distinct [Location] collate SQL_Latin1_General_CP1_CS_AS [Location]
		from dbo.AzureUsageRecords
	) d
) e where RowNumber = 1