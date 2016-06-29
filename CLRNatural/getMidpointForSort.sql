create procedure getMidpointForSort
	@id 	int,
	@lower_order int,
	@higher_order int
as
begin
	WITH subTable AS (
		select
			ROW_NUMBER() OVER (order by	srt.sort) as row_number,
			srt.title as sort_string,
			srt.id as id,
			srt.sort as sort
		from
			SortingExample srt
		where
			srt.id = id
		and	(
			@lower_order is null or srt.sort > @lower_order
		) 
		and	(
			@higher_order is null or srt.sort < @higher_order
		) 
		and srt.sort is not null
		and srt.id != id
	)
	SELECT TOP 1 
		sort_string, 
		id, 
		sort
	FROM 
		subTable
	WHERE 
		row_number >= ((select count(*) from subTable) /2)
	order by row_number
end