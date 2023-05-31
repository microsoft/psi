# Interop Data Formats

Alternative formats when cross language/platform interop is needed include:

* [MessagePack](https://msgpack.org) binary: `MessagePackFormat`
* [JSON](http://www.json.org/) text: `JsonFormat`
* Comma-separated Values (flattened hierarchy): `CsvFormat`

Potential future formats include Python Pickle, Protobuf, and others.

Each is an implementation of several serialization [interfaces described here](../Serialization/Readme.md).

## Concrete JsonFormat

A single `JsonFormat` class provides an implementation of all of the above interfaces for JSON. Message records are of the following form where `<message>` is a JSON-serialized message and `<originatingTime>` is an ISO 8601 string representing the _originating_ time of the message:

```json
{ "originatingTime": <time>, "message": <message> }
```

For example, messages of the form:

```csharp
var message = new
{
    ID = 123,
    Confidence = 0.92,
    Face = new
    {
        X = 213,
        Y = 107,
        Width = 42,
        Height = 61
    }
};
```

Would serialize as the following:

```json
{
    "originatingTime": "1971-11-03T00:00:00Z",
    "message": {
        "ID": 123,
        "Confidence": 0.92,
        "Face": {
            "X": 213,
            "Y": 107,
            "Width": 42,
            "Height": 61
        }
    }
}
```

The persisted format is an array of such records:

```json
[
  <record>,
  <record>,
  ...
]
```

## Concrete CSV

A single `CsvFormat` class provides implementations for comma-separated-values encoded according to [RFC 4180](https://tools.ietf.org/html/rfc4180). Records are of the form:

```csv
<_OriginatingTime_>,<header>,<header>,<header>, ...
<time>,<field>,<field>,<field>, ...
...
```

The first column is the originating time of the messages and is named as such.

Each message includes the header row. Deserializing these will give messages represented as an `ExpandoObject` with named `dynamic` properties for each column (except `_OriginatingTime_`).

There are several limitations to the CSV format. Much of the type information is lost, hierarchical values are flattened and collection properties are ignored.
For example a face tracker message of the following form would serialize the leaf primitive properties as peers:

```
new
{
	ID = 123,
	Confidence = 0.92,
	Face = new // child properties of Face are serialized as siblings to ID and Confidence
	{
		X = 213,
		Y = 107,
		Width = 42,
		Height = 61
		Points = new [] { 123, 456 } // collection property ignored
	}
};
```

Serializes to:

```csv
_OriginatingTime_,ID,Confidence,X,Y,Width,Height
2018-09-06T00:39:19.0883965Z,123,0.92,213,107,42,61
...
```

Identically named fields at different levels in the hierarchy can become confusing. For example, if each `Face` also has a `Confidence`:

```
new
{
	ID = 123,
	Confidence = 0.92,
	Face = new
	{
		Confidence = 0.89, // additional Confidence property
		X = 213,
		Y = 107,
		Width = 42,
		Height = 61
	}
};
```

This serializes _both_ properties as adjacent columns, sharing the name "Confidence" with the outer field first:

```csv
_OriginatingTime_,ID,Confidence,Confidence,X,Y,Width,Height
2018-09-06T00:39:19.0883965Z,123,0.92,0.89,213,107,42,61
...
```

While serialization to this ill-advised schema will work, _deserializing_ from this CSV data will fail to construct `ExpandoObjects` because of the identically named fields.

A solution is to `Select(...)` a more sane structure. Given a stream of face tracker results, it is easy enough to select out a "flattened" view with clear names of your choice (e.g. "OverallConfidence" vs. "FaceConfidence"):

```csharp
var flattened = faces.Select(f => new { ID = f.ID,
                                        OverallConfidence = f.Confidence,   // overall confidence
                                        FaceConfidence = f.Face.Confidence, // face confidence
                                        FaceX=f.Face.Rect.X,
                                        FaceY=f.Face.Rect.Y,
                                        FaceWidth=f.Face.Rect.Width,
                                        FaceHeight=f.Face.Rect.Height });
```

Serializing this would then produce a stream of messages in the form:

```csv
_OriginatingTime_,ID,OverallConfidence,FaceConfidence,FaceX,FaceY,FaceWidth,FaceHeight
2018-09-06T00:39:19.0883965Z,123,0.92,0.89,213,107,42,61
```

### Simple Primitives

In the case of a simple stream of primitive types (e.g. `double` with `faces.Select(f => f.Confidence)`), the persisted form looks like:

```csv
_OriginatingTime_,_Value_
2018-09-06T00:39:19.0883965Z,0.92
```

Notice the special field name `_Value_` used to distinguish this case. This deserializes back to a simple primitive; `double` in this case.

### Numeric Collections

A very special case, which is commonly used in ML with Psi, is a single collection of numeric types such as `IEnumerable<double>`. For example, `new double[] { 1, 2, 3, 4, 5 }` serializes to:

```csv
_OriginatingTime_,_Column0_,_Column1_,_Column2_,_Column3_,_Column4_
2018-09-06T12:12:25.7463172-07:00,1,2,3,4,5
```

Notice the `_Column0_`, `_Column1_`, ... headers. This deserializes back into a simple `double[]`.

### Persistent Form

CSV is generally used as a persisted format, but may also be used for transport (over a message queue, etc.). 

```csv
_OriginatingTime_,ID,Confidence,FaceX,FaceY,FaceWidth,FaceHeight
2018-09-06T00:39:19.0883965Z,123,0.92,213,107,42,61
2018-09-06T00:39:19.0983761Z,123,0.89,215,101,44,63
2018-09-06T00:39:19.1183762Z,123,0.90,212,104,43,62
...
```

Records in the persisted form are delimited by `\r\n` (or appropriate `Environment.NewLine` for the executing platform).

## Concrete MessagePack

A single `MessagePackFormat` class provides implementations for [MessagePack](https://msgpack.org) binary records. The persisted form contains an `int32` length prefix (little endian) to each record and is terminated by a zero-length record. That is:

```text
<length><record bytes><length><record bytes>...0
```

The format is very compact and is recommended when data transfer bandwidth or persisted file size is a concern. The expressiveness of the format is very similar to JSON. It's difficult to give examples of the serialized form, given that it is a byte-level encoding, roughly type-tagged fields with various encoding strategies. The whole payload is then LZ4 compressed, making it quite opaque to humans.

However, there are serialization libraries for MessagePack in 50+ languages, making it *very* portable; a recommended format for streams of structured data.