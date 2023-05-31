// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosServiceTypes =

    open System
    open RosMessage

    module Standard =

        module Empty =
            let Def = { Type = "std_srvs/Empty"
                        MD5  = "d41d8cd98f00b204e9800998ecf8427e"
                        CallFields = []
                        ReturnFields = [] }
            type Kind = unit
            let ToCall () = Seq.empty<NamedRosFieldVal>
            let FromReturn () = ()