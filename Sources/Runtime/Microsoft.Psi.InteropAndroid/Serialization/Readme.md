# Interop Serialization Interfaces

Each [concrete format](../Format/Readme.md) is an implementation of several serialization interfaces. These interfaces are similar to `Microsoft.Psi.Serialization.ISerializer` but are specific to dynamic types and don't include cloning. An `IFormatSerializer` converts a message of any type, along with its originating time, to a simple `byte[]`, while an `IFormatDeserializer` reverses the process; taking a `byte[]` and returning a message and originating time.

```csharp
public interface IFormatSerializer
{
    (byte[], int, int) SerializeMessage(dynamic message, DateTime originatingTime);
}

public interface IFormatDeserializer
{
    (dynamic, DateTime) DeserializeMessage(byte[] payload, int index, int count);
}
```

Versions of these intended for persistent storage, where a sequence of messages are persisted together, are provided as well.

```csharp
public interface IPersistentFormatSerializer
{
    dynamic PersistHeader(dynamic message, Stream stream);

    void PersistRecord(dynamic message, DateTime originatingTime, bool first, Stream stream, dynamic state);

    void PersistFooter(Stream stream, dynamic state);
}

public interface IPersistentFormatDeserializer
{
    IEnumerable<(dynamic, DateTime)> DeserializeRecords(Stream stream);
}
```

`IPersistentFormatSerializer` writes a set of messages to a `Stream`. The header may be field names in the case of CSV, a simple array container in the case of JSON, etc. Similarly, the footer may close such constructs. The `PersistRecord` method is very similar to `SerializeMessage` above, but may include message framing or delimiting, such as `Environment.NewLine` delimiting records in CSV or a comma for JSON, or maybe a length-prefix for binary MessagePack. The `IPersistentFormatDeserializer` reverses the process; producing messages and timestamps from a previously serialized stream.

Notice that messages lose their types at this point; generally becoming `dynamic` over `ExpandoObject` and primitives. This means that it may no longer be possible to reify as the original .NET types after serialization in this way (unlike with Psi Stores).

Note also that, while `dynamic` may be any type, it is recommended that deserialization returns primitives or composites in the form of `ExpandoObject`. For example, the JSON implementation uses `Newtonsoft.Json.JsonConvert` under the covers but, rather than returning `JObject`, is careful to not expose dependencies on this library.

## Note About `dynamic` and `ExpandoObject`

A `dynamic` type may be *anything* in .NET and an `ExpandoObject` may have properties of *any* type. We are using these types to represent untyped values flowing through the system in various places above.

Composite/structured values should be restricted to collections (arrays or `IEnumerable<_>`) of primitives or other composites or `ExpandoObject` of named properties of primitives or other composites. Any "shape" of data is representable this way.