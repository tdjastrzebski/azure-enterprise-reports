-- Author: Tomasz Jastrzębski 2008
-- All Rights Granted, Creative Commons CC0 License
CREATE procedure [dbo].[DefragmentIndexes]
	@Table nvarchar(128),
	@IndexCount int = null out,
	@DefragCount int = null out
with execute as 'aer-admin'
as

set nocount on;

declare row_cursor cursor fast_forward for
	select [name], avg_fragmentation_in_percent
	from sys.dm_db_index_physical_stats(db_id(), object_id(@Table), null, null, null) as a
	inner join sys.indexes as b on a.object_id = b.object_id and a.index_id = b.index_id

declare @name sysname = null;
declare @fragmentation float = 0;

set @IndexCount = 0
set @DefragCount = 0

open row_cursor;
fetch next from row_cursor into @name, @fragmentation;

while @@fetch_status = 0 begin
	set @IndexCount = @IndexCount + 1

	if @fragmentation <= 5 begin
		print @name + ' fragmented ' + cast(@fragmentation as varchar) + '% - skipping';
	end
	
	if @fragmentation > 5 and @fragmentation <= 30 begin
		print @name + ' fragmented ' + cast(@fragmentation as varchar) + '% - reorganizing';
		set @DefragCount = @DefragCount + 1
		exec (N'alter index [' + @name + '] on ' + @Table + ' reorganize;');
	end
	
	if @fragmentation > 30 begin
		print @name + ' fragmented ' + cast(@fragmentation as varchar) + '% - rebuilding';
		set @DefragCount = @DefragCount + 1
		exec (N'alter index [' + @name + '] on ' + @Table + ' rebuild;');
	end

	fetch next from row_cursor into @name, @fragmentation;
end

close row_cursor;
deallocate row_cursor;
GO

GRANT EXECUTE
    ON OBJECT::[dbo].[DefragmentIndexes] TO [aer-writer]
    AS [dbo];

