---
layout: default
title:  Audio Overview
---

# Audio Overview

Recording and playback of audio are common operations in situated interactive applications. For instance, audio input may be required
for speech recognition, or to generate acoustic features for use with acoustic models. Applications may also need to generate and
produce audio output to communicate with users. The <see cref="Microsoft.Psi.Audio">Microsoft.Psi.Audio</see> namespace provides components and operators for capturing,
processing and rendering audio, as well as for generating a range of acoustic features.<br>

**Please note**: Audio capture, and playback is supported on Windows and Linux. Audio resampling is currently only available on Windows.

## Basic Components

Basic audio capabilities are provided by the following components in the `Microsoft.Psi.Audio namespace`:
- `AudioSource` - Captures audio from an audio recording device.
- `AudioPlayer` - Plays back audio on an audio playback device.
- `AudioResampler` - Resamples an audio stream (Windows only).
- `WaveFileAudioSource` - Reads audio from a wave file.
- `WaveFileWriter` - Writes an audio stream to a wave file.

In Platform for Situated Intelligence, audio is generally handled and passed between components via streams of type `AudioBuffer`. An `AudioBuffer` contains a
single buffer of raw audio data along its associated format information in a `WaveFormat` or `WaveFormatEx` object.

## Common Patterns of Usage

The following are some examples of how to use the basic audio components.

### Capturing and playing back audio

The following code will capture audio from the default audio recording device on Windows and echo it to the default audio playback device.

```csharp
using (var pipeline = Pipeline.Create())
{
    var source = new AudioSource(pipeline);
    var player = new AudioPlayer(pipeline);
    source.PipeTo(player);
    pipeline.Run();
}
```

Individual audio devices for capture and playback may be specified in an `AudioSourceConfiguration` or `AudioPlayerConfiguration`
object, which may optionally be supplied when constructing an `AudioSource` or `AudioPlayer` as shown in the following code:

```csharp
var source = new AudioSource(
    pipeline,
    new AudioSourceConfiguration()
    {
        DeviceName = "Headset Microphone (USB)"
    });

var player = new AudioPlayer(
    pipeline,
    new AudioPlayerConfiguration()
    {
        DeviceName = "Remote Audio"
    });
```

The previous examples assume that both default capture and playback formats (sampling rate, channels, etc.) are identical. The audio format
may be explicitly specified by supplying a 'WaveFormat' value in the configuration object as shown in the following example:

```csharp
var format = WaveFormat.Create16kHz1Channel16BitPcm();
var source = new AudioSource(
    pipeline,
    new AudioSourceConfiguration()
    { 
        OutputFormat = format
    });

var player = new AudioPlayer(
    pipeline, 
    new AudioPlayerConfiguration() 
    { 
        InputFormat = format 
    });
```

### Capturing audio from a file and resampling

The `WaveFileAudioSource` component enables audio from a Wave file to be consumed as a \\psi stream. In the following example, a Wave file is
used to generate an audio stream, which is then resampled to a different format using the `AudioResampler` component. Resampling is necessary in
situations where the original audio format is not compatible with the format required by a downstream component that consumes the audio
(for example, a speech recognizer). In the example, the resampled audio is simply sent to an `AudioPlayer` component for playback.

```csharp
var source = new WaveFileAudioSource(pipeline, "recording.wav");
var player = new AudioPlayer(pipeline);
var resampler = new AudioResampler(
    pipeline,
    new AudioResamplerConfiguration()
    {
        OutputFormat = WaveFormat.Create16BitPcm(8000, 1)
    });

source.PipeTo(resampler);
resampler.PipeTo(player);
```

## Acoustic Feature Operators

The following operators are provided to manipulate raw audio samples and to compute acoustic features.

- `Dither` - Applies a random dither to an frame samples.
- `FFT` - Computes the Fast Fourier Transform of an audio frame.
- `FFTPower` - Computes the power spectral density from the FFT.
- `FrameShift` - Segments a stream of audio samples into (potentially overlapping) fixed-length frames.
- `FrequencyDomainEnergy` - Computes the energy within a frequency band.
- `HanningWindow` - Applies a Hanning window to an audio frame.
- `LogEnergy` - Computes the log energy of an audio frame.
- `SpectralEntropy` - Computes the spectral entropy within a frequency band.
- `ToFloat` - Converts raw audio samples to floating-point sample values.
- `ZeroCrossingRate` - Computes the zero-cross frequency of an audio frame.

While any one of these operators may be used individually, they are usually produced collectively using the `AcousticFeatures` component which
aggregates a set of commonly used acoustic feature streams into a single component. Configuration parameters specified in the `AcousticFeaturesConfiguration`
object determine which features to generate. Note that the audio input to this component is assumed to be 1-channel 16-bit PCM audio. Ensure that this format
is specified in `AudioSourceConfiguration` if using the `AudioSource` to capture live audio as input, or use the `AudioResampler` component to convert the
audio stream to the required format.
