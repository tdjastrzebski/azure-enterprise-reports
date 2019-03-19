-- Copyright © Tomasz Jastrzębski 2019
create view dbo.DepartmentsView
as
select DepartmentId, DepartmentName
from (
	select DepartmentId, DepartmentName, max([Date]) LastDate, RowNumber = row_number() over (partition by DepartmentId order by max([Date]) desc)
	from dbo.AzureUsageRecords
	group by DepartmentId, DepartmentName
) a
where RowNumber = 1