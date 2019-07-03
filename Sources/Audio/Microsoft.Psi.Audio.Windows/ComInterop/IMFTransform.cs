// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IMFTransform COM interface (defined in Mftransform.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IMFTransformIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFTransform
    {
        /// <summary>
        /// Retrieves the minimum and maximum number of input and output streams.
        /// </summary>
        /// <param name="inputMinimum">The minimum number of input streams.</param>
        /// <param name="inputMaximum">The maximum number of input streams.</param>
        /// <param name="outputMinimum">The minimum number of output streams.</param>
        /// <param name="outputMaximum">The maximum number of output streams.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetStreamLimits(out int inputMinimum, out int inputMaximum, out int outputMinimum, out int outputMaximum);

        /// <summary>
        /// Retrieves the current number of input and output streams on this MFT.
        /// </summary>
        /// <param name="inputStreams">The number of input streams.</param>
        /// <param name="outputStreams">The number of output streams.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetStreamCount(out int inputStreams, out int outputStreams);

        /// <summary>
        /// Retrieves the stream identifiers for the input and output streams on this MFT.
        /// </summary>
        /// <param name="inputIDArraySize">Number of elements in the inputIDs array. </param>
        /// <param name="inputIDs">An array of input stream identifiers.</param>
        /// <param name="outputIDArraySize">Number of elements in the outputIDs array.</param>
        /// <param name="outputIDs">An array of output stream identifiers.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetStreamIDs(int inputIDArraySize, out int inputIDs, int outputIDArraySize, out int outputIDs);

        /// <summary>
        /// Retrieves the buffer requirements and other information for an input stream.
        /// </summary>
        /// <param name="inputStreamID">The input stream identifier.</param>
        /// <param name="streamInfo">An MFTInputStreamInfo structure containing information about the stream.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetInputStreamInfo(int inputStreamID, out MFTInputStreamInfo streamInfo);

        /// <summary>
        /// Retrieves the buffer requirements and other information for an output stream on this MFT.
        /// </summary>
        /// <param name="outputStreamID">The output stream identifier.</param>
        /// <param name="streamInfo">An MFTInputStreamInfo structure containing information about the stream.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetOutputStreamInfo(int outputStreamID, out MFTInputStreamInfo streamInfo);

        /// <summary>
        /// Retrieves the attribute store for this MFT.
        /// </summary>
        /// <param name="attributes">An IMFAttributes interface.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetAttributes(out IMFAttributes attributes);

        /// <summary>
        /// Retrieves the attribute store for an input stream on this MFT.
        /// </summary>
        /// <param name="inputStreamID">The input stream identifier.</param>
        /// <param name="attributes">An IMFAttributes interface.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetInputStreamAttributes(int inputStreamID, out IMFAttributes attributes);

        /// <summary>
        /// Retrieves the attribute store for an output stream on this MFT.
        /// </summary>
        /// <param name="outputStreamID">The output stream identifier.</param>
        /// <param name="attributes">An IMFAttributes interface.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetOutputStreamAttributes(int outputStreamID, out IMFAttributes attributes);

        /// <summary>
        /// Removes an input stream from this MFT.
        /// </summary>
        /// <param name="streamID">The input stream identifier.</param>
        /// <returns>An HRESULT return code.</returns>
        int DeleteInputStream(int streamID);

        /// <summary>
        /// Adds one or more new input streams to this MFT.
        /// </summary>
        /// <param name="streams">Number of streams to add.</param>
        /// <param name="streamIDs">An array of input stream identifiers.</param>
        /// <returns>An HRESULT return code.</returns>
        int AddInputStreams(int streams, int[] streamIDs);

        /// <summary>
        /// Retrieves a possible media type for an input stream on this MFT.
        /// </summary>
        /// <param name="inputStreamID">Input stream identifier.</param>
        /// <param name="typeIndex">Index of the media type to retrieve.</param>
        /// <param name="type">The IMFMediaType interface.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetInputAvailableType(int inputStreamID, int typeIndex, out IMFMediaType type);

        /// <summary>
        /// Retrieves an available media type for an output stream on this MFT.
        /// </summary>
        /// <param name="outputStreamID">Output stream identifier.</param>
        /// <param name="typeIndex">Index of the media type to retrieve.</param>
        /// <param name="type">The IMFMediaType interface.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetOutputAvailableType(int outputStreamID, int typeIndex, out IMFMediaType type);

        /// <summary>
        /// Sets, tests, or clears the media type for an input stream on this MFT.
        /// </summary>
        /// <param name="inputStreamID">Input stream identifier.</param>
        /// <param name="type">The IMFMediaType interface.</param>
        /// <param name="flags">Zero or more flags from the _MFT_SET_TYPE_FLAGS enumeration.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetInputType(int inputStreamID, IMFMediaType type, int flags);

        /// <summary>
        /// Sets, tests, or clears the media type for an output stream on this MFT.
        /// </summary>
        /// <param name="outputStreamID">Output stream identifier.</param>
        /// <param name="type">The IMFMediaType interface.</param>
        /// <param name="flags">Zero or more flags from the _MFT_SET_TYPE_FLAGS enumeration.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetOutputType(int outputStreamID, IMFMediaType type, int flags);

        /// <summary>
        /// Retrieves the current media type for an input stream on this MFT.
        /// </summary>
        /// <param name="inputStreamID">Input stream identifier.</param>
        /// <param name="type">The IMFMediaType interface.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetInputCurrentType(int inputStreamID, out IMFMediaType type);

        /// <summary>
        /// Retrieves the current media type for an output stream on this MFT.
        /// </summary>
        /// <param name="outputStreamID">Output stream identifier.</param>
        /// <param name="type">The IMFMediaType interface.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetOutputCurrentType(int outputStreamID, out IMFMediaType type);

        /// <summary>
        /// Queries whether an input stream on this MFT can accept more data.
        /// </summary>
        /// <param name="inputStreamID">Input stream identifier.</param>
        /// <param name="flags">A member of the _MFT_INPUT_STATUS_FLAGS enumeration, or zero.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetInputStatus(int inputStreamID, out int flags);

        /// <summary>
        /// Queries whether the transform is ready to produce output data.
        /// </summary>
        /// <param name="flags">A member of the _MFT_OUTPUT_STATUS_FLAGS enumeration, or zero.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetOutputStatus(out int flags);

        /// <summary>
        /// Sets the range of timestamps the client needs for output.
        /// </summary>
        /// <param name="hnsLowerBound">Specifies the earliest time stamp.</param>
        /// <param name="hnsUpperBound">Specifies the latest  time stamp.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetOutputBounds(long hnsLowerBound, long hnsUpperBound);

        /// <summary>
        /// Sends an event to an input stream on this MFT.
        /// </summary>
        /// <param name="inputStreamID">Input stream identifier.</param>
        /// <param name="mediaEvent">The IMFMediaEvent interface of an event object.</param>
        /// <returns>An HRESULT return code.</returns>
        int ProcessEvent(int inputStreamID, IMFMediaEvent mediaEvent);

        /// <summary>
        /// Sends a message to the MFT.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="param">Message parameter. The meaning of this parameter depends on the message type.</param>
        /// <returns>An HRESULT return code.</returns>
        int ProcessMessage(MFTMessageType message, [In] IntPtr param);

        /// <summary>
        /// Delivers data to an input stream on this MFT.
        /// </summary>
        /// <param name="inputStreamID">Input stream identifier.</param>
        /// <param name="sample">The IMFSample interface of the input sample.</param>
        /// <param name="flags">Reserved. Must be zero. </param>
        /// <returns>An HRESULT return code.</returns>
        [PreserveSig]
        int ProcessInput(int inputStreamID, IMFSample sample, int flags);

        /// <summary>
        /// Generates output from the current input data.
        /// </summary>
        /// <param name="flags">Bitwise OR of zero or more flags from the _MFT_PROCESS_OUTPUT_FLAGS enumeration.</param>
        /// <param name="outputBufferCount">Number of elements in the outputSamples array (must be 1).</param>
        /// <param name="outputSamples">A reference to an MFTOutputDataBuffer structure.</param>
        /// <param name="pdwStatus">A bitwise OR of zero or more flags from the _MFT_PROCESS_OUTPUT_STATUS enumeration.</param>
        /// <returns>An HRESULT return code.</returns>
        [PreserveSig]
        int ProcessOutput(int flags, int outputBufferCount, [Out, In] ref MFTOutputDataBuffer outputSamples, out int pdwStatus);
    }
}
