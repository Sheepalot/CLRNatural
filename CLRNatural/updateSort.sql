create procedure updateSort
	@id int,
	@sort int
as
begin
	update
		SortingExample
	set
		sort = sort + 1
	where
		sort >= @sort
	
	update
		SortingExample
	set
		sort = @sort
	where
		id = @id
end
go