// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module XmlRpc =

    open System
    open System.Net.Sockets
    open System.IO
    open System.Text
    open System.Threading
    open System.Xml
    open System.Globalization
    open System.Net

    (*
    XmlRpc is a very simple protocol built atop HTTP and XML encoding.
    It consists of a `methodCall` POSTed over HTTP to a given host/port, followed by a `methodResponse`.
    ROS uses this for communication with the ROS Master, the Parameter Server and with Nodes. Everything except negotiating the messaging channels and the messages themselves.
    For example, to get the URI of the ROS Master:

        <?xml version="1.0"?>
        <methodCall>
          <methodName>getUri</methodName>
          <params>
            <param>
              <value>username</value>
            </param>
          </params>
        </methodCall>

        <?xml version='1.0'?>
        <methodResponse>
          <params>
            <param>
              <value>
                <array>
                  <data>
                    <value>
                      <int>1</int>
                    </value>
                    <value>
                      <string></string>
                    </value>
                    <value>
                      <string>http://my-dev-box-ubuntu:11311/</string>
                    </value>
                  </data>
                </array>
              </value>
            </param>
          </params>
        </methodResponse>

    There are encodings for the following types: `array`, `base64`, `boolean`, `dateTime.iso8601`, `double`, `integer`, `string`, `struct`, `nil`. A `<value>` element without a type is assumed to be a `string`. Structs have name/value pairs (`<member><name>...</name><value>...</value></member>`). Arrays contain `<data>...</data>` with a list of anonymous values.

    Atop this simple encoding, [ROS has a request/response format](http://wiki.ros.org/ROS/Master_Slave_APIs). Requests contain a `caller_id` as the first parameter, followed by zero-or more arguments. Responses containing an array of three values: an `int` (status code), `string` (status message - may be null), and a body value of any type. Below, we'll refer only to the parameters other than `caller_id` and to just the response body portion.

    Everyone seems to want to make complicated "frameworks" around this very simple XmlRpc protocol. Here's my 200-line reader/writer pair. That's all we need...

    See also: https://en.wikipedia.org/wiki/XML-RPC 
    *)

    // these are the types of values that may be encoded. Note that Array and Struct recursively use XmlRpcValue and so can build arbitrarily complex names or anonymous structure.
    type XmlRpcValue =
        | Array    of XmlRpcValue list
        | Base64   of byte[]
        | Boolean  of bool
        | DateTime of DateTime
        | Double   of float
        | Integer  of int
        | String   of string
        | Struct   of (string * XmlRpcValue) list
        | Nil

    // This is a writer, akin to the XmlTextWriter but adding understanding of XmlRpc value encodings and structure of methodCall/Response
    type XmlRpcWriter(stream: Stream) =
        let writer = new XmlTextWriter(stream, Encoding.UTF8)

        member x.WriteValue (value: XmlRpcValue) =
            writer.WriteStartElement("value")
            match value with
            | Array vals ->
                writer.WriteStartElement("array")
                writer.WriteStartElement("data")
                for v in vals do x.WriteValue v
                writer.WriteEndElement() // data
                writer.WriteEndElement() // array
            | Base64 bytes -> writer.WriteElementString("base64", Convert.ToBase64String(bytes))
            | Boolean b -> writer.WriteElementString("boolean", if b then "1" else "0")
            | DateTime d -> writer.WriteElementString("dateTime.iso8601", d.ToUniversalTime().ToString("s", CultureInfo.InvariantCulture))
            | Double d -> writer.WriteElementString("double", d.ToString(CultureInfo.InvariantCulture))
            | Integer i -> writer.WriteElementString("i4", i.ToString(CultureInfo.InvariantCulture))
            | String s -> writer.WriteString(s) // writer.WriteElementString("string", s)
            | Struct fields ->
                writer.WriteStartElement("struct")
                for (n, v) in fields do
                    writer.WriteStartElement("member")
                    writer.WriteElementString("name", n)
                    x.WriteValue(v)
                    writer.WriteEndElement() // member
                writer.WriteEndElement() // struct
            | Nil ->
                writer.WriteStartElement("nil")
                writer.WriteEndElement()
            writer.WriteEndElement() // value

        member x.WriteParams values =
            if Seq.length values > 0 then
                writer.WriteStartElement("params")
                for v in values do
                    writer.WriteStartElement("param")
                    x.WriteValue v
                    writer.WriteEndElement() // param
                writer.WriteEndElement() // params

        member x.WriteMethodCall name parameters =
            writer.WriteStartDocument()
            writer.WriteStartElement("methodCall")
            writer.WriteElementString("methodName", name)
            x.WriteParams parameters
            writer.WriteEndElement() // methodCall
            writer.WriteEndDocument()
            writer.Flush()

        member x.WriteMethodResponse value =
            writer.WriteStartDocument()
            writer.WriteStartElement("methodResponse")
            x.WriteParams [value]
            writer.WriteEndElement() // methodResponse
            writer.WriteEndDocument()
            writer.Flush()

    // Symetrically, this is a reader akin to XmlTextReader but with understanding of XmlRpc value encodings and structure of methodCall/Response
    type XmlRpcReader(stream: Stream) =
        let reader = new XmlTextReader(stream)
        let readToNamed name pred =
            while reader.Read() && not reader.EOF && pred () do ()
            if name <> "" && reader.Name <> name then sprintf "Malformed XmlRpc response - expected <%s>" name |> failwith
        let readTo = readToNamed ""
        let startElement () = reader.NodeType <> XmlNodeType.Element
        let endElement () = reader.NodeType <> XmlNodeType.EndElement
        let element () = startElement () && endElement ()
        let startElementOrText () = startElement () && reader.NodeType <> XmlNodeType.Text

        member x.ReadValue() =
            readTo startElementOrText
            let value = match reader.Name with
                        | "array" ->
                            let rec readArray elements =
                                if reader.Name = "value" then
                                    x.ReadValue() :: elements |> readArray
                                else
                                    readTo endElement
                                    List.rev elements
                            (if reader.ReadToDescendant("value") then readArray [] else []) |> Array
                        | "base64" -> reader.ReadElementContentAsString() |> Convert.FromBase64String |> Base64
                        | "boolean" -> reader.ReadElementContentAsInt() = 1 |> Boolean
                        | "dateTime.iso8601" -> reader.ReadElementContentAsDateTime() |> DateTime
                        | "double" -> reader.ReadElementContentAsDouble() |> Double
                        | "int" | "i4" -> reader.ReadElementContentAsInt() |> Integer
                        | "string" -> reader.ReadElementContentAsString() |> String
                        | "value" | "" -> reader.Value |> String // <value> without <string> defaults to String
                        | "struct" ->
                            let rec readStruct fields =
                                if reader.Name = "member" then
                                    readToNamed "name" startElement
                                    let name = reader.ReadElementContentAsString()
                                    let value = x.ReadValue()
                                    readTo element
                                    (name, value) :: fields |> readStruct
                                else List.rev fields |> Struct
                            readTo startElement
                            readStruct []
                        | "nil" -> Nil
                        | unexpected -> sprintf "Malformed XmlRpc - unexpected value type '%s'" unexpected |> failwith
            if reader.Name <> "value" || reader.NodeType <> XmlNodeType.Element then
                readTo (fun () -> (reader.NodeType <> XmlNodeType.EndElement || reader.Name = "value") && reader.NodeType <> XmlNodeType.Element)
            value

        member x.ReadMethodCall() =
            if not (reader.ReadToDescendant("methodName")) then failwith "Malformed XmlRpc - expected <methodName>"
            let name = reader.ReadElementContentAsString()
            let rec readParams values =
                if not (reader.Read()) || reader.EOF || (reader.Name = "methodCall" && reader.NodeType = XmlNodeType.EndElement) then name, List.rev values
                elif reader.Name = "value" && reader.NodeType = XmlNodeType.Element then
                    let value = x.ReadValue()
                    readParams (value :: values)
                else readParams values
            let values = readParams []
            while not (reader.Name = "methodCall" && reader.NodeType = XmlNodeType.EndElement) && reader.EOF && reader.Read() do ()
            values

        member x.ReadMethodResponse() =
            readToNamed "methodResponse" element
            readTo startElement
            if reader.Name = "fault" then
                if not (reader.ReadToDescendant("value")) then failwith "Malformed XmlRpc fault"
                x.ReadValue() |> sprintf "XmlRpc Fault: %A" |> failwith
            let value = if reader.ReadToDescendant("value") then x.ReadValue() else Nil
            while not (reader.Name = "methodResponse" && reader.NodeType = XmlNodeType.EndElement) && not reader.EOF && reader.Read() do ()
            value

    // XmlRpc is normal transported by an HTTP POST/response
    // Given an endpoint and method name along with arguments (XmlRpcValue), returns the resulting XmlRpcValue
    // No attempt is made to parse the values, only tokenize.
    let xmlRpcRequest (uri: string) meth args =
        let client = HttpWebRequest.Create(uri, Method="POST", ContentType="text/xml")
        (new XmlRpcWriter(client.GetRequestStream())).WriteMethodCall meth args
        (new XmlRpcReader(client.GetResponse().GetResponseStream())).ReadMethodResponse()

    // An XmlRpc "server" listens for requests on a particular TCP port, waiting for HTTP POSTs
    // I didn't like how HttpListener requires admin privileges and restricts listening blanketly on a port
    // Instead we handle a very minimal HTTP protocol right here
    // A port is assigned and returned.
    // Your callback is called with method name and args and is expected to return a value (XmlRpcValue) or throw
    // Again, no attempt is made here to parse the method call or validate arguments, etc.
    let xmlRpcListen (callback: string -> XmlRpcValue list -> XmlRpcValue) =
        let listener = new TcpListener(IPAddress.Any, 0)
        listener.Start()
        let port = (listener.LocalEndpoint :?> IPEndPoint).Port
        let listen () =
            while true do
                try
                    let client = listener.AcceptTcpClient()
                    let stream = client.GetStream()
                    let rec skipHeader newlines i =
                        let c = stream.ReadByte()
                        let newlines' = if c = int '\r' || c = int '\n' then newlines + 1 else 0
                        if c >= 0 && newlines' < 4 then skipHeader newlines' (i + 1) else i
                    let i = skipHeader 0 0 // merely ignore HTTP headers!
                    let reader = new XmlRpcReader(stream)
                    let name, args = reader.ReadMethodCall()
                    let ret = callback name args
                    let body = new MemoryStream()
                    let writer = new XmlRpcWriter(body)
                    writer.WriteMethodResponse(ret)
                    let write (str: string) = let b = Encoding.ASCII.GetBytes(str) in stream.Write(b, 0, b.Length)
                    write "HTTP/1.1 200 OK\r\n"
                    write "Content-Type: text/xml\r\n"
                    write (sprintf "Content-Length: %i\r\n" body.Length)
                    write "Server: XMLRPC++ 0.7\r\n"
                    write "\r\n"
                    stream.Write(body.GetBuffer(), 0, int body.Length)
                    client.Close()
                with ex -> printfn "Exception: %A" ex
        (new Thread(new ThreadStart(listen), IsBackground=true)).Start()
        port