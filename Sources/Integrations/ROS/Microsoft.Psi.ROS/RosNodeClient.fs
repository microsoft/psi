// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosNodeClient =

    open XmlRpc
    open RosRpc

    // Communication with ROS nodes via XmlRpc methods (http://wiki.ros.org/ROS/Slave_API)
    // Note: `paramUpdate` and `publisherUpdate` are unsupported (should only come from ROS master)

    type BusDirection = In | Out | Both
    type BusInfo = { Id: XmlRpcValue // opaque - defined by node
                     Destination: string
                     Direction: BusDirection
                     Transport: string
                     Topic: string
                     Extra: XmlRpcValue list } // may contain connection info (Python library only ATM)

    type RosNodeClient(uri, caller) =
        member x.GetBusStats() =
            match request uri "getBusStats" [caller] with
            | Array x -> x
            | x -> failwith_unexpected x
        member x.GetBusInfo() =
            match request uri "getBusInfo" [caller] with
            | Array connections ->
                let toDir = function "i" -> In | "o" -> Out | "b" -> Both | x -> sprintf "Invalid connection direction '%s'" x |> failwith
                let toConn = function
                    | Array (id :: String destination :: String dir :: String transport :: String topic :: extra) ->
                        { Id = id
                          Destination = destination
                          Direction = toDir dir
                          Transport = transport
                          Topic = topic
                          Extra = extra }
                    | x -> failwith_unexpected x
                connections |> List.map toConn
            | x -> failwith_unexpected x
        member x.GetRosMasterUri() = requestString uri "getMasterUri" [caller]
        member x.Shutdown message = requestUnit uri "shutdown" [caller; message]
        member x.GetPid() = requestInt uri "getPid" [caller]
        member x.GetPublications() = requestStringPairs uri "getPublications" [caller]
        member x.GetSubscriptions() = requestStringPairs uri "getSubscriptions" [caller]
        member x.RequestTopic(topic, protocols) =
            let protos = List.map (fun p -> String p) protocols |> Array
            let args = [String caller; String topic; Array [protos]]
            match requestWithRawArgs uri "requestTopic" args with
            | Array [String "TCPROS"; String host; Integer port] -> host, port
            | x -> failwith_unexpected x // UDPROS unsupported
