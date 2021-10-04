namespace FsDto

open Npgsql
open Donald
open System
open System.Data
open FSharp.Reflection
open System.Reflection

exception MappingNotFound of string

type PrimaryKeyInt = 
| AutoInc
| IntF of int

//[<AbstractClass>]
type DTO<'T>() =
    //abstract member customMapFromDB: IDataReader * Type * string -> obj
    //default this.customMapFromDB(rd, t, col_name) = raise (System.NotImplementedException("customMapFromDB is not implemented"))
    
    static member handleChar(c: char) =
        if Char.IsUpper(c) then "_" + c.ToString().ToLower()
        else c.ToString().ToLower()
    
    static member toColumnName (s: string) : string =
        let first_char = s.[0].ToString().ToLower()
        let norm_tail = Seq.tail(s) |> Seq.map(fun x -> DTO<'T>.handleChar x) |> String.Concat
        first_char.ToString() + norm_tail

    member this.readValue(rd: IDataReader, t: Type, recordKeyName: string) =
        let col_name = recordKeyName |> DTO<'T>.toColumnName
        if t = typeof<PrimaryKeyInt> then box(IntF (rd.ReadInt32 col_name))
        elif t = typeof<bool> then box(rd.ReadBoolean col_name)
        elif t = typeof<int16> then box(rd.ReadInt16 col_name)
        elif t = typeof<int32> then box(rd.ReadInt32 col_name)
        elif t = typeof<int64> then box(rd.ReadInt64 col_name)
        elif t = typeof<decimal> then box(rd.ReadDecimal col_name)
        elif t = typeof<double> then box(rd.ReadDouble col_name)
        elif t = typeof<float> then box(rd.ReadFloat col_name)
        elif t = typeof<char> then box(rd.ReadChar col_name)
        elif t = typeof<string> then box(rd.ReadString col_name)
        elif t = typeof<byte> then box(rd.ReadByte col_name)
        elif t = typeof<byte[]> then box(rd.ReadBytes col_name)
        elif t = typeof<Guid> then box(rd.ReadGuid col_name)
        elif t = typeof<DateTime> then box(rd.ReadDateTime col_name)
        
        elif t = typeof<bool option> then box(rd.ReadBooleanOption col_name)
        elif t = typeof<int16 option> then box(rd.ReadInt16Option col_name)
        elif t = typeof<int32 option> then box(rd.ReadInt32Option col_name)
        elif t = typeof<int64 option> then box(rd.ReadInt64Option col_name)
        elif t = typeof<decimal option> then box(rd.ReadDecimalOption col_name)
        elif t = typeof<double option> then box(rd.ReadDoubleOption col_name)
        elif t = typeof<float option> then box(rd.ReadFloatOption col_name)
        elif t = typeof<char option> then box(rd.ReadCharOption col_name)
        elif t = typeof<string option> then box(rd.ReadStringOption col_name)
        elif t = typeof<byte option> then box(rd.ReadByteOption col_name)
        elif t = typeof<byte[] option> then box(rd.ReadBytesOption col_name)
        elif t = typeof<Guid option> then box(rd.ReadGuidOption col_name)
        elif t = typeof<DateTime option> then box(rd.ReadDateTimeOption col_name)

        else
            let method_name = sprintf "mapTo_%s" recordKeyName
            // this method doesn't allow to use static method
            let self_type = this.GetType()
            let static_flags = BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.IgnoreCase
            let mi = self_type.GetMethod(method_name, static_flags)
            if mi = null then
                raise (System.NotImplementedException($"Method {method_name} is not implemented"))

            printfn "%A" mi
            let result = mi.Invoke(null, [| rd; col_name |])
            box(result)

    member this.ReadFromDB(rd: IDataReader) =
        let DomModelType = typeof<'T>
        assert (FSharpType.IsRecord(DomModelType))
        let fields = FSharp.Reflection.FSharpType.GetRecordFields(DomModelType)
        let values = 
            fields
            |> Array.map(fun x-> (unbox (this.readValue (rd, x.PropertyType, x.Name))))
            //|> Array.collect (fun elem -> [| 0 .. elem |])
        
        //let inst = Activator.CreateInstance(typeof<'T>)
        Reflection.FSharpValue.MakeRecord(DomModelType, values) :?> 'T


    member this.mapFromDTO(rd: IDataReader) =
        this.ReadFromDB(rd)

    (*
    static member printCreateTable =
        let fields = FSharp.Reflection.FSharpType.GetRecordFields(DomModelType)
        let values = 
            fields
            |> Array.iter(fun x -> )
    *)

    
    static member genSqlColumns (includePK: bool) =
        typeof<'T>.GetProperties()
        |> Array.filter (fun elem -> match includePK with true -> true | _ -> elem.PropertyType <> typeof<PrimaryKeyInt>)
        |> Array.map(fun x-> DTO<'T>.toColumnName x.Name) |> Array.toList

    static member genSqlValues (includePK: bool) =
        typeof<'T>.GetProperties()
        |> Array.filter (fun elem -> match includePK with true -> true | _ -> elem.PropertyType <> typeof<PrimaryKeyInt>)
        |> Array.map(fun x-> "@" + DTO<'T>.toColumnName x.Name) |> Array.toList
        
    static member genSqlKVPairs (includePK: bool) =
        typeof<'T>.GetProperties()
        |> Array.filter (fun elem -> match includePK with true -> true | _ -> elem.PropertyType <> typeof<PrimaryKeyInt>)
        |> Array.map(fun x->
            DTO<'T>.toColumnName x.Name + " = @" + DTO<'T>.toColumnName x.Name) |> Array.toList

    static member SqlSelectAll : string =
        //let table = "users"
        let table = typeof<'T>.Name.ToLower()
        let fields = String.Join(", ", DTO<'T>.genSqlColumns(includePK = true))
        $"Select {fields}
          from public.{table}"

    static member SqlSelectOne : string =
        //let table = "users"
        let table = typeof<'T>.Name.ToLower()
        let fields = String.Join(", ", DTO<'T>.genSqlColumns(includePK = true))
        $"Select {fields}
          from public.{table}
          where
          id = @id"


    member this.getFieldValuesList (data: 'T) : (string * SqlType) list =
        let self_type = typeof<'T>
        printfn "%A" self_type

        self_type.GetProperties()
        // we don't need a field with autoincrement
        |> Array.filter (fun elem -> elem.PropertyType <> typeof<PrimaryKeyInt>)
        |> Array.map(fun p ->
            let prop_info = self_type.GetProperty(p.Name)
            let prop_val = prop_info.GetValue(data)
            let t = prop_info.PropertyType

            printfn "%A" prop_info
            printfn "%A" prop_val

            //let prop_type = prop_val.GetType()
            //let prop_val_casted = (downcast prop_val)
            //DTO<'T>.getPropValue (ty, prop_info, prop_val)

            let value = prop_val
            let final_v = 
                //if   t = typeof<PrimaryKeyInt> then SqlType.Int (value :?> int)
                if t = typeof<bool> then SqlType.Boolean (value :?> bool)
                elif t = typeof<int16> then SqlType.Int16 (value :?> int16)
                elif t = typeof<Int32> then SqlType.Int32 (value :?> int32)
                elif t = typeof<int64> then SqlType.Int64 (value :?> int64)
                elif t = typeof<decimal> then SqlType.Decimal (value :?> decimal)
                elif t = typeof<double> then SqlType.Double (value :?> double)
                elif t = typeof<float> then SqlType.Float (value :?> float)
                elif t = typeof<char> then SqlType.Char (value :?> char)
                elif t = typeof<string> then SqlType.String (value :?> string)
                elif t = typeof<byte> then SqlType.Byte (value :?> byte)
                elif t = typeof<byte[]> then SqlType.Bytes (value :?> byte[])
                elif t = typeof<Guid> then SqlType.Guid (value :?> Guid)
                elif t = typeof<DateTime> then SqlType.DateTime (value :?> DateTime)

                elif t = typeof<bool option> then 
                    let vo = value :?> bool option
                    match vo with Some v -> SqlType.Boolean v | None -> SqlType.Null

                elif t = typeof<int16 option> then 
                    let vo = value :?> int16 option
                    match vo with Some v -> SqlType.Int16 v | None -> SqlType.Null

                elif t = typeof<int32 option> then 
                    let vo = value :?> int32 option
                    match vo with Some v -> SqlType.Int32 v | None -> SqlType.Null

                elif t = typeof<int64 option> then 
                    let vo = value :?> int64 option
                    match vo with Some v -> SqlType.Int64 v | None -> SqlType.Null

                elif t = typeof<decimal option> then 
                    let vo = value :?> decimal option
                    match vo with Some v -> SqlType.Decimal v | None -> SqlType.Null

                elif t = typeof<double option> then 
                    let vo = value :?> double option
                    match vo with Some v -> SqlType.Double v | None -> SqlType.Null

                elif t = typeof<float option> then 
                    let vo = value :?> float option
                    match vo with Some v -> SqlType.Float v | None -> SqlType.Null

                elif t = typeof<char option> then 
                    let vo = value :?> char option
                    match vo with Some v -> SqlType.Char v | None -> SqlType.Null

                elif t = typeof<string option> then 
                    let vo = value :?> string option
                    match vo with Some v -> SqlType.String v | None -> SqlType.Null

                elif t = typeof<byte option> then 
                    let vo = value :?> byte option
                    match vo with Some v -> SqlType.Byte v | None -> SqlType.Null

                elif t = typeof<byte[] option> then 
                    let vo = value :?> byte[] option
                    match vo with Some v -> SqlType.Bytes v | None -> SqlType.Null

                elif t = typeof<Guid option> then 
                    let vo = value :?> Guid option
                    match vo with Some v -> SqlType.Guid v | None -> SqlType.Null

                elif t = typeof<DateTime option> then 
                    let vo = value :?> DateTime option
                    match vo with Some v -> SqlType.DateTime v | None -> SqlType.Null


                else
                    let method_name = sprintf "mapFrom_%s" p.Name
                    // this method doesn't allow to use static method
                    let self_type = this.GetType()
                    let static_flags = BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.IgnoreCase
                    let mi = self_type.GetMethod(method_name, static_flags)
                    if mi = null then
                        raise (System.NotImplementedException($"Method {method_name} is not implemented"))

                    printfn "%A" mi
                    let res = mi.Invoke(null, [| value |])

                    printfn "res is: "
                    printfn "%A" res
                    res :?> SqlType


            DTO<'T>.toColumnName p.Name, final_v
        )
        |> Array.toList


    static member SqlInsertOut : string =
        //let table = "users"
        let table = typeof<'T>.Name.ToLower()
        let fields = String.Join(", ", DTO<'T>.genSqlColumns(includePK = false))
        let valParams = String.Join(", ", DTO<'T>.genSqlValues(includePK = false))
        $"Insert into public.{table} 
          ({fields})
          values
          ({valParams})
          RETURNING *"

    static member SqlUpdateOut : string =
        //let table = "users"
        let table = typeof<'T>.Name.ToLower()
        let fields = String.Join(", ", DTO<'T>.genSqlColumns(includePK = false))
        let vals = String.Join(", ", DTO<'T>.genSqlValues(includePK = false))
        let kwargs = String.Join(", ", DTO<'T>.genSqlKVPairs(includePK = false))
        $"Update public.{table} 
          set {kwargs}
          where
          id = @id
          RETURNING *"

    static member SqlDeleteOne : string =
        let table = typeof<'T>.Name.ToLower()
        $"Delete
          from public.{table}
          where
          id = @id"