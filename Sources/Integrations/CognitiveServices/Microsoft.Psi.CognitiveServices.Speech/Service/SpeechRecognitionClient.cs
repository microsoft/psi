// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Provides a client for the Cognitive Services Speech service.
    /// </summary>
    internal sealed class SpeechRecognitionClient : IDisposable
    {
        private ClientWebSocket webSocket;
        private IAuthentication authentication;
        private Task receiverTask;
        private string subscriptionKey;
        private string region;
        private string language;
        private string connectionId;
        private string requestId;
        private SpeechRecognitionMode recognitionMode;
        private WaveFormat audioFormat;

        // async-waitable lock for webSocket
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechRecognitionClient"/> class.
        /// </summary>
        /// <param name="recognitionMode">The recognition mode to use.</param>
        /// <param name="language">The recognition language.</param>
        /// <param name="subscriptionKey">The speech service API key.</param>
        /// <param name="region">The speech service region associated to the subscription.</param>
        public SpeechRecognitionClient(SpeechRecognitionMode recognitionMode, string language, string subscriptionKey, string region = null)
        {
            this.webSocket = new ClientWebSocket();
            this.region = region?.Replace(" ", string.Empty); // convert to region endpoint URL string
            if (string.IsNullOrWhiteSpace(this.region))
            {
                this.authentication = new Authentication(subscriptionKey);
            }
            else
            {
                this.authentication = new AzureAuthentication(subscriptionKey, this.region);
            }

            this.subscriptionKey = subscriptionKey;
            this.language = language;
            this.recognitionMode = recognitionMode;
        }

        /// <summary>
        /// An event that is raised when a final speech recognition response has beeen received.
        /// </summary>
        public event EventHandler<SpeechResponseEventArgs> OnResponseReceived;

        /// <summary>
        /// An event that is raised when a partial speech recognition response has beeen received.
        /// </summary>
        public event EventHandler<PartialSpeechResponseEventArgs> OnPartialResponseReceived;

        /// <summary>
        /// An event that is raised in response to a service error.
        /// </summary>
        public event EventHandler<SpeechErrorEventArgs> OnConversationError;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.authentication.Dispose();
            this.semaphore.Dispose();
        }

        /// <summary>
        /// Sets the format of the audio that will be sent to the service.
        /// </summary>
        /// <param name="audioFormat">The audio format.</param>
        public void SetAudioFormat(WaveFormat audioFormat)
        {
            this.audioFormat = audioFormat;
        }

        /// <summary>
        /// Sends the next chunk of audio in the audio stream to the speech service.
        /// </summary>
        /// <param name="audioBytes">The raw audio.</param>
        /// <param name="token">A task cancellation token.</param>
        /// <param name="forceReconnect">Indicates whether a new connection should be opened.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task SendAudioAsync(byte[] audioBytes, CancellationToken token, bool forceReconnect = false)
        {
            await this.semaphore.WaitAsync();
            try
            {
                int offset = 0;

                if (forceReconnect || this.webSocket.State != WebSocketState.Open)
                {
                    await this.InitializeConnectionAsync(token);
                }

                while (offset < audioBytes.Length)
                {
                    int count = Math.Min(audioBytes.Length - offset, 8192);
                    await this.SendMessageAsync(new AudioMessage(audioBytes, offset, count), this.requestId, token);
                    offset += count;
                }
            }
            catch (WebSocketException wse)
            {
                this.RaiseTranslateError(wse);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Sends an empty audio packet to signal the end of the audio stream.
        /// </summary>
        /// <param name="token">A task cancellation token.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task SendEndAudioAsync(CancellationToken token)
        {
            await this.semaphore.WaitAsync(token);
            try
            {
                if (this.webSocket.State == WebSocketState.Open)
                {
                    await this.SendMessageAsync(new AudioMessage(), this.requestId, token);
                }
            }
            catch (WebSocketException wse)
            {
                this.RaiseTranslateError(wse);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Initializes the connection to the service and prepares it to receive audio for recognition.
        /// </summary>
        /// <param name="token">A task cancellation token.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task InitializeConnectionAsync(CancellationToken token)
        {
            do
            {
                this.webSocket = await this.ConnectAsync(token);
                this.requestId = this.GetNewGuid();
                await this.SendSpeechConfigAsync(token);
                await this.SendAudioFormatAsync(this.audioFormat, token);
            }
            while (!(await this.ReceiveMessageAsync(this.webSocket, this.requestId, token) is TurnStartMessage));

            this.receiverTask = this.ReceiveAsync(this.webSocket, this.requestId, token);
        }

        /// <summary>
        /// Closes the connection to the service.
        /// </summary>
        /// <param name="webSocket">The WebSocket to close.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task CloseConnectionAsync(WebSocket webSocket)
        {
            await this.semaphore.WaitAsync();
            try
            {
                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Close requested", CancellationToken.None);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Establishes a new web socket connection to the service.
        /// </summary>
        /// <param name="token">A task cancellation token.</param>
        /// <returns>
        /// A Task representing the asynchronous operation whose result will contain a new
        /// ClientWebSocket for the connection upon task completion.
        /// </returns>
        private async Task<ClientWebSocket> ConnectAsync(CancellationToken token)
        {
            this.connectionId = this.GetNewGuid();

            var webSocket = new ClientWebSocket();
            webSocket.Options.SetRequestHeader("X-ConnectionId", this.connectionId);
            webSocket.Options.SetRequestHeader("Authorization", "Bearer " + this.authentication.GetAccessToken());
            if (!string.IsNullOrWhiteSpace(this.region))
            {
                var uri = new Uri($"wss://{this.region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language={this.language}&format=detailed");
                await webSocket.ConnectAsync(uri, token);
            }
            else
            {
                await webSocket.ConnectAsync(
                    new Uri(
                        "wss://speech.platform.bing.com" +
                        $"/speech/recognition/{this.recognitionMode.ToString().ToLower()}/cognitiveservices/v1" +
                        "?format=detailed" +
                        "&language=" + this.language),
                    token);
            }

            return webSocket;
        }

        /// <summary>
        /// Sends the speech.config message to the service.
        /// </summary>
        /// <param name="token">A task cancellation token.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task SendSpeechConfigAsync(CancellationToken token)
        {
            await this.SendMessageAsync(new SpeechConfigMessage(), this.requestId, token);
        }

        /// <summary>
        /// Sends the audio format to the service. This needs to be the first audio message for any new connection.
        /// </summary>
        /// <param name="format">The audio format.</param>
        /// <param name="token">A task cancellation token.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task SendAudioFormatAsync(WaveFormat format, CancellationToken token)
        {
            await this.SendMessageAsync(new AudioMessage(format), this.requestId, token);
        }

        /// <summary>
        /// Sends a message to the service.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="requestId">The request id associated with the message.</param>
        /// <param name="token">A task cancellation token.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task SendMessageAsync(SpeechServiceMessage message, string requestId, CancellationToken token)
        {
            message.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
            message.Headers["X-RequestId"] = requestId;

            await this.webSocket.SendAsync(
                new ArraySegment<byte>(message.GetBytes()),
                message is SpeechServiceBinaryMessage ? WebSocketMessageType.Binary : WebSocketMessageType.Text,
                true,
                token);
        }

        /// <summary>
        /// Receives a single message from the service.
        /// </summary>
        /// <param name="webSocket">The connected WebSocket on which to listen for the message.</param>
        /// <param name="requestId">The request ID associated with the message.</param>
        /// <param name="token">A task cancellation token.</param>
        /// <returns>
        /// A Task representing the asynchronous operation whose result will contain the message
        /// received from the service upon task completion.
        /// </returns>
        private async Task<SpeechServiceMessage> ReceiveMessageAsync(WebSocket webSocket, string requestId, CancellationToken token)
        {
            WebSocketReceiveResult result = null;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192]);

            using (var stream = new MemoryStream())
            {
                // Read incoming message into a MemoryStream (potentially in multiple chunks)
                do
                {
                    try
                    {
                        result = await webSocket.ReceiveAsync(buffer, token);
                    }
                    catch (OperationCanceledException)
                    {
                        continue;
                    }

                    stream.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (result != null && !result.EndOfMessage);

                token.ThrowIfCancellationRequested();

                stream.Seek(0, SeekOrigin.Begin);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var message = reader.ReadToEnd();

                        var speechServiceMessage = SpeechServiceMessage.Deserialize(message);

                        if (speechServiceMessage.RequestId == requestId)
                        {
                            return speechServiceMessage;
                        }
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (webSocket.CloseStatus.HasValue)
                    {
                        // Translate the close status to a ConversationError and raise it
                        this.RaiseTranslatedError(webSocket.CloseStatus.Value, webSocket.CloseStatusDescription);
                    }

                    return null;
                }

                // No other message types are supported
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Listens for messages received from the service and handles them appropriately.
        /// </summary>
        /// <param name="webSocket">The connected WebSocket on which to listen for the message.</param>
        /// <param name="requestId">The request ID associated with the conversation.</param>
        /// <param name="token">A task cancellation token.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task ReceiveAsync(WebSocket webSocket, string requestId, CancellationToken token)
        {
            try
            {
                while (webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var speechServiceMessage = await this.ReceiveMessageAsync(webSocket, requestId, token);

                    if (speechServiceMessage is SpeechPhraseMessage)
                    {
                        RecognitionResult phraseResponse = new RecognitionResult((SpeechPhraseMessage)speechServiceMessage);
                        this.OnResponseReceived?.Invoke(this, new SpeechResponseEventArgs(phraseResponse));
                    }
                    else if (speechServiceMessage is SpeechHypothesisMessage)
                    {
                        PartialRecognitionResult partialResponse = new PartialRecognitionResult((SpeechHypothesisMessage)speechServiceMessage);
                        this.OnPartialResponseReceived?.Invoke(this, new PartialSpeechResponseEventArgs(partialResponse));
                    }
                    else if (speechServiceMessage is SpeechFragmentMessage)
                    {
                        PartialRecognitionResult partialResponse = new PartialRecognitionResult((SpeechFragmentMessage)speechServiceMessage);
                        this.OnPartialResponseReceived?.Invoke(this, new PartialSpeechResponseEventArgs(partialResponse));
                    }
                    else if (speechServiceMessage is TurnEndMessage || speechServiceMessage == null)
                    {
                        // End of turn - stop listening
                        break;
                    }
                }
            }
            catch (WebSocketException e)
            {
                this.RaiseTranslateError(e);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await this.CloseConnectionAsync(webSocket);
            }
        }

        /// <summary>
        /// Translates a WebSocketCloseStatus and raises a conversation error if appropriate.
        /// </summary>
        /// <param name="status">The WebSocketCloseStatus code.</param>
        /// <param name="message">The associated WebSocket CloseStatusDescription.</param>
        private void RaiseTranslatedError(WebSocketCloseStatus status, string message)
        {
            switch (status)
            {
                case WebSocketCloseStatus.ProtocolError:
                    this.RaiseOnConversationError(SpeechClientStatus.WebSocketProtocolError, message);
                    break;

                case WebSocketCloseStatus.InvalidPayloadData:
                    this.RaiseOnConversationError(SpeechClientStatus.WebSocketInvalidPayloadData, message);
                    break;

                case WebSocketCloseStatus.InternalServerError:
                    this.RaiseOnConversationError(SpeechClientStatus.WebSocketServerError, message);
                    break;
            }
        }

        /// <summary>
        /// Translates an exception and raises the appropriate conversation error event.
        /// </summary>
        /// <param name="error">The exception to translate.</param>
        private void RaiseTranslateError(WebSocketException error)
        {
            var innerError = error.InnerException;

            if (innerError is WebException webError &&
                webError.Response is HttpWebResponse response)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        this.RaiseOnConversationError(SpeechClientStatus.HttpBadRequest, webError.Message);
                        break;

                    case HttpStatusCode.Unauthorized:
                        this.RaiseOnConversationError(SpeechClientStatus.HttpUnauthorized, webError.Message);
                        break;

                    case HttpStatusCode.Forbidden:
                        this.RaiseOnConversationError(SpeechClientStatus.HttpForbidden, webError.Message);
                        break;

                    case HttpStatusCode.NotFound:
                        this.RaiseOnConversationError(SpeechClientStatus.HttpNotFound, webError.Message);
                        break;

                    case HttpStatusCode.InternalServerError:
                        this.RaiseOnConversationError(SpeechClientStatus.HttpServerError, webError.Message);
                        break;

                    case HttpStatusCode.ServiceUnavailable:
                        this.RaiseOnConversationError(SpeechClientStatus.HttpServiceUnavailable, webError.Message);
                        break;
                }
            }
        }

        /// <summary>
        /// Raises a ConversationError event.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The message for the event.</param>
        private void RaiseOnConversationError(SpeechClientStatus errorCode, string message)
        {
            this.OnConversationError?.Invoke(this, new SpeechErrorEventArgs(errorCode, message));
        }

        /// <summary>
        /// Generates a new random UUID in a format that may be used for connection or request IDs.
        /// </summary>
        /// <returns>The UUID string.</returns>
        private string GetNewGuid()
        {
            return Guid.NewGuid().ToString().Replace("-", string.Empty);
        }
    }
}
