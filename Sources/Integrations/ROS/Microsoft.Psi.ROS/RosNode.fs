// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosNode =

    open System
    open System.Text
    open System.Threading
    open System.Net
    open System.Net.Sockets
    open System.IO
    open System.Diagnostics
    open XmlRpc
    open RosTcp
    open RosMasterClient
    open RosParamClient
    open RosPublisher
    open RosSubscriber
    open RosServiceClient
    open RosMessage

    // Communication *as* a ROS node via XmlRpc methods (http://wiki.ros.org/ROS/Slave_API)

    type Node(name, host, rosMasterIp, rosMasterPort) =
        let rosMasterUri = sprintf "http://%s:%i" rosMasterIp rosMasterPort
        let rosMaster = new RosMasterClient(rosMasterUri, name)
        let mutable publishers = Map.empty<string, IPublisher> // topic -> publisher
        let mutable subscribers = Map.empty<string, ISubscriber> // topic -> subscriber
        let error message = Array [Integer -1; String message; Nil]
        let ret value = Array [Integer 1; String "ok"; value]
        let port = XmlRpc.xmlRpcListen (fun name args ->
            match name, args with
            | "getBusStats", args -> error "getBusStats NYI"
            | "getBusInfo", args -> error "getBusInfo NYI"
            | "getMasterUri", args -> String rosMasterUri
            | "shutdown", args -> error "shutdown NYI"
            | "getPid", args -> error "getPid NYI"
            | "getSubscriptions", args -> error "getSubscriptions NYI"
            | "getPublications", args -> error "getPublications NYI"
            | "paramUpdate", [String caller; String key; value] -> error "paramUpdate NYI"
            | "publisherUpdate", args ->
                sprintf "Publisher Update %A" args |> Trace.WriteLine
                match args with
                | [String caller; String topic; Array publishers] ->
                    match Map.tryFind topic subscribers with
                    | Some current ->
                        publishers
                        |> List.map (function String uri -> uri | _ -> failwith "Malformed publisher update (expected String)")
                        |> List.iter (fun pub -> current.Subscribe(pub))
                        Nil
                    | None -> error "Publisher update for a topic to which we've never subscribed?!"
                | _ -> error "Malformed publisher update"
            | "requestTopic", [String caller; String topic; Array protocols] ->
                sprintf "Request Topic: %s (%s) - %A" topic caller protocols |> Trace.WriteLine
                let protos = protocols
                             |> List.map (function Array ps -> List.map (function String s -> s | _ -> failwith "Malformed protocol spec") ps | _ -> failwith "Malformed protocol list")
                             |> List.filter (List.contains "TCPROS")
                if List.length protos = 0 then error "Only TCPROS supported" else
                    match Map.tryFind topic publishers with
                    | Some pub ->
                        let port = pub.CreateChannel()
                        ret (Array [String "TCPROS"; String host; Integer port])
                    | None -> sprintf "Unknown topic: %s" topic |> error
            | name, args -> sprintf "Unsupported method: %s" name |> error)
        let uri = sprintf "http://%s:%i" host port
        new (name, host, rosMasterIp) = Node(name, host, rosMasterIp, 11311) // default port
        member x.Param = new ParamClient(rosMasterUri, name, uri)
        member x.CreatePublisher(message, topic, latching) =
            let pub = new Publisher(message, topic, latching, rosMaster)
            publishers <- Map.add topic (pub :> IPublisher) publishers
            match rosMaster.RegisterPublisher(topic, message.Type, uri) with 
            | subscriber :: _ -> sprintf "Subscriber: %s" subscriber |> Trace.WriteLine
            | _ -> sprintf "No subscribers" |> Trace.WriteLine
            pub
        member x.UnregisterPublisher(topic) = rosMaster.UnregisterPublisher(topic, uri)
        member x.Subscribe(message: RosMessage.MessageDef, topic, callback: Action<NamedRosFieldVal seq>) =
            let publishers = rosMaster.RegisterSubscriber(topic, message.Type, uri)
            let sub = new Subscriber(message, topic, name, callback, rosMaster, rosMasterIp) :> ISubscriber
            List.iter sub.Subscribe publishers
            subscribers <- Map.add topic sub subscribers
            sub
        member x.UnregisterSubscriber(topic) = rosMaster.UnregisterSubscriber(topic, uri)
        member x.CreateServiceClient(service, name, latching) = // note: latching unsupported
            let publisher = rosMaster.LookupService(name)
            let client = new ServiceClient(service, name, RosDns.resolve rosMasterIp publisher, name)
            client
        member x.UnregisterService(service) = rosMaster.UnregisterService(service, uri)