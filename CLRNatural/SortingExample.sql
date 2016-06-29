create table SortingExample (
	id int identity(1,1),
	title nvarchar(255) not null,
	sort int default null
)