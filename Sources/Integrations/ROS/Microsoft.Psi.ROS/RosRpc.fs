// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosRpc =

    open XmlRpc

    let failwith_unexpected x = sprintf "Unexpected: %A" x |> failwith

    let requestWithRawArgs uri meth args =
        match xmlRpcRequest uri meth args with
        | Array [Integer code; String status; ret] -> if code <> 1 then sprintf "Error response: %s (%i)" status code |> failwith else ret
        | x -> failwith_unexpected x

    let request uri meth args = requestWithRawArgs uri meth (List.map (fun a -> String a) args)

    let requestString uri meth args = match request uri meth args with String  s -> s | x -> failwith_unexpected x
    let requestInt    uri meth args = match request uri meth args with Integer n -> n | x -> failwith_unexpected x
    let requestBool   uri meth args = match request uri meth args with Boolean b -> b | x -> failwith_unexpected x
    let requestArray  uri meth args = match request uri meth args with Array   a -> a | x -> failwith_unexpected x

    let requestUnit   uri meth args = request uri meth args |> ignore

    let requestStringOption uri meth args =
        try
            match request uri meth args with String s -> Some s | _ -> None
        with _ -> None // failure status code means "not found"

    let requestStringArray uri meth args = requestArray uri meth args |> List.map (function String s -> s | x -> failwith_unexpected x)

    let requestStringPairs uri meth args =
        match request uri meth args with
        | Array pairs -> List.map (function Array [String name; String typ] -> name, typ | x -> failwith_unexpected x) pairs
        | x -> failwith_unexpected x