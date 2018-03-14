---
layout: default
title:  Intervals
---

# Intervals

Intervals are used to represent spans of real (`double`), integer (`int`), time (`DateTime`) or time span (`TimeSpan`) values. In the simplest case they are bounded by a pair of point values, inclusively. In some cases they may be unbounded at one or both ends and the endpoints may be exclusive.

## Construction

Intervals may be constructed from simple pairs of points. These are bounded and inclusive at both ends. You may also specify exclusivity. For intervals bounded at only one end, use the `TimeInterval.LeftBounded(...)`/`TimeInterval.RightBounded(...)` static methods. For an interval unbounded at both ends, use `TimeInterval.Infinite`.

We'll use `TimeInterval` in these examples, but the same applies to `RealInterval`, `IntInterval` and `RelativeTimeInterval`s. All implement the same (`IInterval<T>`) interface and also have the same static members and operator overloads.

```csharp
var now = DateTime.Now;
var fiveMinAgo = now.Subtract(TimeSpan.FromMinutes(5));

var last5min = new TimeInterval(fiveMinAgo, now);
var toInfinityAndBeyond = TimeInterval.LeftBounded(now);
var last5minExclusive = new TimeInterval(fiveMinAgo, false, now, false);
var infinite = TimeInterval.Infinite;
```

Negative intervals (where `Left` > `Right`) are allowed. However, empty intervals where `Left` = `Right` and one end is non-inclusive are not allowed - an exception is thrown. Also, interval endpoints that are unbound yet inclusive make no sense (and are impossible to construct).

## Usage

### Properties

The endpoints each have the `Point` value and a flag indicating whether it's `Bounded` and/or `Inclusive`. These are in the `LeftEndpoint`/`RightEndpoint` properties. For convenience, there are also `Left`/`Right` properties equivalent to `LeftEndpoint.Point`/`RightEndpoint.Point`.

Unbounded endpoints will still contain a point value, but it will always be `<T>.MinValue`/`<T>.MaxValue`.

```csharp
var toInfinityAndBeyond = TimeInterval.LeftBounded(now);
var isRightBounded = toInfinityAndBeyond.RightEndpoint.Bounded; // false
var rightPoint = toInfinityAndBeyond.Right; // DateTime = 12/31/9999 11:59:59 PM
```

Other useful properties such as the `Span` and `Center` are available. Note that for `Time` intervals, points are `DateTime`s and spans are `TimeSpans`s. For `Real` and `Int` intervals the point and span types are the same.

For intervals bound at only one end, the `Span` is always `<T>.MaxValue` and the `Center` is the unbound end (min/max value).

Other useful properties include: `IsFinite`, `IsOpen`, `IsClosed`, `IsDegenerate`, `IsHalfBounded`, `IsNegative`, ...

### Operations

There are methods to determine whether a `PointIsWithin(...)` an interval, whether an interval `IntersectsWith(...)` another or `IsDisjointFrom(...)` or `IsSubsetOf(...)`. All of these take the inclusive/exclusive endpoint properties into account.

One note about `IsSubsetOf(...)` is that a completely overlapping interval will be considered a subset. On the other hand, the `IsProperSubsetOf(...)` method returns `true` only it's a subset _and_ not equal (again, taking inclusive/exclusive endpoint properties into account).

#### Transformations

You may translate an interval by a span. That is, shift both ends of a `TimeInterval` 5 minutes forward for example. You may also scale an interval to the left, right or out from the center, and may do so by a factor of the current width or by a set span.

```csharp
var next5min = last5min.Translate(TimeSpan.FromMinutes(5));
var next10min = next5min.ScaleRight(2.0);
var next20min = next10min.ScaleRight(TimeSpan.FromMinutes(10));
```

There are also `+`/`-` operator overloads allowing translation:

```csharp
var next5min = last5min + TimeSpan.FromMinutes(5);
```

Scaling from the center splits the difference. That is, scaling by `2.0` will result in an interval with twice the `Span`; having added half to each end.

### Understanding `RelativeTimeIntervals`

The `RelativeTimeInterval` type is used mainly to represent a _relative_ time interval. That is, a time interval around an unspecified origin. The `Left` point is generally a negative `TimeSpan` and the `Right` a positive one. For example, an interval from -5 minutes to +10 minutes:

```csharp
var left = TimeSpan.FromMinutes(-5);
var right = TimeSpan.FromMinutes(10);
var relativeTime = new RelativeTimeInterval(left, right);
```

You can then take this `relativeTime`, along with an origin `DateTime`, and construct a `TimeInterval` (rather than `RelativeTimeInterval`) from this:

```csharp
var origin = DateTime.Now;
var timeInterval = new TimeInterval(origin, relativeTime);
```

This is a special constructor for `TimeInterval` (nothing similar in other interval types). It is very similar to accomplishing directly by offsetting the `origin` by `left`/`right` time spans:

```csharp
var timeInterval = new TimeInterval(origin + left, origin + right);
```

The difference is that the `relativeTime` is a proper interval on which you can apply scaling and translating operations, etc. Also, the `relativeTime` carries with it the notions of boundedness and inclusivity. These properties are propagated into the `timeInterval` when constructed using the `(DateTime, RelativeTimeInterval)` constructor.

There is also an operator overload for `+` so that the following is equivalent:

```csharp
var timeInterval = origin + relativeTime;
```

### Operator Overloads

The base `Interval` type additionally provides `+`/`-` operator overloads:

```csharp
IInterval<TPoint, TSpan> operator +(Interval<TPoint, TSpan> interval, TSpan span)
IInterval<TPoint, TSpan> operator -(Interval<TPoint, TSpan> interval, TSpan span)
```

These are equivalent to `interval.Translate(span)` and `interval + interval.NegateSpan(span)` respectively.