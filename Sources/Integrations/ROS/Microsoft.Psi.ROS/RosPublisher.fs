// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#nowarn "40" // recursive type

namespace Microsoft.Ros

module RosPublisher =

    open System
    open System.Text
    open System.Threading
    open System.Net
    open System.Net.Sockets
    open System.IO
    open System.Diagnostics
    open RosTcp
    open RosMasterClient
    open RosMessage

    type IPublisher =
        abstract member CreateChannel : unit -> int // TCP port
        abstract member Publish : NamedRosFieldVal seq -> unit

    type Publisher(message: MessageDef, topic, latching, rosMaster : RosMasterClient) =
        let mutable channels = List.empty
        interface IPublisher with
            member x.CreateChannel() =
                let listener = new TcpListener(IPAddress.Any, 0)
                listener.Server.NoDelay <- true
                listener.Start()
                let port = (listener.LocalEndpoint :?> IPEndPoint).Port
                let mutable (writer : BinaryWriter option) = None
                let mutable (last : byte[] option) = None // last message for latching
                let rec processor = MailboxProcessor<byte[]>.Start(fun box ->
                    async {
                        try
                            while true do
                                let! msg = box.Receive()
                                match writer with
                                | Some writer ->
                                    writer.Write(msg.Length)
                                    writer.Write(msg)
                                    writer.Flush()
                                    writer.BaseStream.Flush()
                                | None ->
                                    Trace.WriteLine "Not listening yet"
                                    () // not listening yet
                        with ex ->
                            sprintf "Connection Error: %s (%s)" ex.Message topic |> Trace.TraceError
                            channels <- List.filter ((<>) processor) channels } )
                channels <- processor :: channels
                let listen () =
                    try
                        let buffer = Array.create 8192 0uy
                        sprintf "Waiting on Port %i" port |> Trace.WriteLine
                        let client = listener.AcceptTcpClient()
                        sprintf "Client on Port %i" port |> Trace.WriteLine
                        let stream = client.GetStream()
                        readHeaders stream |> ignore
                        writeHeaders stream ["type", message.Type; "md5sum", message.MD5; "latching", if latching then "1" else "0"]
                        writer <- Some (new BinaryWriter(stream, Encoding.UTF8))
                    with ex ->
                        sprintf "Connection Error: %s (%s)" ex.Message topic |> Trace.TraceError
                        channels <- List.filter ((<>) processor) channels
                (new Thread(new ThreadStart(listen), IsBackground=true)).Start()
                port
            member x.Publish(msg: NamedRosFieldVal seq) = channels |> Seq.iter (fun (chan: MailboxProcessor<byte[]>) -> chan.Post(serialize msg))