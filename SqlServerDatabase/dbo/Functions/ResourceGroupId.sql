-- Copyright © Tomasz Jastrzębski 2019
CREATE function [dbo].[ResourceGroupId]
(
	@DepartmentId int,
	@AccountId int,
	@SubscriptionGuid uniqueidentifier,
	@ResourceGroup varchar(100)
)
returns uniqueidentifier
as
begin
	return convert(uniqueidentifier, hashbytes('MD5', cast(@DepartmentId as varchar) + ':' + cast(@AccountId as varchar)  + ':' + cast(@SubscriptionGuid as char(36)) + ':' + lower(@ResourceGroup)), 2)
end