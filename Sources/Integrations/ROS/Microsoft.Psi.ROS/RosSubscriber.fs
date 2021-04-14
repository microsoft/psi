// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosSubscriber =

    open RosMessage
    open RosMasterClient
    open RosNodeClient
    open RosTcp
    open System
    open System.IO
    open System.Text
    open System.Threading
    open System.Net.Sockets
    open System.Diagnostics

    (* A Subscriber represents an entity that's interesting in a particular topic.
       It carries with it information about the message type (MD5 hash, etc.) and
       the callback function to be called.

       At some point it will Subscribe(..) to the topic, once a publisher is known.
       This may be immediately upon a Node registering or later upon a publisherUpdate.
       It may also re-subscribe to a different publisher upon a publisherUpdate. *)

    type ISubscriber =
        abstract member Subscribe : string -> unit
        abstract member Unsubscribe : unit -> unit

    type Subscriber(message: RosMessage.MessageDef, topic, caller, callback: Action<NamedRosFieldVal seq>, rosMaster: RosMasterClient, rosMasterIp: string) =
        let mutable publishers = []
        let mutable active = true
        interface ISubscriber with
            member x.Subscribe(publisher: string) =
                if not (List.contains publisher publishers) then // don't resubscribe
                    publishers <- publisher :: publishers
                    let listen () =
                        try
                            sprintf "  Publisher: %s" publisher |> Trace.WriteLine
                            let client = new RosNodeClient(RosDns.resolve rosMasterIp publisher, caller)
                            let host, port = client.RequestTopic(topic, ["TCPROS"])
                            sprintf "Connecting: %s %i (%s)" host port topic |> Trace.WriteLine
                            let tcp = new TcpClient(RosDns.resolve rosMasterIp host, port)
                            tcp.NoDelay <- true
                            let stream = tcp.GetStream()
                            writeHeaders stream [
                                "callerid", caller
                                "type", message.Type
                                "md5sum", message.MD5
                                "tcp_nodelay", "1"
                                "topic", topic]
                            readHeaders stream |> ignore
                            let reader = new BinaryReader(stream, Encoding.UTF8)
                            while active do
                                let bytes = reader.ReadBytes(reader.ReadInt32())
                                try
                                    deserialize bytes message.Fields |> callback.Invoke
                                with ex -> sprintf "Deserialization error: %s (%s) %A" ex.Message topic bytes |> Trace.TraceError
                        with ex ->
                            sprintf "Connection Error: %s (%s)" ex.Message topic |> failwith
                    (new Thread(new ThreadStart(listen), IsBackground=true)).Start()
            member x.Unsubscribe() = active <- false