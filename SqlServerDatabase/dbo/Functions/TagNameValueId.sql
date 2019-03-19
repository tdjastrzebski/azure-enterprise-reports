-- Copyright © Tomasz Jastrzębski 2019
CREATE function [dbo].[TagNameValueId]
(
	@Name nvarchar(512),
	@Value nvarchar(256)
)
returns uniqueidentifier
as
begin
	--@declare @text as char(64) = convert(char(64), hashbytes('SHA2_256', @Name + ':' + @Value), 2)
	--return convert(uniqueidentifier,
	--   convert(binary(8), convert(bigint, convert(binary(8), substring(@text, 1, 16), 2)) ^ convert(bigint, convert(binary(8), substring(@text, 17, 16), 2))) +
	--   convert(binary(8), convert(bigint, convert(binary(8), substring(@text, 33, 16), 2)) ^ convert(bigint, convert(binary(8), substring(@text, 49, 16), 2)))
	--)
	return convert(uniqueidentifier, hashbytes('MD5', lower(@Name) + ':' + lower(@Value)), 2)
end