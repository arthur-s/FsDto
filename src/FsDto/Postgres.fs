namespace FsDto

module Database = 
    open Npgsql
    open Donald
    open System
    open System.Data

    type NpgsqlDataReader with
            member private this.GetOrdinalOption (name : string) = 
                let i = this.GetOrdinal(name)        
                match this.IsDBNull(i) with 
                | true  -> None
                | false -> Some(i)

            member private this.GetOption (map : int -> 'a when 'a : struct) (name : string) = 
                let fn v = 
                    try
                        map v
                    with 
                    | :? InvalidCastException as ex -> raise (FailiedCastException { FieldName = name; Error = ex })
                
                this.GetOrdinalOption(name)
                |> Option.map fn

            /// Safely retrieve DateTime Option
            member this.ReadDateTimeOffsetOption (name : string) =
                //name |> this.GetOption (fun i -> this.GetFieldValue(i))
                name |> this.GetOption (fun i -> this.GetFieldValue<DateTimeOffset>(i))

            member this.ReadDateTimeOffset (name : string) =
                this.ReadDateTimeOffsetOption name |> Option.defaultValue DateTimeOffset.MinValue
        end