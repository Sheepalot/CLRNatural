create procedure selectSortableRow
	@id int = null
as
begin
	select
		srt.title,
		srt.id,
		srt.sort
	from
		SortingExample srt
	where
		srt.id = @id
	order by
		srt.sort
end