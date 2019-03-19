-- Copyright © Tomasz Jastrzębski 2019
CREATE view dbo.MetersView
-- Azure data, albeit rarely, contain the same MeterIds for different MeterCategory, MeterSubcategory, MeterName, MeterRegion
as
select MeterCategory, MeterSubcategory, MeterName,
	NewMeterId =  dbo.NewMeterId(MeterCategory, MeterSubcategory, MeterName)
from (
	select distinct MeterCategory, MeterSubcategory, MeterName
	from dbo.AzureUsageRecords
) d