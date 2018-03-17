// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosDns =

    open System.Text.RegularExpressions

    let mutable known = Map.empty<string, string>

    let add host ip = known <- Map.add host ip known

    let hostRegex = new Regex("http://([^:]+):", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

    let resolve rosMaster (uri: string) =
        let host = if uri.StartsWith("http://") then hostRegex.Match(uri).Groups.[1].Value else uri
        match Map.tryFind host known with
        | Some found -> uri.Replace(host, found)
        | None -> uri.Replace(host, rosMaster) // default