-- Copyright © Tomasz Jastrzębski 2019
create procedure [dbo].[TruncateAzureUsageRecords_Stage]
with execute as 'aer-admin'
as
begin
	set nocount on;
	truncate table dbo.AzureUsageRecords_Stage
end
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[TruncateAzureUsageRecords_Stage] TO [aer-writer]
    AS [dbo];

