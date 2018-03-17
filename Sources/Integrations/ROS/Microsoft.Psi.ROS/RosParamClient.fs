// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosParamClient =

    open XmlRpc
    open RosRpc

    // Communication with ROS parameter server via XmlRpc methods (http://wiki.ros.org/ROS/Parameter%20Server%20API)

    type ParamClient(uri, caller, callerUri) =
        member x.Set(key, value) = requestWithRawArgs uri "setParam"         [String caller; String key; value] |> ignore
        member x.Delete      key = requestUnit        uri "deleteParam"      [caller; key]
        member x.Has         key = requestBool        uri "hasParam"         [caller; key]
        member x.Search      key = requestString      uri "searchParam"      [caller; key]
        member x.GetNames()      = requestStringArray uri "getParamNames"    [caller]
        member x.Subscribe   key = request            uri "subscribeParam"   [caller; callerUri; key]
        member x.Unsubscribe key = requestInt         uri "unsubscribeParam" [caller; callerUri; key]
        member x.Get         key = request            uri "getParam"         [caller; key]
        member x.GetArray    key = match x.Get key with Array    x -> x  | x -> sprintf "Expected Array (got %A)"    x |> failwith
        member x.GetBase64   key = match x.Get key with Base64   x -> x  | x -> sprintf "Expected Base64 (got %A)"   x |> failwith
        member x.GetBoolean  key = match x.Get key with Boolean  x -> x  | x -> sprintf "Expected Boolean (got %A)"  x |> failwith
        member x.GetDateTime key = match x.Get key with DateTime x -> x  | x -> sprintf "Expected DateTime (got %A)" x |> failwith
        member x.GetDouble   key = match x.Get key with Double   x -> x  | x -> sprintf "Expected Double (got %A)"   x |> failwith
        member x.GetInteger  key = match x.Get key with Integer  x -> x  | x -> sprintf "Expected Integer (got %A)"  x |> failwith
        member x.GetString   key = match x.Get key with String   x -> x  | x -> sprintf "Expected String (got %A)"   x |> failwith
        member x.GetStruct   key = match x.Get key with Struct   x -> x  | x -> sprintf "Expected Struct (got %A)"   x |> failwith
        member x.GetNil      key = match x.Get key with Nil        -> () | x -> sprintf "Expected Nil (got %A)"      x |> failwith