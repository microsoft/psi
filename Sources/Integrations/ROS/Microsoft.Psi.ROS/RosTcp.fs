// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosTcp =

    open System.IO
    open System.Text

    (*
    ROS headers are used in ROSTCP/UDP communication. They are a length-prefixed sequence of length-prefixed strings, each of the form "<name>=<value>".
    The first four bytes are the (little-endian) length of the header section (not including the four bytes themselves)
    Followed by a four-byte string length and a UTF8 encoded string in the form "<name>=<value>"
    BinaryReader/Writer handles strings by a variable length prefix value. So we have to encode to a byte array and write the 4-byte length ourselves.

    See: http://wiki.ros.org/ROS/Connection%20Header - though, misleading example in that header and body sections are not actually sent *together*
    *)

    // write sequence of name/value pairs as headers
    let writeHeaders (stream: Stream) (headers: (string * string) seq) =
        let header = new MemoryStream()
        let writer = new BinaryWriter(header, Encoding.UTF8)
        let writeString (str: string) =
            let chars = Encoding.UTF8.GetBytes(str)
            writer.Write(chars.Length)
            writer.Write(chars)
        writer.Write(0) // header length (patched later)
        headers |> Seq.iter (fun (n, v) -> sprintf "%s=%s" n v |> writeString)
        let len = int header.Position
        header.Position <- 0L; len - 4 |> writer.Write // patch header length
        stream.Write(header.GetBuffer(), 0, len)
        stream.Flush()

    // read sequence of name/value pairs from stream
    let readHeaders (stream: Stream) =
        let buffer = Array.create 8192 0uy
        let reader = new BinaryReader(stream, Encoding.UTF8)
        let rec readHeaders' length = seq {
            let rec readBytes index count =
                let len = reader.Read(buffer, index, count)
                if len < count then readBytes (index + len) (count - len)
            let len = reader.ReadInt32()
            readBytes 0 len
            let s = Encoding.UTF8.GetString(buffer, 0, len)
            let i = s.IndexOf('=') // *first* '=' - value itself may have unescaped '='
            if i = -1 then failwith "Malformed header (expected name=value)"
            yield s.Substring(0, i), s.Substring(i + 1)
            let length' = length - len - 4
            if length' > 0 then yield! readHeaders' length' }
        reader.ReadInt32() |> readHeaders' |> List.ofSeq |> Seq.ofList // force eager reading, but return as lazy seq