// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosMessage =

    open System
    open System.Collections.Generic
    open System.Dynamic
    open System.Text
    open System.IO

    // primitives (definition and value structure)

    type RosFieldDef =
        | BoolDef | Int8Def | UInt8Def | Int16Def | UInt16Def | Int32Def | UInt32Def | Int64Def | UInt64Def | Float32Def | Float64Def | TimeDef | DurationDef | StringDef
        | VariableArrayDef of RosFieldDef // length prefixed
        | FixedArrayDef of int * RosFieldDef // fixed (known) length
        | StructDef of NamedRosFieldDef list
    and NamedRosFieldDef = string * RosFieldDef

    type RosFieldVal =
        | BoolVal          of bool // introduced in ROS 0.9
        | Int8Val          of int8 // deprecated alias: byte
        | UInt8Val         of uint8 // deprecated alias: char
        | Int16Val         of int16
        | UInt16Val        of uint16
        | Int32Val         of int32
        | UInt32Val        of uint32
        | Int64Val         of int64
        | UInt64Val        of uint64
        | Float32Val       of single
        | Float64Val       of float
        | TimeVal          of uint32 * uint32 // secs/nsecs
        | DurationVal      of int32 * int32 // secs/nsecs
        | StringVal        of string
        | VariableArrayVal of RosFieldVal list
        | FixedArrayVal    of RosFieldVal list
        | StructVal        of NamedRosFieldVal list
    and NamedRosFieldVal  = string * RosFieldVal

    let GetBoolVal          = function BoolVal          v -> v           | _ -> failwith "Expected Bool value"
    let GetInt8Val          = function Int8Val          v -> v           | _ -> failwith "Expected Int8 value"
    let GetUInt8Val         = function UInt8Val         v -> v           | _ -> failwith "Expected UInt8 value"
    let GetInt16Val         = function Int16Val         v -> v           | _ -> failwith "Expected Int16 value"
    let GetUInt16Val        = function UInt16Val        v -> v           | _ -> failwith "Expected UInt16 value"
    let GetInt32Val         = function Int32Val         v -> v           | _ -> failwith "Expected Int32 value"
    let GetUInt32Val        = function UInt32Val        v -> v           | _ -> failwith "Expected UInt32 value"
    let GetInt64Val         = function Int64Val         v -> v           | _ -> failwith "Expected Int64 value"
    let GetUInt64Val        = function UInt64Val        v -> v           | _ -> failwith "Expected UInt64 value"
    let GetFloat32Val       = function Float32Val       v -> v           | _ -> failwith "Expected Float32 value"
    let GetFloat64Val       = function Float64Val       v -> v           | _ -> failwith "Expected Float64 value"
    let GetTimeVal          = function TimeVal          (s, n) -> (s, n) | _ -> failwith "Expected Time value"
    let GetDurationVal      = function DurationVal      (s, n) -> (s, n) | _ -> failwith "Expected Duration value"
    let GetStringVal        = function StringVal        v -> v           | _ -> failwith "Expected String value"
    let GetVariableArrayVal = function VariableArrayVal v -> v           | _ -> failwith "Expected variable array value"
    let GetFixedArrayVal    = function FixedArrayVal    v -> v           | _ -> failwith "Expected fixed array value"
    let GetStructVal        = function StructVal        v -> v           | _ -> failwith "Expected struct value"

    let rec GetDynamicFieldVals (fields : NamedRosFieldVal seq) =
        let expando = ExpandoObject()
        let dict = expando :> IDictionary<string, obj>
        fields |> Seq.iter (fun (n, v) -> dict.Add(n, GetDynamicVal v))
        box expando
    and GetDynamicVal = function
        | BoolVal          v -> box v
        | Int8Val          v -> box v
        | UInt8Val         v -> box v
        | Int16Val         v -> box v
        | UInt16Val        v -> box v
        | Int32Val         v -> box v
        | UInt32Val        v -> box v
        | Int64Val         v -> box v
        | UInt64Val        v -> box v
        | Float32Val       v -> box v
        | Float64Val       v -> box v
        | TimeVal          (s, n) -> box (s, n)
        | DurationVal      (s, n) -> box (s, n)
        | StringVal        v -> box v
        | VariableArrayVal v -> v |> Array.ofList |> box
        | FixedArrayVal    v -> v |> Array.ofList |> box
        | StructVal        v -> GetDynamicFieldVals v

    // serdes

    let rec readField (reader: BinaryReader) = function
        | BoolDef     -> reader.ReadByte() <> 0uy |> BoolVal
        | Int8Def     -> reader.ReadSByte()       |> Int8Val
        | UInt8Def    -> reader.ReadByte()        |> UInt8Val
        | Int16Def    -> reader.ReadInt16()       |> Int16Val
        | UInt16Def   -> reader.ReadUInt16()      |> UInt16Val
        | Int32Def    -> reader.ReadInt32()       |> Int32Val
        | UInt32Def   -> reader.ReadUInt32()      |> UInt32Val
        | Int64Def    -> reader.ReadInt64()       |> Int64Val
        | UInt64Def   -> reader.ReadUInt64()      |> UInt64Val
        | Float32Def  -> reader.ReadSingle()      |> Float32Val
        | Float64Def  -> reader.ReadDouble()      |> Float64Val
        | TimeDef     -> (reader.ReadUInt32(), reader.ReadUInt32()) |> TimeVal
        | DurationDef -> (reader.ReadInt32(),  reader.ReadInt32())  |> DurationVal
        | StringDef   -> reader.ReadBytes(reader.ReadInt32()) |> Encoding.UTF8.GetString |> StringVal
        | VariableArrayDef t     -> [for _ in 1 .. reader.ReadInt32() -> readField reader t] |> VariableArrayVal
        | FixedArrayDef (len, t) -> [for _ in 1 .. len                -> readField reader t] |> FixedArrayVal
        | StructDef m -> List.map (fun (n, t) -> n, readField reader t) m |> StructVal
    let readMessage reader = List.map (fun (n, d) -> n, readField reader d)

    let rec writeField (writer: BinaryWriter) = function
        | BoolVal      b -> writer.Write(if b then 255uy else 0uy)
        | Int8Val      i -> writer.Write(i)
        | UInt8Val     i -> writer.Write(i)
        | Int16Val     i -> writer.Write(i)
        | UInt16Val    i -> writer.Write(i)
        | Int32Val     i -> writer.Write(i)
        | UInt32Val    i -> writer.Write(i)
        | Int64Val     i -> writer.Write(i)
        | UInt64Val    i -> writer.Write(i)
        | Float32Val   f -> writer.Write(f)
        | Float64Val   f -> writer.Write(f)
        | TimeVal     (s, n) -> writer.Write(s); writer.Write(n)
        | DurationVal (s, n) -> writer.Write(s); writer.Write(n)
        | StringVal    s ->
            let str = Encoding.UTF8.GetBytes(s)
            writeField writer (Int32Val str.Length)
            writer.Write(str)
        | VariableArrayVal a ->
            writeField writer (Int32Val a.Length)
            List.iter (writeField writer) a
        | FixedArrayVal a -> List.iter (writeField writer) a
        | StructVal s -> List.iter (snd >> writeField writer) s

    let writeMessage writer = Seq.iter (snd >> writeField writer)

    let serialize message =
        let ms = new MemoryStream()
        let writer = new BinaryWriter(ms)
        writeMessage writer message
        ms.GetBuffer().[0..int ms.Position - 1]

    let deserialize (bytes: byte[]) =
        new BinaryReader(new MemoryStream(bytes)) |> readMessage

    // util

    let toDateTime (sec: uint32) (nsec: uint32) = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(float sec).AddMilliseconds(float nsec / 1000000.0)
    let fromDateTime (dt: DateTime) =
        let sec = dt.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds
        let nsec = (sec - Math.Truncate(sec)) * 1000000000.0
        uint32 sec, uint32 nsec

    let toTimeSpan sec nsec = (new TimeSpan(0, 0, 0, sec, nsec / 1000000))
    let fromTimeSpan (ts: TimeSpan) =
        let sec = ts.TotalSeconds
        let nsec = (sec - Math.Truncate(sec)) * 1000000000.0
        int sec, int nsec
        
    type MessageDef = { Type:   string
                        MD5:    string
                        Fields: NamedRosFieldDef list }

    let CreateMessageDef typ md5 fields = { Type   = typ
                                            MD5    = md5
                                            Fields = List.ofSeq fields }

    type ServiceDef = { Type:         string
                        MD5:          string
                        CallFields:   NamedRosFieldDef list
                        ReturnFields: NamedRosFieldDef list }

    let CreateServiceDef typ md5 callFields returnFields = { Type         = typ
                                                             MD5          = md5
                                                             CallFields   = List.ofSeq callFields
                                                             ReturnFields = List.ofSeq returnFields }
                                                             
    let CreateStructDef def = StructDef (def.Fields)
    
    let CreateStructVal fields = StructVal (List.ofSeq fields)

    let CreateVariableArrayVal vals = VariableArrayVal (List.ofSeq vals)

    let CreatFixedArrayVal vals = FixedArrayVal (List.ofSeq vals)

    let malformed () = failwith "Malformed message structure"
