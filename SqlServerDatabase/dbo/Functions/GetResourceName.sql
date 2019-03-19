-- Copyright © Tomasz Jastrzębski 2019
create function [dbo].[GetResourceName]
(
	@InstanceId varchar(512)
)
returns varchar(150)
as
begin
	return case when charindex('/', @InstanceId) > 0 then substring(@InstanceId, len(@InstanceId) - charindex('/', reverse(@InstanceId)) + 2, charindex('/', reverse(@InstanceId)) - 1) else @InstanceId end
end