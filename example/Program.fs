open System
open System.Data
open NodaTime
open FsDto
open Donald
open Npgsql

open FsDto.Database

type DOBRange = R15_25 | R26_35 | R36_45 | R46_55 | R56_65 | R65Plus

type User = 
    {
        Id: PrimaryKeyInt
        Username: string //add unique
        Email: string option
        Tel: string
        IsActive: bool
        FirstName: string
        FatherName: string option
        LastName: string option
        Sex: string
        DateOfBirth: LocalDate option
        DobRange: DOBRange option
        DateJoined: ZonedDateTime
        LastActivity: ZonedDateTime
    }

type UserDTO() =
    inherit DTO<User>()

    // Map To instance types
    static member mapTo_DobRange(rd : IDataReader, col_name: string) =
        match rd.ReadStringOption col_name with
        Option.None -> None
        | Some "R15_25" -> Some R15_25
        | Some "R26_35" -> Some R26_35
        | Some "R36_45" -> Some R36_45
        | Some "R46_55" -> Some R46_55
        | Some "R56_65" -> Some R56_65
        | Some "R65Plus" -> Some R65Plus
        | _ -> None

    static member mapTo_DateOfBirth(rd : IDataReader, col_name: string) =
        let pgRd = rd :?> NpgsqlDataReader
        match pgRd.ReadDateTimeOption col_name with
        Some v -> Some (LocalDate.FromDateTime v)
        | None -> None

    static member mapTo_DateJoined(rd : IDataReader, col_name: string) =
        let pgRd = rd :?> NpgsqlDataReader
        pgRd.ReadDateTimeOffset "date_joined" |> ZonedDateTime.FromDateTimeOffset

    static member mapTo_LastActivity(rd : IDataReader, col_name: string) =
        let pgRd = rd :?> NpgsqlDataReader
        pgRd.ReadDateTimeOffset "last_activity" |> ZonedDateTime.FromDateTimeOffset
    

    // Map From val to Sql types
    static member mapFrom_DobRange (dob_range : DOBRange option) =
        match dob_range with
        | Some R15_25 -> SqlType.String "R15_25"
        | Some R26_35 -> SqlType.String "R26_35"
        | Some R36_45 -> SqlType.String "R36_45"
        | Some R46_55 -> SqlType.String "R46_55"
        | Some R56_65 -> SqlType.String "R56_65"
        | Some R65Plus -> SqlType.String "R65Plus"
        | None -> SqlType.Null

    static member mapFrom_DateOfBirth(value: LocalDate option) =
        match value with Some s -> SqlType.DateTime (s.ToDateTimeUnspecified()) | None -> SqlType.Null

    static member mapFrom_DateJoined(value: ZonedDateTime) =
        SqlType.DateTimeOffset (value.ToDateTimeOffset())
    
    static member mapFrom_LastActivity(value: ZonedDateTime) =
        SqlType.DateTimeOffset (value.ToDateTimeOffset())


    static member getConn =
        let connectionString = "Host=localhost; Port=5432; Database=db; Username=user; Password='pass'; Timeout=10"
        let conn = new Npgsql.NpgsqlConnection(connectionString)
        conn

    
    member this.get (id: int): DbResult<User option> =    
        let sql = UserDTO.SqlSelectOne
        printfn "%s" sql
        let p = [ "id", SqlType.Int id ]
        UserDTO.getConn
        |> Db.newCommand sql
        |> Db.setParams p
        |> Db.querySingle this.ReadFromDB

    member this.list: DbResult<User list> =    
        let sql = UserDTO.SqlSelectAll
        printfn "%s" sql
        UserDTO.getConn
        |> Db.newCommand sql
        //|> Db.setParams param
        |> Db.query this.ReadFromDB

    member this.handle (k,bv) = 
        (k, unbox(bv))

    member this.create (user: User) =
        let sql = UserDTO.SqlInsertOut
        printfn "%s" sql
        let qparams = this.getFieldValuesList user
        printfn "%A" qparams
        //let p = this.getFieldValuesList |> List.map this.handle
        UserDTO.getConn
        |> Db.newCommand sql
        |> Db.setParams qparams
        |> Db.querySingle this.ReadFromDB
        //|> Db.ExecReader

    member this.update (user: User) =
        let sql = UserDTO.SqlUpdateOut
        printfn "%s" sql
        let qparams = this.getFieldValuesList user
        printfn "%A" qparams
        //let p = this.getFieldValuesList |> List.map this.handle
        UserDTO.getConn
        |> Db.newCommand sql
        |> Db.setParams qparams
        |> Db.querySingle this.ReadFromDB
        //|> Db.exec

    member this.delete (user: User) =
        let sql = UserDTO.SqlDeleteOne
        printfn "%s" sql
        UserDTO.getConn
        |> Db.newCommand sql
        |> Db.setParams [ "id", match user.Id with IntF i -> SqlType.Int  i | _ -> SqlType.Null ]
        |> Db.exec
        
        

[<EntryPoint>]
let main argv =
    let userDto = UserDTO()

    let user = userDto.get 1
    match user with
    | Ok result' -> 
        match result' with
        | Some u ->
            //userDto.delete u // you may delete item
            let userCr = {u with Id=AutoInc; Email=(Some "arthur17@company.com")}
            printfn "%A" userCr
            let userOut = userDto.create userCr
            printfn "%A" userOut
            match userOut with
            | Ok res -> 
                match res with
                | Some u ->
                    let user4 = {u with Email=Some "555@company.com"}
                    match userDto.update user4 with
                    | Ok res ->
                        match res with
                        | Some u -> 
                            printfn "updated result is: %A" u
                        | None -> printfn "None :("
                    | Error e -> printf "DbResult should not be Error: %s" e.Error.Message
                | None -> ()
            | Error e -> printf "DbResult should not be Error: %s" e.Error.Message
        | None -> ()
           
    | Error e -> printf "DbResult should not be Error: %s" e.Error.Message

    
    0 // return an integer exit code