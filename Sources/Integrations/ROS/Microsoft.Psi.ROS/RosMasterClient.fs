// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosMasterClient =

    open XmlRpc
    open RosRpc

    // Communication with ROS master via XmlRpc methods (http://wiki.ros.org/ROS/Master_API)

    type SystemStateCoupling = (string * string list) list
    type SystemState = { Publishers:  SystemStateCoupling
                         Subscribers: SystemStateCoupling
                         Services:    SystemStateCoupling }

    type RosMasterClient(uri, caller) =
        member x.GetUri() = requestString uri "getUri" [caller]
        member x.LookupNode name = requestString uri "lookupNode" [caller; name]
        member x.LookupService name = requestString uri "lookupService" [caller; name]
        member x.GetTopicTypes() = requestStringPairs uri "getTopicTypes" [caller]
        member x.GetPublishedTopics subgraph = requestStringPairs uri "getPublishedTopics" [caller; subgraph]

        member x.RegisterService(service, serviceApi, callerApi) = requestStringArray uri "registerService" [caller; service; serviceApi; callerApi]
        member x.UnregisterService(service, serviceApi) = requestInt uri "unregisterService" [caller; service; serviceApi]

        member x.RegisterSubscriber(topic, typ, callerApi) = requestStringArray uri "registerSubscriber" [caller; topic; typ; callerApi]
        member x.UnregisterSubscriber(topic, callerApi) = requestInt uri "unregisterSubscriber" [caller; topic; callerApi]

        member x.RegisterPublisher(topic, typ, callerApi) = requestStringArray uri "registerPublisher" [caller; topic; typ; callerApi]
        member x.UnregisterPublisher(topic, callerApi) = requestInt uri "unregisterPublisher" [caller; topic; callerApi]

        member x.GetSystemState() =
            match request uri "getSystemState" [caller] with
            | Array [Array publishers; Array subscribers; Array services] ->
                let coupling = function
                    | Array [String name; Array bindings] -> name, List.map (function String s -> s | x -> failwith_unexpected x) bindings
                    | x -> failwith_unexpected x
                { Publishers  = List.map coupling publishers
                  Subscribers = List.map coupling subscribers
                  Services    = List.map coupling services }
            | x -> failwith_unexpected x