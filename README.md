# FSharp DTO mapper

WARNING: No production ready!

Uses [Donald](https://github.com/pimbrouwers/Donald) under the hood.

Features:
* Model communication with DB handles by DTO object, for example write flow: Model --> ModelDTO --> Database. Read flow: Database --> ModelDTO --> Model
* you may use custom SQL
* You may extend it for any DB  (currently it works for Postgres only, but you may add modules for other DBs, see the source)

An example located in `example` directory.