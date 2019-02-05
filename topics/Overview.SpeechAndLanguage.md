---
layout: default
title:  Speech and Language Overview
---

# Speech and Language Overview

Situated interactive applications often need to deal with speech when communicating with users using natural language. The `Microsoft.Psi.Speech` and `Microsoft.Psi.Language` namespaces provide components and data types for basic speech recognition, voice activity detection and text-to-speech synthesis (Windows only).

## Speech recognition

\\psi provides a number of speech recognition components based on different speech recognition technologies.

- [SystemSpeechRecognizer](/psi/topics/Overview.SpeechAndLanguage#SystemSpeechRecognizer) (Windows only) - Uses the [System.Speech.Recognition.SpeechRecognitionEngine](https://msdn.microsoft.com/en-us/library/system.speech.recognition.speechrecognitionengine.aspx) that is based on Desktop Speech technology that comes with the .NET Framework on Windows.
- [MicrosoftSpeechRecognizer](/psi/topics/Overview.SpeechAndLanguage#MicrosoftSpeechRecognizer) (Windows only) - Uses the [Microsoft.Speech.Recognition.SpeechRecognitionEngine](https://msdn.microsoft.com/en-us/library/microsoft.speech.recognition.speechrecognitionengine.aspx) that is based on the [Microsoft Speech Platform](https://msdn.microsoft.com/en-us/library/hh361572.aspx) (requires a separate download and installation).
- [AzureSpeechRecognizer](/psi/topics/Overview.SpeechAndLanguage#AzureSpeechRecognizer) - Uses the [Azure Speech Service](https://azure.microsoft.com/en-us/services/cognitive-services/speech-services) that is part of Microsoft Cognitive Services.

<a name="SystemSpeechRecognizer"/>

### The SystemSpeechRecognizer component

The `SystemSpeechRecognizer` component performs continuous recognition on an audio stream. Recognition results are of type `SpeechRecognitionResult` and implement the `IStreamingSpeechRecognitionResult` interface. In general, this pattern allows for results from speech recognition components based on different underlying technologies to conform to a common interface for consumption by downstream components. Final speech recognition results are posted on the `Out` stream while partial recognition results are posted on the `PartialRecognitionResults` stream. Partial results contain partial hypotheses while speech is in progress and are useful for displaying hypothesized text as feedback to the user. The final result is emitted once the recognizer has determined that speech has ended, and will contain the top hypothesis for the utterance.

The following example shows how to perform speech recognition on an audio stream. Note that by default the speech recognizer expects a 16 kHz, 1-channel, 16-bit PCM audio stream. If the format of the audio source is different, either specify the correct format in the `AudioCaptureConfiguration.OutputFormat` configuration parameter (as shown), apply resampling to the audio stream using the `Resample` audio operator, or set the `SystemSpeechRecognizerConfiguration.InputFormat` configuration parameter to match the audio source format. However, not all input audio formats are supported.

```csharp
using (var pipeline = Pipeline.Create())
{
    // Capture audio from the default recording device in the correct format
    var audio = new AudioCapture(
        pipeline, 
        new AudioCaptureConfiguration()
        {
            OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm()
        });

    // Create a new speech recognizer component
    var recognizer = new SystemSpeechRecognizer(pipeline);

    // Send the audio to the recognizer
    audio.PipeTo(recognizer);

    // Print partial recognition results - use the Do operator to print
    var partial = recognizer.PartialRecognitionResults
        .Do(partial => Console.WriteLine(partial.Text.ToString()));

    // Print final recognition results
    var final = recognizer.Out
        .Do(final => Console.WriteLine(final.Text.ToString()));

    // Run the pipeline
    pipeline.Run();
}
```

#### Speech recognition results

The speech recognizer components generate recognition results which are represented by a `SpeechRecognitionResult` object. Results may be either partial or final (as indicated by the `IsFinal` property). Each result object contains one or more `Alternates`, each representing a single hypothesis. The top hypothesis may be accessed directly via the `Text` property. In addition, the raw audio associated with the recognition result is stored in the `Audio` property.

**NOTE:** Due to the fact that the partial and final recognition result event times are estimated from the audio stream position of the underlying recognition engine, the originating times of messages from the `SystemSpeechRecognizer` and `MicrosoftSpeechRecognizer` components may not reflect the exact times of the corresponding utterances in the input audio stream. See [this issue](https://github.com/Microsoft/psi/issues/20) for more details.

<a name="Grammars"/>

#### Grammars

By default, the recognizer uses a free text dictation grammar, but can also be configured to work with custom grammar files. These are XML files that conform to the [W3C SRGS Specification](http://www.w3.org/TR/speech-grammar/). An example [grammar](https://msdn.microsoft.com/en-us/library/ms554241(v=vs.110).aspx) file would be:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<grammar version="1.0" xml:lang="en-US"
            xmlns="http://www.w3.org/2001/06/grammar"
            tag-format="semantics/1.0" root="Main">
    <!--
    Defines an SRGS grammar for requesting a flight. This grammar includes
    a Cities rule that lists the cities that can be used for departures
    and destinations.
    -->
    <rule id="Main">
        <item>
            I would like to fly from <ruleref uri="#Cities"/>
            to <ruleref uri="#Cities"/>
        </item>
    </rule>

    <rule id="Cities" scope="public">
        <one-of>
            <item>Seattle</item>
            <item>Los Angeles</item>
            <item>New York</item>
            <item>Miami</item>
        </one-of>
    </rule>
</grammar>
```

To use one or more grammar files in the `SystemSpeechRecognizer`, set the `Grammars` configuration parameter to a list of `GrammarInfo` objects, each of which is a name-value pair of `Name` and `FileName`. Each `Name` is used as a key for the supplied grammar and should be unique.

```csharp
// Example of instantiating a recognizer with a set of grammar files.
var recognizer = new SystemSpeechRecognizer(pipeline,
    new SystemSpeechRecognizerConfiguration()
    {
        Grammars = new GrammarInfo[]
        {
            new GrammarInfo() { Name = "Hi", FileName = "Hi.grxml" },
            new GrammarInfo() { Name = "Bye", FileName = "Bye.grxml" },
            new GrammarInfo() { Name = "Yes", FileName = "Yes.grxml" },
            new GrammarInfo() { Name = "No", FileName = "No.grxml" },
            new GrammarInfo() { Name = "ThankYou", FileName = "ThankYou.grxml" }
        }
    });
```

#### Intents

Grammar rules may be annotated with semantic tags to augment them with semantic interpretation. When used with the `SystemSpeechRecognizer` component, any semantic interpretation of recognized text will be translated to intents that are output on the `IntentData` stream. The `IntentData` class contains a list of detected intents (with associated confidence scores), and a list of entities, each of which represents a key-value pair of an entity and its value, where they exist. The following is an example of how an intent and entity may be specified using semantic tags in a grammar.

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<grammar version="1.0" xml:lang="en-US"
            xmlns="http://www.w3.org/2001/06/grammar"
            tag-format="semantics/1.0" root="Main">

    <rule id="Main">
        <tag>$.RepeatReminder = {}</tag>
        <ruleref special="GARBAGE"/>
        <one-of>
            <item>remind me</item>
            <item>remind me again</item>
            <item>snooze for</item>
        </one-of>
        <ruleref special="GARBAGE"/>
        <one-of>
            <item>one minute<tag>$.RepeatReminder.Snooze = 1</tag></item>
            <item>a minute<tag>$.RepeatReminder.Snooze = 1</tag></item>
            <item>five minutes<tag>$.RepeatReminder.Snooze = 5</tag></item>
        </one-of>
        <ruleref special="GARBAGE"/>
    </rule>
</grammar>
```

With the above grammar configured, a recognized phrase of "remind me in a minute" would result in an `IntentData` message containing a single intent of `RepeatReminder` and an entity of `Snooze` with a value of `1`.

<a name="MicrosoftSpeechRecognizer"/>

### The MicrosoftSpeechRecognizer component
The `MicrosoftSpeechRecognizer` component, like the `SystemSpeechRecognizer` component, performs speech recognition on a stream of audio. However, it is implemented using the [Microsoft Server Speech Platform SDK](https://msdn.microsoft.com/en-us/library/hh361572.aspx). The usage of this component and its API are almost identical to that of the `SystemSpeechRecognizer` component. However, the `MicrosoftSpeechRecognizer` currently only supports [grammar-based](/psi/topics/Overview.SpeechAndLanguage#Grammars) speech recognition.

<a name="AzureSpeechRecognizer"/>

### The AzureSpeechRecognizer component

The `AzureSpeechRecognizer` component uses the [Cognitive Services Speech to Text API](https://azure.microsoft.com/en-us/services/cognitive-services/speech-to-text). In contrast to the `SystemSpeechRecognizer`, it requires as input a joint audio and voice activity signal, represented as a `ValueTuple<AudioBuffer, bool>`. The second item is a flag that indicates whether the `AudioBuffer` contains speech (or more specifically, voice activity). To construct such an input signal from a stream of raw audio, the [SimpleVoiceActivityDetector](/psi/topics/Overview.SpeechAndLanguage#SimpleVoiceActivityDetector) or [SystemVoiceActivityDetector](/psi/topics/Overview.SpeechAndLanguage#SystemVoiceActivityDetector) (Windows only) components may be used in conjunction with the `Join` operator, as in the following example.

```csharp
using (var pipeline = Pipeline.Create())
{
    // Create recognizer component. SubscriptionKey is required.
    var recognizer = new AzureSpeechRecognizer(
        pipeline,
        new AzureSpeechRecognizerConfiguration()
        {
            SubscriptionKey = "...", // replace with your own subscription key
            Region = "..." // replace with your service region (e.g. "WestUS")
        });

    var audio = new AudioCapture(
        pipeline, 
        new AudioCaptureConfiguration() 
        {
            OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm()
        });

    // AzureSpeechRecognizer requires VAD signal as input
    var vad = new SystemVoiceActivityDetector(pipeline);

    // Send the audio to the VAD to detect voice activity
    audio.PipeTo(vad);
    
    // Use the Join operator to combine the audio and VAD signals into a voice-annotated audio signal
    var voice = audio.Join(vad);

    // Send the voice-annotated audio to the recognizer
    voice.PipeTo(recognizer);

    // Print the final recognized text - use Select operator to select the Text property
    var output = recognizer.Select(result => result.Text).Do(text => Console.WriteLine(text));

    // Run the pipeline
    pipeline.Run();
}
```

The component posts final and partial recognition results represented by the `SpeechRecognitionResult` object on the `Out` and `PartialRecognitionResults` streams respectively.

Note that the `SubscriptionKey` and `Region` configuration parameters are required in order to use the speech recognition service. A subscription may be obtained [by registering here](https://azure.microsoft.com/en-us/try/cognitive-services/?api=speech-services).

## Voice activity detection

<a name="SystemVoiceActivityDetector"/>

### The SystemVoiceActivityDetector component

The `SystemVoiceActivityDetector` uses the [System.Speech.Recognition.SpeechRecognitionEngine](https://msdn.microsoft.com/en-us/library/system.speech.recognition.speechrecognitionengine.aspx) as a simple way to detect the start and end of voice activity in the audio stream.

The output of this component is a stream of boolean messages where a value of true indicates that voice activity was present at the originating time of the message, and a false indicates that silence (or no voice activity) was detected.

```csharp
// Create the VAD component
var vad = new Microsoft.Psi.Speech.SystemVoiceActivityDetector(pipeline);

// Send the audio to the VAD
audio.PipeTo(vad);

// Print VAD results
var output = vad.Out.Do(speech => Console.WriteLine(speech ? "Speech" : "Silence"));
```

Internally, the component feeds received audio into the underlying [System.Speech.Recognition.SpeechRecognitionEngine](https://msdn.microsoft.com/en-us/library/system.speech.recognition.speechrecognitionengine.aspx) which will detect whenever the audio state changes (e.g. from silence to speech). An output message will be posted for each input audio message indicating whether voice activity was present in the audio buffer. Because detection of the audio state change may have some inherent delay, the `VoiceActivityStartOffsetMs` and `VoiceActivityEndOffsetMs` configuration parameters are provided for fine-tuning.

Note that the `SystemVoiceActivityDetector` is only available on Windows Desktop platforms due to its use of the Windows Desktop Speech API.

<a name="SimpleVoiceActivityDetector"/>

### The SimpleVoiceActivityDetector component

The `SimpleVoiceActivityDetector` uses a simple log energy heuristic to determine the presence or absence of sound that could possibly contain voice activity within an audio signal. While not as robust as the `SystemVoiceActivityDetector`, it is available cross-platform.

Like the `SystemVoiceActivityDetector`, the output of this component is a stream of boolean messages representing the result of the detection.

Internally, the component relies primarily on a log energy threshold to detect the presence of a signal. You may tune this value in the `LogEnergyThreshold` property of the `SimpleVoiceActivityDetectorConfiguration` object that is passed the component on instantiation. The log energy is calculated based on a fixed frame size and frame rate that is applied to the input audio signal. These parameters may also be modified in the `SimpleVoiceActivityDetectorConfiguration` object. Finally, the detection and silence windows (i.e. the continuous length of time that the component has to detect sound or silence before it triggers a state change) are also configurable for fine-tuning the performance of the component.

## Speech synthesis

The `SystemSpeechSynthesizer` component performs text-to-speech conversion using the .NET [System.Speech.Synthesis.SpeechSynthesizer](https://msdn.microsoft.com/en-us/library/system.speech.synthesis.speechsynthesizer.aspx) class. The following is an example of how this component might be used to do text-to-speech. Note the use of the `Voice` configuration parameter to select the synthesis voice. The name here refers to the installed text-to-speech voices.

```csharp
var synthesizer = new Microsoft.Psi.Speech.SystemSpeechSynthesizer(
    pipeline,
    new Microsoft.Psi.Speech.SystemSpeechSynthesizerConfiguration
    {
        Voice = "Microsoft Zira Desktop"
    });

var player = new Microsoft.Psi.Audio.AudioPlayer(
    pipeline,
    new Microsoft.Psi.Audio.AudioPlayerConfiguration()
    {
        InputFormat = Microsoft.Psi.Audio.WaveFormat.Create16kHz1Channel16BitPcm()
    });

// Synthesize the recognized text and play that back through an audio player component
recognizer.Select(r => r.Text).PipeTo(synthesizer);
synthesizer.PipeTo(player);
```

The output of the `SystemSpeechSynthesizer` component is a stream of raw audio representing the synthesized speech.

<a name="LanguageUnderstanding"/>

## Language understanding

Language understanding is the process of inferring semantic intent from the recognized text. It is typically performed as the next stage following speech recognition, although it may be applied to any stream of text. The `LUISIntentDetector` uses the cloud-based [LUIS](https://www.microsoft.com/cognitive-services/en-us/language-understanding-intelligent-service-luis) service in conjunction with custom or pre-built apps developed using LUIS. See [http://www.luis.ai/](http://www.luis.ai/) for more details.

The output of the `LUISIntentDetector` component is a stream of `IntentData` which wraps a list of Intents and a list of Entities that were understood in the input phrase, as determined by the LUIS service. The LUIS service returns a JSON object that is then deserialized into an `IntentData` object that is posted on the `Out` stream.

## References
- [Azure Speech Services](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service)
- [Language Understanding Intelligent Service (LUIS)](https://www.microsoft.com/cognitive-services/en-us/language-understanding-intelligent-service-luis)
- [Microsoft Speech Platform](https://msdn.microsoft.com/en-us/library/hh361572.aspx)