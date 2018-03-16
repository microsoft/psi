---
layout: default
title:  Synchronization
---

# Synchronization

Joins allow fusing of two or more streams into a single stream.
Other frameworks (e.g. Rx) have similar operators such as `Zip()`, but in \\psi these are not based on the wall-clock time or ordinal of message arrival.
Instead, these are based on message `OriginatingTime` stamps.
Messages may arrive at any wall-clock time, but the timestamp in the envelope is what really matters.

## Interpolators

Joins are driven by a _primary_ stream while pairing with _secondary_ streams.
When we say `a.Join(b)`, then `a` is the primary stream.
Each message from the primary stream _may_ produce a joined value taken or synthesized from the secondary stream.
Interpolators determine how the secondary values are chosen.

| Interpolator | Window | Require Next Value |
|:--|:--|:--|
| `Match.Exact()` | ∅ | yes |
| `Match.Best<T>(...)` | [−∞, +∞] (default) | yes |

An `Exact` match requires that messages in the secondary stream align _exactly_ on time.
If no _exact_ match is found in the secondary stream then the primary stream message is dropped.
This is the default interpolator used by `Join(...)`.
You may however specify another interpolator such as `Best` (discussed next) or a custom window (see section below). 

A `Best` match will pair with a message from the secondary stream having the _nearest_ time; searching backward and/or forward in time.
The default is to search an _infinite_ time window both forward and backward in time; producing the absolute _best_ match.
No primary messages are dropped. Each is paired with the nearest secondary message or else waits (potentially forever) for a provably _best_ match.

To do this properly, notice that it is required that the next message is seen.
It cannot be known whether a given message from the secondary stream is the "best" match until the next message has been seen to confirm that it is not better.
Waiting for the next secondary message before emitting a pairing introduces latency.
Message times do increase monotonically so the _single_ next message is sufficient.
It is important to note that this may cause awkward pairings if the secondary stream drops or is extremely sparse. Primary messages may be paired with _very_ distant secondaries for lack of anything better. For this reason, it is occasionally desirable to give a time window constraint (discussed next).

## Custom Windows

Instead of the default behavior of `Best` (matching an _infinite_ time window), we may specify a window to `Match.Best(...)` as either a `RelativeTimeInterval` directly or as a `TimeSpan` which becomes an interval [-span, +span].
In fact, there is an implicit type conversion from `RelativeTimeInterval` and `TimeSpan` to a `Best` interpolator so _anywhere_ an interpolator is expected, a window may be specified.
This includes `Join` (e.g. `Join(TimeSpan.FromMilliseconds(50))` is equivalent to `Join(Match.Best(TimeSpan.FromMilliseconds(50)))`) and other domain-specific "Join-like" operators which may not necessarily provide direct overloads.
It is important to note that a non-infinite time window means that `Best` is no longer guaranteed not to drop primary messages — it _will_ if no secondary is found within the window.

## Default Values

When no suitable message is found in the secondary stream, the joins above will _not_ produce a value.
If instead we want to _always_ get something for every primary stream message, we can use one of the `*OrDefault()` variants: `Match.ExactOrDefault()`, `Match.BestOrDefault(...)`.
These will pair with `default(T)` values when nothing more suitable is found.

# Scalar Joins

Restating, simple joins between a primary stream and a _single_ secondary are done with `primary.Join(secondary)`.
Optionally, an interpolator may be given (`Match.Best()` by default) or a `RelativeTimeInterval` or `TimeSpan`.

A function mapping pairs of joined values to a type of our choosing (`Func<TPrimary, TSecondary, TOut>`) may be given as well.

## Tuple-flattening

If no mapping function is given to `Join(...)` then a stream of `ValueTuple` is produced by default.
That is, essentially `ValueTuple.Create` is the default mapping function.

This would become unwieldy once joins of joins of joins were used; producing tuples of tuples of tuples...
For example, `a.Join(b).Join(c).Join(d)` would produce `(((a, b), c), d)`.
To remedy this, there are versions of `Join(...)` on `IProducer<ValueTuple<...>>` up to an arity of seven which produce _flattened_ tupples.
This way `a.Join(b)` produces `(a, b)` but then this `.Join(c)` produces `(a, b, c)` and further `.Join(d)` produces `(a, b, c, d)`.
This is what is meant by "tuple-flattening."

A stream of scalar (or otherwise non-tuple) values joined with a stream of `ValueTuple<...>` _also_ produces a flattened tuple stream.
That is, aside from `a.Join(b).Join(c).Join(d)` producing `(a, b, c, d)` as above, the following does as well:

```csharp
    var cd = c.Join(d); // (c, d)
    var bcd = b.Join(cd); // (b, c, d)
    var abcd = a.Join(bcd); // (a, b, c, d)
```

# Vector Joins

Vector joins are from a single primary stream (`IProducer<T>`), as usual, but with a sequence of secondary streams.
That is, secondary streams as an `IEnumerable<IProducer<T>>`; each secondary of the same type as the primary.
The result is a _collection_ of values having been joined.
Results are in the form of a `T[]`, or a mapping function may be provided.

## Sparse Vector Joins

Like (dense) vector joins above, sparse vector joins work over a sequence of secondary streams (`IEnumerable<IProducer<T>>`).
However, it is not expected that every primary value has a corresponding secondary value to which to join.
Instead, the secondary stream may represent streams that "exist" (in a sense) or not for any given primary message.

A concrete example might me a face detector and tracker.
Each time a new face is detected, a _new_ stream of tracking results (location, size) is created.
These may proceed in parallel (in fact, sparse joins are most often used in conjunction with `Parallel` operations).
At one point face A and B may be detected and tracking streams started.
Then a new face C appears, then face B moves out of frame leaving only A and C, etc.
At each point we would like to join just the current faces.

This is a job for a sparse vector join.
As faces come and go a stream of mappings from ID to stream ordinal should be produced.
This becomes the primary stream.
Faces are tracked in parallel and become the secondary streams.
The result of the join is then a stream of collections of tracking results, where each collection contains only the active faces.

The primary stream may be of mappings directly (`IProducer<Dictionary<TKey, int>>`) or may be of any type, providing a `keyMapSelector` function is given.

# Pair

The `Pair` operator is much like `Join` but does not take message timestamps into account.
Instead, it merely pairs with the latest message seen in the wall-clock sense.
It is important to note that this makes `Pair` inherently non-deterministic and in fact not strictly a _synchronization_ operation.

An overload accepts an `initialValue` to be used when a message arrives on a one stream while nothing at all has been seen on the other.
It is paired with the `initialValue` in this case.
Otherwise, when no `initialValue` is provided, the `Pair` operator emits _nothing_ until a message on each stream has been seen.

Just as with `Join`, there is tuple-flattening behavior with `Pair` (see above).