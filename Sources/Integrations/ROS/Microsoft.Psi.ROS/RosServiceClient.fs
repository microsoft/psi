// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosServiceClient =

    open System
    open System.IO
    open System.Net
    open System.Net.Sockets
    open RosTcp
    open RosMessage

    type IServiceClient =
        abstract member Call : NamedRosFieldVal seq -> NamedRosFieldVal seq

    type ServiceClient(service, name, address: string, caller) =
        interface IServiceClient with
            member x.Call(value) =
                let client = new TcpClient()
                client.NoDelay <- true
                match address.Replace("rosrpc://", "").Split(':') with
                | [| host; port |] ->
                    match System.Int32.TryParse port with
                    | true, port ->
                        client.Connect(host, port)
                        let stream = client.GetStream()
                        writeHeaders stream ["service", name; "type", service.Type; "md5sum", service.MD5; "callerid", caller]
                        readHeaders stream |> ignore
                        let writer = new BinaryWriter(stream)
                        let msg = serialize value
                        writer.Write(msg.Length)
                        writer.Write(msg)
                        writer.Flush()
                        writer.BaseStream.Flush()
                        let reader = new BinaryReader(stream)
                        let status = reader.ReadByte()
                        let len = reader.ReadInt32()
                        let bytes = reader.ReadBytes(len)
                        if status = 1uy then deserialize bytes service.ReturnFields |> Seq.ofList
                        else deserialize bytes ["data", StringDef] |> sprintf "Service error: %A" |> failwith
                    | false, _ -> sprintf "Could not parse port: %s (%s)" port address |> failwith
                | _ -> sprintf "Could not parse address: %s" address |> failwith