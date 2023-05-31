# HoloLens Capture Exporter

The HoloLensCaptureExporter is a tool to convert data within \psi stores that have been collected by the [HoloLensCaptureServer](..\HoloLensCaptureServer) to other formats. Example usage:

```bash
> HoloLensCaptureExporter -p C:\data\Temp\HoloLensCapture.0009 -o C:\data\Temp\HoloLensCapture.0009\Export
```

This will open the store specified by `-p`, find and convert streams within, and export the results to the output directory given by `-o`.

## Options

The following options are available:

| Option | Abbr     | Description                                                           |
| ------ | -------- | --------------------------------------------------------------------- |
| `p`    | `path`   | Path to the input Psi data store.                                     |
| `o`    | `output` | Output path to export data to.                                        |
| `n`    | `name`   | Optional name of the input Psi data store (default: HoloLensCapture). |

## Output Formats

The following describes the output structure and formats that are exported by the tool.

### Primitives

#### Timestamps

Timestamps represent the originating time of sensor readings. They are in Coordinated Universal Time (UTC), represented by a single 64-bit integer counting 100-nanosecond "ticks" since 00:00:00 (midnight), January 1, 0001 (C.E.) in the Gregorian calendar.

For example, 603202464000000000 would be midnight on Alan Turing's birthday (23 JUN 1912).

#### Poses

Poses for cameras, head, and hand joints are represented by a coordinate system. These are persisted as tab-separated values from the underlying 4×4 matrix in row-major order. For example: 

```text
M₀₀       M₀₁        M₀₂       M₀₃       M₁₀       M₁₁        M₁₂       M₁₃        M₂₀        M₂₁       M₂₂       M₂₃       M₃₀ M₃₁ M₃₂ M₃₃
-------------------------------------------------------------------------------------------------------------------------------------------
0.947...  -0.010...  0.318...  0.081...  0.053...  0.990...  -0.125...  -0.003...  -0.314...  0.135...  0.939...  0.042...  0   0   0   1
```

#### Gaze

Eye gaze is represented by a 3D ray, and is persisted as the tab-separated x, y, and z values of the origin point (P) and then the direction vector (V). For example:

```text
P[x]      P[y]       P[z]      V[x]      V[y]      V[z]
-----------------------------------------------------------
0.547...  -0.710...  0.418...  0.267...  0.159...  0.990...
```

### Sensor Streams

The following are the HoloLens 2 sensor streams that are exported from the original \psi store.

#### IMU

##### Accelerometer

Accelerometer data is persisted to a text file (`Accelerometer/Accelerometer.txt`) containing newline delimited records, each containing the originating timestamp (see above), and the X-, Y-, and Z-axis inertial force in m/s² as tab-separated fields. For example:

```text
Originating Time    X-axis                Y-axis               Z-axis
----------------------------------------------------------------------------------
637835894408235676  -0.76258039474487305  -9.7197809219360352  -1.3236149549484253
637835894408244692  -0.7307015061378479   -9.7032041549682617  -1.2934621572494507
637835894408253708  -0.77219980955123901  -9.7326211929321289  -1.3198481798171997
...
```

##### Gyroscope

Gyroscope data is persisted to a text file (`Gyroscope/Gyroscope.txt`) containing newline delimited records, each containing the originating timestamp (see above), and the X-, Y-, and Z-axis angular momentum in rad/s as tab-separated fields. For example:

```text
Originating Time    X-axis                 Y-axis               Z-axis
-----------------------------------------------------------------------------------
637835894409333078  -0.015870876610279083  0.16464650630950928  -0.0266859270632267
637835894409334594  -0.068954452872276306  0.15596729516983032   0.0281371828168636
637835894409336109  -0.023053843528032303  0.21979841589927673  -0.0425699315965173
...
```

##### Magnetometer

Magnetometer data is persisted to a text file (`Magnetometer/Magnetometer.txt`) containing newline delimited records, each containing the originating timestamp (see above), and the X-, Y-, and Z-axis magnetic flux density in μT (microteslas) as tab-separated fields. For example:

```text
Originating Time    X-axis              Y-axis               Z-axis
-------------------------------------------------------------------------------
637835894403011447  261.45001220703125  -364.6500244140625   459.75003051757813
637835894403211379  261.75000012000007  -364.95001220703125  458.70001220703125
637835894403411400  262.20001220703125  -365.70001220703125  460.80001831054688
...
```

#### Head

Head pose data is persisted to a text file (`Head/Head.txt`) containing newline delimited records, each containing the originating timestamp (see above), and the pose (see above) values as tab-separated fields. For example:

```text
Originating Time    M₀₀       M₀₁        M₀₂       M₀₃       M₁₀       M₁₁       M₁₂        M₁₃        M₂₀        M₂₁       M₂₂       M₂₃       M₃₀ M₃₁ M₃₂ M₃₃
---------------------------------------------------------------------------------------------------------------------------------------------------------------
637835897125443909  0.947...  -0.010...  0.318...  0.081...  0.053...  0.990...  -0.125...  -0.003...  -0.314...  0.135...  0.939...  0.042...  0   0   0   1
637835897132944315  0.949...  -0.013...  0.314...  0.079...  0.057...  0.989...  -0.124...  -0.003...  -0.303...  0.140...  0.940...  0.042...  0   0   0   1
637835897135110505  0.945...  -0.023...  0.323...  0.080...  0.067...  0.989...  -0.127...  -0.005...  -0.317...  0.140...  0.937...  0.041...  0   0   0   1
...
```

#### Eyes

Eyes pose data is persisted to a text file (`Eyes/Eyes.txt`) containing newline delimited records, each containing the originating timestamp (see above), and the 3D ray (see above) values as tab-separated fields. For example:

```text
Originating Time    P[x]      P[y]       P[z]      V[x]      V[y]      V[z]
-------------------------------------------------------------------------------
637835897125443909  0.547...  -0.710...  0.418...  0.267...  0.159...  0.990...
637835897132944315  0.547...  -0.712...  0.418...  0.267...  0.159...  0.960...
637835897135110505  0.548...  -0.712...  0.419...  0.297...  0.160...  0.960...
...
```

#### Hands

Hand pose data is persisted to two text files (`Hands/Left.txt`, `Hands/Right.txt`) containing newline delimited records, each containing the originating timestamp (see above), the `IsActive` boolean flag (`0` = false, `1` = true), the pose of each joint, the valid state of each joint, and the tracked state for each joint. For example:

```text
Originating Time    Active  M₀₀        M₀₁        M₀₂       M₀₃       M₁₀        M₁₁        M₁₂        M₁₃       M₂₀       M₂₁        M₂₂        M₂₃        M₃₀ M₃₁ M₃₂ M₃₃  M₀₀       M₀₁        M₀₂       ...  V₁ V₂ ... V₂₆  T₁ T₂ ... T₂₆
------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
637835897125443909  1       -0.214...  -0.336...  0.916...  0.201...  -0.531...  -0.747...  -0.399...  0.110...  0.819...  -0.573...  -0.018...  -0.460...  0   0   0   1    0.916...  -0.336...  0.214...  ...  1  1  ... 1    1  1  ... 1
637835897125447654  0       NaN        NaN        NaN       NaN       NaN        NaN        NaN        NaN       NaN       NaN        NaN        NaN        NaN NaN NaN NaN  NaN       NaN        NaN       ...  0  0  ... 0    0  0  ... 0
```

The first value is the flag indicating active state. This is followed by sets of 16 doubles for each of the following 26 joints (416 values), followed by 26 boolean values (`0` or `1`) for the valid state of each joint (Vⱼ), followed by 26 boolean values (`0` or `1`) for the tracked state of each joint (Tⱼ). Note that missing/invalid joint values may be represented by `NaN` as in the second record above.

- Palm
- Wrist
- ThumbMetacarpal
- ThumbProximal
- ThumbDistal
- ThumbTip
- IndexMetacarpal
- IndexProximal
- IndexIntermediate
- IndexDistal
- IndexTip
- MiddleMetacarpal
- MiddleProximal
- MiddleIntermediate
- MiddleDistal
- MiddleTip
- RingMetacarpal
- RingProximal
- RingIntermediate
- RingDistal
- RingTip
- PinkyMetacarpal
- PinkyProximal
- PinkyIntermediate
- PinkyDistal
- PinkyTip

#### Audio

Audio buffers are persisted to a WAVE file (`Audio/Audio.wav`) containing IEEE float encoded, 48KHz, single channel data.

Additionally, a set of audio buffer files in the form (`Audio000123.bin`) are persisted to the `Audio/Buffers` directory. Per-buffer originating timestamps are persisted to a `Timing.txt` file as a tab-separated pair of frame number and timestamp. The originating times listed within `Timing.txt` represent the *ending* time of each buffer. For example:

```text
Frame  Originating Time
-------------------------
0      637835897126152279
1      637835897132152359
2      637835897142152493
```

#### Video

The following camera streams are potentially available (depending on the configuration of the HoloLensCaptureApp):

- Video - color front-facing camera.
- Preview - mixed reality preview combining video with holograms.
- Infrared - infrared view from the depth camera.
- Depth - far-depth camera.
- AhatDepth - near-depth camera.
- LeftFront - gray-scale camera.
- RightFront - gray-scale camera.
- LeftLeft - gray-scale camera.
- RightRight - gray-scale camera.

Data from each camera is persisted in separate folder (e.g. `Video/`, `Depth/`, `LeftFront/`, ...).

##### Frame Images

A set of frame-by-frame image files in the form (`000001.jpg`, `000071.png`) are persisted. If the original stream is encoded (depending on HoloLensCaptureApp configuration), then frames are persisted as JPEG files. If the original stream is GZIPed or unencoded, then frames are persisted as lossless PNG files.

##### Timings

Per-frame originating timestamps are persisted to a `Timing.txt` file as a tab-separated pair of frame number and timestamp. For example:

```text
Frame  Originating Time
-------------------------
0      637835897126152279
1      637835897132152359
2      637835897142152493
```

##### Camera Pose

The pose of the camera over time is persisted to a `Pose.txt` file containing newline delimited records, each containing the originating timestamp (see above), and the pose (see above) values as tab-separated fields. For example:

```text
Originating Time    M₀₀       M₀₁        M₀₂       M₀₃       M₁₀       M₁₁       M₁₂        M₁₃        M₂₀        M₂₁       M₂₂       M₂₃       M₃₀ M₃₁ M₃₂ M₃₃
---------------------------------------------------------------------------------------------------------------------------------------------------------------
637835897126152279  0.768...  -0.013...  0.639...  0.147...  0.097...  0.990...  -0.097...  -0.011...  -0.631...  0.136...  0.762...  0.041...  0   0   0   1
```

##### Camera Intrinsics

The intrinsics of the camera are persisted to an `Intrinsics.txt` file containing tab-separated fields containing the intrinsics matrix, distortion parameters, focal length information, the principal point, etc. as described in detail below.

```text
M₀₀         M₀₁  M₀₂         M₁₀  M₁₁         M₁₂         M₂₀  M₂₁  M₂₂  R₀         R₁        R₂  R₃  R₄  R₅  T₀  T₁  FL          FLₓ         FLᵧ         PPₓ         PPᵧ         D   W   H
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
251.437...  0    160.242...  0    251.647...  168.953...  0    0    1    -0.282...  0.064...  0   0   0   0   0   0   251.542...  251.437...  251.647...  160.242...  168.953...  1   320 288
```

The first 9 values represent the 3×3 intrinsics matrix (M₀₀ - M₂₂). This transform converts camera coordinates (in the camera's local space) into normalized device coordinates (NDC) ranging from -1 .. +1.

The next 6 values represent the radial distortion parameters (R₀ - R₅).

The next 2 values represent the tangential distortion parameters (T₀, T₁).

The next value represents the focal length in pixels (FL).

The next 2 values represent the focal length separated in X and Y in pixels (FLₓ, FLᵧ).

The next 2 values represent the principal point in pixels (PPₓ, PPᵧ).

The next value is a boolean indicating whether the closed form equation of the Brown-Conrady Distortion model distorts or undistorts (D).

The final two values represent the image width (W) and height (H) in pixels.

##### Calibration Maps

When configured in the HoloLensCaptureApp (`includeDepthCalibrationMap = true`), pixel-by-pixel calibration maps are persisted to a `CalibrationMap.txt` file containing tab-separated fields describing the width (W), height (H) and per-pixel values of points on the camera unit plane (P₀ - Pₙ).

```text
W    H    P₀     P₁     ...  Pₙ
----------------------------------
640  480  0.123  0.456  ...  0.789
```

##### Video MPEG

Image frames (just for the front-facing color camera) are also exported to a single `Video.mpeg` video file in the `Video/` folder. Audio is also included in the video if available.

The start and end times of the MPEG video are recorded in `VideoMpegTiming.txt`, containing two lines corresponding to the starting ticks (earliest of the first video frame and start of the first audio buffer originating timestamps) and the ending ticks (latest of the last video frame and end of the last audio buffer). Note that the audio begins at the *start* of the first buffer and ends at the *end* of the last buffer. Also note that audio is resampled from 32-bit IEEE Float format to 16-bit PCM and so audio buffers written to the MPEG have different sizes (1920 bytes vs. 16384 bytes) compared to those of the source audio stream and the timings listed in `VideoMpegTiming.txt` may not exactly match with information in the `Audio\Buffers\Timing.txt` file.

#### Scene Understanding

The [scene understanding](https://docs.microsoft.com/en-us/windows/mixed-reality/design/scene-understanding) stream contains information about observed surfaces in the environment. This includes integrated 3D meshes and rectangular "flat" areas. Sets of scene information is classified into the following categories:

- __World__ - The World objects are _all_ of the observed surfaces.
- __Inferred__ - Inferred objects are generally bits of mesh to fill in unobserved, but assumed, portions of surfaces.
- __Background__ - Known to be not one of the other recognized kinds of scene object. This class shouldn't be confused with Unknown where Background is known not to be wall/floor/ceiling etc. while Unknown isn't yet categorized.
- __Wall__ - A physical wall. Walls are assumed to be immovable environmental structures.
- __Floor__ - Floors are any surfaces on which one can walk. Note: stairs aren't floors. Also note, that floors assume any walkable surface and therefore there's no explicit assumption of a singular floor. Multi-level structures, ramps etc. should all classify as floor.
- __Ceiling__ - The upper surface of a room.
- __Platform__ - A large flat surface on which you could place holograms. These tend to represent tables, countertops, and other large horizontal surfaces.
- __Unknown__ - This scene object has yet to be classified and assigned a kind. This shouldn't be confused with Background, as this object could be anything, the system has just not come up with a strong enough classification for it yet.

Each of these classes of scene objects are given a directory within `SceneUnderstanding` (e.g. `SceneUnderstanding/World`, `SceneUnderstanding/Floor`, ...). Within each directory there are two text files and two subdirectories containing meshes.

`Rectangles.txt` contains flat surface rectangles defined by the corner points. Newline delimited records, each contain the originating timestamp (see above), and sets of four pairs (8) of tab-separated fields defining a rectangle (in top-left, top-right, bottom-left, bottom-right order). Zero or more rectangles may be present for a given timestamp. Each rectangle is followed by a "placement rectangle" which represents a smaller rectangle within the first that is deemed the best area to place something (e.g. the best area on a table surface, roughly centered but without intersecting objects). If no suitable placement area is found, then 8 `NaN` values will be given. For example:

```text
Originating Time    R₀ₓ        R₀ᵧ        R₁ₓ        R₁ᵧ        R₂ₓ        R₂ᵧ        R₃ₓ        R₃ᵧ        P₀ₓ  P₀ᵧ  P₁ₓ  P₁ᵧ  P₂ₓ  P₂ᵧ  P₃ₓ  P₃ᵧ ...
------------------------------------------------------------------------------------------------------------------------------------------------------
637835897112518938  -4.574...  -1.062...  -4.144...   2.392...   0.701...  -1.718...   1.736...  -1.745...  NaN  NaN  NaN  NaN  NaN  NaN  NaN  NaN ...
637835897712518938   1.143...   1.832...   0.701...  -1.725...  -4.133...   2.486...  -4.574...  -1.071...  NaN  NaN  NaN  NaN  NaN  NaN  NaN  NaN ...
```

A `Meshes.txt` file contains counts of the number of meshes exported at each timestamp. Meshes come in two flavors: plain `Meshes` which may contain a high degree of detail and `ColliderMeshes` which may be simplified and are intended for collision and occlusion rather than high fidelity rendering. Newline delimited records, each contain the originating timestamp (see above), and the count of meshes and collider meshes. For example:

```text
Originating Time    Meshes  Collider
------------------------------------
637835897112518938	9	    9
637835897712518938	9	    9
```

Within the subfolders `/Meshes` and `/ColliderMeshes`, [mesh `.obj` files](https://en.wikipedia.org/wiki/Wavefront_.obj_file) are exported. Directories are created at each timestamp (e.g. `Meshes/637835897112518938`) and files named `Mesh0.obj`, `Mesh1.obj`, are exported.

### Debugging

The following streams are for debugging purposes and are not persisted, but may be viewed in the original \psi store using PsiStudio.

- HoloLensDiagnostics
- DepthDebugOutOfOrderFrames
- AhatDebugOutOfOrderFrames
