-- Copyright © Tomasz Jastrzębski 2019
CREATE view dbo.TagNamesValuesView
as
select TagName, TagValue, TagNameValueId = dbo.TagNameValueId(TagName, TagValue)
from (
	select distinct TagName, TagValue
	from dbo.AzureUsageTags
	where TagName not like 'hidden-%' and TagName not like 'link:%'
) d