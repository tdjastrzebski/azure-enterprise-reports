-- Copyright © Tomasz Jastrzębski 2019
CREATE view dbo.TagsView
as
select RecordId, TagNameValueId = dbo.TagNameValueId(TagName, TagValue), TagName, TagValue
from dbo.AzureUsageTags
where TagName not like 'hidden-%' and TagName not like 'link:%'
