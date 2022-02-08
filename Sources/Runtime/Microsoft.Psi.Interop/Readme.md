# Psi Interop

Here we describe the infrastructure provided in the `Microsoft.Psi.Interop` namespace to facilitate interoperation of Psi with other languages and platforms. Psi clearly supports .NET languages (C#/F#/...) on Linux/Mac/Windows. The primary targets for further interop are Python for data science and external ML and JavaScript for web "dashboards" and the (future) Electron-based PsiStudio.

## Alternatives to Psi Store Format

Alternative data formats to the Psi Store include the following and [are documented here](Format/Readme.md).

* [MessagePack](https://msgpack.org) (binary)
* [JSON](http://www.json.org/) (text)
* Comma-separated Values (flattened hierarchy)

## Transports

Streams in any of these formats may be persisted to or read from disk, or may be communicated over a message queue. Such [transports are described here](Transport/Readme.md).

## CLI Tool

The [PsiStoreTool](../../Tools/PsiStoreTool/Readme.md) exposes the above facilities as a command-line tool. It may be used to explore available streams in a store, convert [to other formats (MessagePack, JSON, CSV)](../../Runtime/Microsoft.Psi.Interop/Format/Readme.md)
and [persist to disk or send over a message queue](../../Runtime/Microsoft.Psi.Interop/Transport/Readme.md) for consumption by other platforms and languages.

## Rendezvous

The [rendezvous system](Rendezvous/Readme.md) maintains and relays information about \psi streams available on the network, allowing a distributed system to negotiate remoting connections.
