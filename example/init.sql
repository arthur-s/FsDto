CREATE TABLE IF NOT EXISTS public.user (
	id SERIAL PRIMARY KEY,
	username text not null,
    email varchar(255),
	tel varchar(255) not null,
    is_active boolean default FALSE,
    first_name text not null,
    father_name text,
    last_name text,
	sex char not null,
	date_of_birth date,
	dob_range varchar(10),
    date_joined timestamp with time zone not null,
	last_activity timestamp with time zone not null
);

Insert into "user"
	(username, email, tel, is_active, first_name, father_name, last_name, sex,
	 date_of_birth, dob_range, 
	 date_joined, last_activity)
values
	('arthur', 'arthur@example.com', 792700000, True, '', '', '', 'm',
	 '1987-01-01', 'R26_35',
	 '2021-9-15 14:00:00 +05:00', '2021-9-15 15:00:00 +05:00');
