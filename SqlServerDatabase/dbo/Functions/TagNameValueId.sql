-- Copyright © Tomasz Jastrzębski 2019
CREATE function [dbo].[TagNameValueId]
(
	@Name nvarchar(512),
	@Value nvarchar(256)
)
returns uniqueidentifier
as
begin
	return convert(uniqueidentifier, hashbytes('MD5', lower(@Name) + ':' + lower(@Value)), 2)
end
