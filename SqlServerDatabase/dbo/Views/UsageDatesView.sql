-- Copyright © Tomasz Jastrzębski 2019
CREATE view [dbo].[UsageDatesView]
as
select
	[Date],
	[Year] = datepart(year, [Date]),
	--[Month],
	--[MonthName] = datename(month, [Date]),
	[MonthName] = (case when month([Date]) < 10 then 'M0' else 'M' end) + cast(month([Date]) as varchar(2)),
	--[Quarter] = datepart(quarter, [Date]),
	QuarterName = 'Q' + cast(datepart(quarter, [Date]) as char(1)),
	--[Week] = datepart(week, [Date]),
	[WeekName] = (case when datepart(week, [Date]) < 10 then 'W0' else 'W' end) + cast(datepart(week, [Date]) as varchar(2)),
	YearQuarter = cast(year([Date]) as char(4)) + ' Q' + cast(datepart(quarter, [Date]) as varchar(2)),
	YearMonth = cast(year([Date]) as char(4)) + (case when month([Date]) < 10 then ' M0' else ' M' end) + cast(month([Date]) as varchar(2)),
	YearWeek = cast(year([Date]) as char(4)) + (case when datepart(week, [Date]) < 10 then ' W0' else ' W' end) + cast(datepart(week, [Date]) as varchar(2)),
	--[Weekday] = datepart(weekday, [Date]),
	[Weekday] = (datepart(weekday, [Date]) + @@datefirst + 5) % 7 + 1 -- Monday first
	--[WeekdayName] = datename(weekday, [Date])
from (
	select distinct [Date] from [dbo].[AzureUsageRecords]
) d
