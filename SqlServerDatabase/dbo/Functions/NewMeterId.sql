-- Copyright © Tomasz Jastrzębski 2019
CREATE function [dbo].[NewMeterId]
(
	@MeterCategory varchar(50),
	@MeterSubcategory varchar(50),
	@MeterName varchar(100)
)
returns uniqueidentifier
as
begin
	return convert(uniqueidentifier, hashbytes('MD5', lower(@MeterCategory) + ':' + lower(@MeterSubcategory) + ':' + lower(@MeterName)), 2)
end