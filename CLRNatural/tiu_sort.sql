create trigger tiu_sort on SortingExample for insert, update
as
begin
	SET NOCOUNT ON;
    IF UPDATE (title)
    BEGIN
		declare
			@id int
        declare updated
		cursor local static for
			select
				i.id
			from
				inserted i
	
		open updated
		fetch updated into @id
		while @@fetch_status = 0
		begin
			exec NaturalSort 'getMidpointForSort', 'selectSortableRow', 'updateSort', @id
			fetch updated into @id
		end
	close updated
	deallocate updated
    END
end