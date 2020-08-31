# Speech Sample

In this sample, we will demonstrate how to build a simple speech recognition application on Linux using the Azure Cognitive Services Speech service. In the process, we will write a simple speech recognizer \psi component that wraps the Speech service client. We will then feed it live audio from the AudioCapture component to produce speech-to-text results. 

## Prerequisistes

* A microphone or other audio capture device.
* ALSA sound libraries installed (see [Audio Overview](https://github.com/microsoft/psi/wiki/Audio-Overview#troubleshooting-audio-on-linux)).
* A valid Cognitive Services Speech subscription key (see [Try the Speech service for free](https://docs.microsoft.com/azure/cognitive-services/speech-service/get-started)).

## Speech Recognizer Component

To begin, we will create a simple speech recognizer component that uses the [Speech SDK](https://aka.ms/csspeech). The Speech SDK is available as a [NuGet package](https://www.nuget.org/packages/Microsoft.CognitiveServices.Speech/)  which has already been referenced in the sample project.

We will name this component `ContinuousSpeechRecognizer` since it performs continuous recognition on an input audio stream. The component is declared and initialized as follows:

```csharp
public class ContinuousSpeechRecognizer : ConsumerProducer<AudioBuffer, string>, ISourceComponent, IDisposable
{
    public ContinuousSpeechRecognizer(Pipeline pipeline, string subscriptionKey, string region)
        : base(pipeline)
    {
        var config = SpeechConfig.FromSubscription(subscriptionKey, region);
        this.pushStream = AudioInputStream.CreatePushStream();
        this.audioInput = AudioConfig.FromStreamInput(this.pushStream);
        this.recognizer = new SpeechRecognizer(config, this.audioInput);
    }
}
```

By deriving from the base `ConsumerProducer<TIn, TOut>` class, we create a component that takes a single stream of type `TIn` as input and produces a single stream of type `TOut` as output. In this case, our component consumes a stream of `AudioBuffer` and produces a stream of `string`.

We initialize the component with the `subscriptionKey` and `region` of the Speech service, and create the internal recognizer using the Speech SDK. You can find more examples on how to use the Speech SDK in the [Speech SDK samples repository](https://github.com/Azure-Samples/cognitive-services-speech-sdk/).

Note that this component implements the `ISourceComponent` interface. This is because it posts messages in response to external events (in this case the asynchronous notifications from the Speech service) rather than _reactively_ (i.e. only from within its receiver method in response to an incoming message). 

The `ISourceComponent` interface also defines two methods, `Start` and `Stop`, which we will implement to start and stop the recognizer:

```csharp
    public void Start(Action<DateTime> notifyCompletionTime)
    {
        this.recognizer.Recognized += this.Recognizer_Recognized;
        this.recognizer.StartContinuousRecognitionAsync().Wait();
        notifyCompletionTime(DateTime.MaxValue);
    }

    public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
    {
        this.recognizer.Recognized -= this.Recognizer_Recognized;
        this.pushStream.Close();
        this.recognizer.StopContinuousRecognitionAsync().Wait();
        notifyCompleted();
    }
```

The `Start` method registers an event handler for the `Recognized` event and starts the recognizer in continuous recognition mode. Note that we call the `notifyCompletionTime` with a value of `DateTime.MaxValue` because this component has no definite completion time and will continue producing output for as long as it continues to receive speech input.

The `Stop` method lets the pipeline notify the component that it is shutting down (e.g. when the application exits or the pipeline is disposed). We unregister the event handler, close the input and stop the recognizer. Once the recognizer has stopped, we notify the pipeline that this component is done posting messages by calling `notifyCompleted` (some components may continue producing final messages after the `Stop` method returns, but this is not the case here).

Next, we need to push the audio data received on the component's input stream to the Speech service. We will do this in the component's `Receive` method:

```csharp
    protected override void Receive(AudioBuffer data, Envelope envelope)
    {
        this.pushStream.Write(data.Data);
    }
```

\psi audio components generally pass audio data around wrapped in an `AudioBuffer`. Here, we simply write the `byte[]` buffer containing the audio `Data` to the recognizer's input stream.

Finally, we define the handler for `Recognized` events raised by the recognizer:

```csharp
    private void Recognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
    {
        var originatingTime = this.Out.Pipeline.StartTime.AddTicks((long)e.Offset) + e.Result.Duration;
        this.Out.Post(e.Result.Text, originatingTime);
    }
```

All we do here is post the recognized text to the component's output stream. We need to do a little bit of work to figure out the originating time of the recognized text. The `SpeechRecognitionEventArgs` from the recognizer provides us with the offset and duration of the recognized text. By convention, we use the time at the end of the utterance as the originating time.

And that's it - we have written a simple speech-to-text component by wrapping the Speech service client. In the next section, we will see how to hook this up to some audio and get recognition results.

You can learn more about writing \psi components in the [Writing Components](https://github.com/microsoft/psi/wiki/Writing-Components) wiki page.

## Speech-to-Text

In order to use our new `ContinuousSpeechRecognizer` component, we need to give it a stream of `AudioBuffer`. We will do this by creating an `AudioCapture` component in a pipeline to capture audio from an input device.

```csharp
using (Pipeline pipeline = Pipeline.Create())
{
    var audio = new AudioCapture(pipeline, new AudioCaptureConfiguration { DeviceName = deviceName, Format = WaveFormat.Create16kHz1Channel16BitPcm() });
}
```

By default, the speech recognizer takes 16 kHz, 16-bit mono PCM audio as input, so that is what we will use in our audio capture configuration. The `deviceName` specifies the audio device to capture from. This is typically of the form "plughw:_c_,_d_" where _c_ is the soundcard index and _d_ is the device index (e.g. "plughw:0,0", "plughw:1,0", etc.). You can list the available capture devices using the `arecord -L` command.

To learn more about working with audio in \psi, see the [Audio Overview](https://github.com/microsoft/psi/wiki/Audio-Overview) wiki page.

Having created the audio stream, we then instantiate a new `ContinuousSpeechRecognizer` component in the same pipeline, connect the audio to its input using the `PipeTo` operator, and print its output using the `Do` operator: 

```csharp
using (Pipeline pipeline = Pipeline.Create())
{
    var audio = new AudioCapture(pipeline, new AudioCaptureConfiguration { DeviceName = deviceName, Format = WaveFormat.Create16kHz1Channel16BitPcm() });
    var recognizer = new ContinuousSpeechRecognizer(pipeline, azureSubscriptionKey, azureRegion);

    audio.PipeTo(recognizer);

    recognizer.Out.Do((result, e) => Console.WriteLine($"{e.OriginatingTime.TimeOfDay}: {result}"));

    pipeline.RunAsync();
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey(true);
}
```

Run the above above pipeline and speak into the microphone. You should see recognized text print to the console.

## Links
* [Speech service documentation](https://aka.ms/csspeech)
* [Try the Speech service for free](https://docs.microsoft.com/azure/cognitive-services/speech-service/get-started)
* [Writing Components](https://github.com/microsoft/psi/wiki/Writing-Components)
* [Audio Overview](https://github.com/microsoft/psi/wiki/Audio-Overview)