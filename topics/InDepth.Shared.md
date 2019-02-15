---
layout: default
title:  Cooperative Buffering
---

# Cooperative Buffering

## Introduction

One of the design goals of the Platform for Situated Intelligence runtime was to leverage the ease of programming offered by the .NET framework, while enabling the type of performance usually afforded only by carefully tuned native applications. To this end, a number of memory management mechanisms have been introduced to mitigate the costs of frequent large memory allocations and garbage collection.

## Motivation

In a \\psi pipeline, messages flowing between components may be copied multiple times in order to support asynchronous component execution as well as component isolation. As a message is passed to a component's receiver, it is deep-cloned (i.e. copied) to ensure isolation from other components that may also receive the same message. Due to the potentially large number of messages flowing through the pipeline, these frequent memory allocations may impose a significant performance hit on the system. When the message payloads are large (such as in the case of images, depth maps, etc.), performance is likely to be futher impacted.

The default .NET model of delayed memory management via garbage collection is not ideal for such frequent large allocations. The memory allocated for an object can only be reused after the object is garbage collected. Since garbage collection is relatively infrequent, and independent of the lifespan of the allocated objects, the time interval between the object becoming available for garbage collection (that is, when there are no more live references to it) and the memory becoming available for reuse can be quite large. More importantly, the memory is only reclaimed as a result of a garbage collection pass, with a cost proportional to the number of objects allocated since the last garbage collection pass. The generational model of the .NET garbage collector mitigates this issue to some extent, but not enough to avoid large garbage collection pauses in memory-intensive streaming systems, such as when allocating video frames at frame rate.

## Message Cloning and Recycling

The message cloning system in the Platform for Situated Intelligence runtime provides for memory buffer reuse to avoid new memory allocations when the message flow in the application reaches a steady state. This is achieved through the use of _recycling pools_ in message receivers, such that the memory backing a message may be recycled back to the pool once the receiver has finished processing it. When a message is cloned as it passes to a receiver, rather than allocating new memory from the managed heap, the cloning system first attempts to acquire a recycled object from the receiver's recycling pool into which the message may be copied. Only when there are no more available recycled objects in the pool is a new object created. In this way, the total number of new allocations is kept to a minimum.

### Message Lifetime

Internally, the runtime recycles a message once it has been processed by a receiver. This means that a received message has a lifetime only for the duration of a component's receiver method. Within the receiver method, the message may be inspected (and even modified) without regard for other components which may be operating concurrently on the same message, since each receiver gets its own cloned copy of the message. Once the receiver method has finished executing, the message is automatically recycled back to the recycling pool so that its underlying memory may be reused in a future message. The following is an example of how a message is typically used from within the receiver method:

```csharp
public void Receive(T data, Envelope envelope)
{
    // Received message `data` is used to compute some result which is then posted on the `Out` stream
    var result = this.ComputeResult(data);
    this.Out.Post(result, e.OriginatingTime);
    ...
    // Message `data` will be automatically recycled upon exiting this method
}
```

<a name="MessageCloning"></a>

### Message Cloning

If the component needs to hold on to the message beyond the lifetime of the receiver method, the message must first be cloned. A `DeepClone()` extension method is provided by the runtime for this purpose. The component is then responsible for recycling this long-lived cloned message when it is no longer needed. This is achieved by calling the `Receiver<T>.Recycle()` method on the receiver from which the original message was received. The following example illustrates this:

```csharp
private T recentQueue = new ConcurrentQueue<T>();

public void Receive(T data, Envelope envelope)
{
    // Make a cloned copy of `data` to store it beyond the execution of this method
    var copy = data.DeepClone(this.In.Recycler);
    this.recentQueue.Enqueue(copy);
    ...
    // Message `data` will be automatically recycled upon exiting this method.
    // `copy` will need to be explicitly recycled later when it is no longer needed.
}

// To be called when the local copies are no longer needed
public void ClearData()
{
    while (this.recentQueue.TryDequeue(out T data))
    {
        // Call the receiver's `Recycle()` method to recycle the message
        this.In.Recycle(data);
    }
}
```

In the above example, the `DeepClone()` extension method takes as an argument the recycler (of type `IRecyclingPool<T>`) associated with the receiver on which the message arrived. This allows the messages to be cloned into recycled memory if available, thus minimizing additional memory allocations.

Another overload of the `DeepClone()` extension method takes an existing copy into which the new copy is to be cloned. For example, if there already exists a long-lived copy which is no longer needed, then it may simply be reused as the target into which the new message is cloned:

```csharp
private T latest;

public void Receive(T data, Envelope envelope)
{
    // Update the local copy `latest` by cloning into it
    data.DeepClone(ref this.latest);
    ...
}
```

A final overload of `DeepClone()` takes no arguments and simply creates a new copy each time it is called. Use this to clone when a recycling pool is otherwise unavailable (e.g. in the closure of a `Do()` stream operator), and where it is acceptable to leave it to the garbage collector to reclaim the memory when the cloned object is no longer needed.

```csharp
private T latest;

public void Receive(T data, Envelope envelope)
{
    // Create a new copy of `data`
    this.latest = data.DeepClone();
    ...
}
```

## Shared Objects

Streams of large messages (such as those representing images or other large data structures) present additional challenges. While the use of recycling pools in conjunction with message cloning can alleviate some issues arising from frequent memory allocations, deep-cloning these messages still imposes a performance penalty due to the memory copy operation. Recall that message cloning fulfills the design goals of supporting asynchronous component execution as well as isolation of components. In cases where messages may be regarded as read-only within receivers (i.e. they are not modified in-place), we may skip the cloning step altogether, in effect sharing the same message across multiple components. However in order to do this, we need some way of knowing when a shared message is no longer being referenced and may be recycled.

### The `Shared<T>` Class

The `Shared<T>` class is designed to allow sharing of large objects and to provide full control over object allocations, making it possible to not only pass shared objects between concurrent components, but also reuse the allocated memory (when used in conjunction with a shared pool) once an object is no longer needed. The `Shared<T>` class uses explicit _reference counting_ to determine when an object can be disposed or released back to a shared pool, without having to rely on the garbage collector. Internally, the `Shared<T>` class encapsulates a _resource_ of type `T` in a _shared container_, which provides the reference counting functionality. The underlying resource may be accessed via the `Shared<T>.Resource` property.

### Reference Counting

Upon creation, a `Shared<T>` object has a reference count of 1. New references to the same shared object should be created by calling the `AddRef()` method. **Do not** use the assignment operator to create new references to a `Shared<T>` object (other than short-lived references within local scope). When a reference to a `Shared<T>` is no longer needed, release it by calling `Dispose()`, or wrap it in a `using` block.

```csharp
Shared<T> shared = Shared.Create<T>(resource); // ref count == 1
Shared<T> shared2 = shared.AddRef(); // ref count == 2

// shared.Resource and shared2.Resource refer to the same underlying object
...

shared.Dispose(); // ref count == 1
shared2.Dispose(); // ref count == 0
```

```csharp
Shared<T> shared = Shared.Create<T>(resource); // ref count == 1
using (Shared<T> shared2 = shared.AddRef()) // ref count == 2
{
    ...
} // ref count == 1

shared.Dispose(); // ref count == 0
```

### Behavior of `Shared<T>` in Receivers

The shared container is treated differently by the message cloning system such that when passing a message of type `Shared<T>` to a receiver, instead of deep-cloning the resource, the reference count of the container is incremented. Thus, the same resource may be passed between components without an expensive copy operation. When the same `Shared<T>` message is delivered to more than one receiver, its reference count is simply incremented multiple times, once per receiver.

Within the receiver method, the underlying resource encapsulated by the `Shared<T>` object may be accessed in the usual way via its `Resource` property. Since the same shared resource may potentially be accessed concurrently from elsewhere, it **must not** be modified in-place and should be considered read-only. When the receiver method finishes executing, the runtime automatically decrements the reference count of the `Shared<T>` object, allowing it to be recycled or disposed once there are no more references to it.

```csharp
public void Receive(Shared<T> shared, Envelope envelope)
{
    // Use the shared resource (read-only)
    this.DoSomethingWith(shared.Resource);

    // Shared ref count will be decremented upon exiting this method
}
```

### Long-Lived References

Since the reference to the shared object is released upon exiting the receiver, if the component needs to hold on to the shared resource beyond the lifetime of the receiver method, it must do so by calling `AddRef()` on the `Shared<T>` object. This increments the reference count and returns a new `Shared<T>` object encapsulating the same shared resource. When this long-lived reference to the shared resource is no longer needed, it must be released by calling the `Dispose()` method. This ensures that its reference count remains consistent and allows the underlying resource to be recycled or disposed once there are no more references to it.

```csharp
private Shared<T> latest;

public void Receive(Shared<T> shared, Envelope envelope)
{
    // Ensure that any previous long-lived reference is first released
    this.latest?.Dispose();

    // Use `AddRef()` to create a new long-lived reference to the shared object
    this.latest = shared.AddRef();

    // Shared ref count will be decremented upon exiting this method but
    // the long-lived reference `this.latest` will remain valid.
}

public void Dispose()
{
    // Ensure that any remaining reference is released upon component disposal
    // (assumes that the component implements the `IDisposable` interface).
    this.latest?.Dispose();
}
```

The above guidelines for creating long-lived references to `Shared<T>` objects beyond the lifetime of the receiver method using the `AddRef()` and `Dispose()` methods is analogous to creating long-lived copies of non-shared messages using `DeepClone()` and `Recycle()`, as discussed in the section on [message cloning](/psi/topics/InDepth.Shared#MessageCloning).

> **Note**: Due to the unique implementation of the internal serializer for `Shared<T>` objects to support automatic reference counting when posting shared messages to receivers, the `DeepClone()` extension methods **do not** actually create deep-cloned copies when called on `Shared<T>` objects. Instead, the reference count is simply incremented as though `AddRef()` was called.

### Usage With Shared Pools

While the `Share<T>` class facilitates sharing of a resource, in order to support reuse of the underlying shared resources, the `Shared<T>` class should be used in conjunction with a _shared pool_. The `SharedPool<T>` class provides a generic implementation of a common pool from which `Shared<T>` objects of type `T` may be allocated. Upon allocation from the pool, a `Shared<T>` object has an initial reference count of 1. 

```csharp
// Create a shared pool of objects of type `Buffer`. The `SharedPool` constructor
// takes an allocator function that constructs an instance of `Buffer` when needed.
SharedPool<Buffer> bufferPool = new SharedPool<Buffer>(() => new Buffer());

// Allocate a `Shared<Buffer>` from the pool - this will either return an unused
// instance of `Buffer` that was previously recycled back to the pool, or create
// a new one if there are no available buffers in the pool.
Shared<Buffer> sharedBuffer = bufferPool.GetOrCreate();
...

// Release the shared reference. This decrements the ref count to 0, which
// automatically recycles the underlying resource back to the pool.
sharedBuffer.Dispose();
```

When allocating a shared object from a `SharedPool<T>` by calling its `GetOrCreate()` method, the pool first tries to reuse a previously recycled object by wrapping it in a `Shared<T>` instance. If there are no available recycled objects in the pool, it creates a new one using the allocator function that was provided when the pool was created.

For situations where multiple pools of similar objects may be required (e.g. buffers of different sizes, images of different formats and/or dimensions), one may use the `KeyedSharedPool<T>` class, where the key can be any property that distinguishes one pool of compatible objects from another:

```csharp
// Create a shared pool of objects of type `Buffer` keyed by buffer size.
// The `KeyedSharedPool` constructor takes an allocator function that
// constructs an instance of `Buffer` of the requested size, when needed.
KeyedSharedPool<Buffer, int> bufferPool =
    new KeyedSharedPool<Buffer>(size => new Buffer(size));

using (Shared<Buffer> largeBuffer = bufferPool.GetOrCreate(100000))
{
    // use the buffer
    ...
}

using (Shared<byte[]> smallBuffer = bufferPool.GetOrCreate(100))
{
    // use the buffer
    ...
}
```

When allocating a shared object from a `KeyedSharedPool<T, TKey>`, the key value that is passed to the `GetOrCreate()` method is used to determine the sub-pool from which to retrieve or create the shared object. The shared object will be recycled back to the same sub-pool when it is no longer referenced.

### Specialized Shared Pools

While the `SharedPool<T>` and `KeyedSharedPool<T, TKey>` classes may be used directly to create instanced shared pools, in some applications it may be more convenient to define specialized pools of specific object types using one of the above as a singleton instance so that the pool may be shared across components within the application. \\psi includes the following specialized pools.

| Shared Pool | Description |
| :----------- | :---------- |
| [Microsoft.Psi.SharedArrayPool&lt;T&gt;](/psi/api/class_microsoft_1_1_psi_1_1_shared_array_pool.html) | Provides a generic pool of shared arrays of type `T[]`. |
| [Microsoft.Psi.Imaging.ImagePool](/psi/api/class_microsoft_1_1_psi_1_1_imaging_1_1_image_pool.html) | Provides a pool of shared images of type `Microsoft.Psi.Imaging.Image`. |
| [Microsoft.Psi.Imaging.EncodedImagePool](/psi/api/class_microsoft_1_1_psi_1_1_imaging_1_1_encoded_image_pool.html) | Provides a pool of shared images of type `Microsoft.Psi.Imaging.EncodedImage`. |

### Summary and Examples

The following is a summary of the properties and guidelines for using `Shared<T>` objects:
- When a `Shared<T>` object is instantiated, the reference count of the encapsulated resource is set to 1.
- When `Shared<T>.AddRef()` is called, a new reference is created and the reference count is incremented by 1.
- When `Shared<T>.Dispose()` is called (either explicitly or implicitly by wrapping it in a `using` block), the reference count is decremented by 1.
- When the reference count reaches zero, the underlying resource is either disposed (if it was created without a pool), or recycled back to the pool from which it was allocated.
- Where possible, create `Shared<T>` objects from a shared pool to take advantage of the benefits of recycling and reuse.
- Avoid using the assignment operator to capture references to a `Shared<T>` instance beyond local scope.
- Call `Shared<T>.AddRef()` to create a new reference to the shared resource, and remember to call `Shared<T>.Dispose()` to release the reference when it is no longer needed.
- It is not required to call `AddRef()` when posting a `Shared<T>` instance to a message stream.
- `Shared<T>` objects received from a message stream in a receiver method do not need to be explicitly disposed. 
- Never store a long-lived reference directly to the underlying `Shared<T>.Resource`.
- The underlying resource in a `Shared<T>` object should not be modified. Create a copy of it if it needs to be modified.

The following are some examples of how `Shared<T>` objects are used in different component scenarios.

#### Source Component

A source component may post `Shared<T>` messages on an output stream in response to some external event occurring in the world, such as data captured by a sensor such as a camera. For example, an image capture source component may implement the following event handler which is called whenever a new image is acquired. The raw image data is copied into the resource of a `Shared<Image>` acquired from the `ImagePool` (assuming the image format and dimensions are fixed and known in advance). The `Shared<Image>` is then posted to the output stream of the component.

```csharp
private void OnImageCaptured(IntPtr data, DateTime timestampUtc)
{
    // Get or create an empty `Shared<Image>` of the required dimensions
    using (var sharedImage = ImagePool.GetOrCreate(this.Width, this.Height, this.Format))
    {
        // Copy the raw data into the newly allocated shared resource
        sharedImage.Resource.CopyFrom(data);        
        this.Out.Post(sharedImage, timestampUtc);
    }
}
```

Note that the `sharedImage` instance is wrapped in a `using` block from the time it is allocated from the pool until after it has been posted to the output stream. This ensures that the reference count to the underlying shared image is released when no longer needed by this component. When posting `sharedImage` to the output stream, each connected receiver will receive a `Shared<Image>` instance to the same underlying shared image, and its reference count will be incremented automatically by the runtime. There is no need to explictly call `AddRef()`.

#### Reactive (Stateless) Component

A reactlve component _reacts_ to a received message by acting on it in some fashion (e.g. applying a transform), and posting the result to an output stream, all within the execution of its receiver method. For instance, a component which applies a transform to an image might implement a receiver method such as the following:

```csharp
public void Receive(Shared<Image> sharedImage, Envelope e)
{
    // Get or create an empty `Shared<Image>` to hold the result
    // (assumes transformed image will be of the same dimensions).
    using (var destImage = ImagePool.GetOrCreate(sharedImage.Resource.Width, sharedImage.Resource.Height, this.format))
    {
        // Apply the transform to the underlying image `sharedImage.Resource`
        // and store the transformed output in`destImage.Resource`.
        this.transform(sharedImage.Resource, destImage.Resource);
        this.Out.Post(destImage, e.OriginatingTime);
    }
}
```

#### Stateful Component

A stateful component may need to keep a reference to a received message beyond the point after which its receiver method has finished executing. For `Shared<T>` message receivers, this requires calling `AddRef()` to ensure that the reference count properly accounts for the additional reference outside of the receiver method. This reference must also be properly released by calling `Dispose()` when it is no longer needed.

For example, consider a component which updates a long-lived reference to the last received message, but only when some condition is true:

```csharp
private Shared<Image> displayImage;

public void Receive(Shared<Image> sharedImage, Envelope e)
{
    // This condition may not be true for every receiver call.
    if (this.needsUpdate)
    {
        // Ensure the previous reference is properly released before
        // we assign a new reference to `this.displayImage`.
        this.displayImage?.Dispose();

        // Call `AddRef()` to create a new reference. 
        // Do not simply do `this.displayImage = sharedImage`.
        this.displayImage = sharedImage.AddRef();
    }
}

// Assumes component is IDisposable.
public void Dispose()
{
    // Ensure any remaining reference is properly released.
    this.displayImage?.Dispose();
}
```

### Tracing Memory Leaks

Since a reference to a `Shared<T>` instance should always be disposed to ensure that the reference count is correct, failure to adhere to this rule may result in memory "leaks", where `Shared<T>` resources are not returned to the pool and simply garbage collected. Though not a memory leak in the strictest sense since the underlying resource is eventually reclaimed by the garbage collector, this nevertheless negates the benefits of using a pool in the first place, since the memory is not recycled and new heap allocations would be needed to satisfy additional requests.

The `TRACKLEAKS` conditional complation flag, which is defined by default on debug builds in the `Microsoft.Psi` project, enables tracing of `Shared<T>` object lifetimes for debugging purposes. When `TRACKLEAKS` is enabled, a stack trace is saved during the creation of a `Shared<T>` instance, and if it is not properly disposed of but instead is garbage collected, a debug message will be printed to the debug trace output when it is finalized.

The `TRACKLEAKS` flag also enables tracing in message recycling pools to ensure that messages are being recycled properly. If messages are never recycled back to the pool from which they were allocated, this will cause a debug trace message to be printed.
