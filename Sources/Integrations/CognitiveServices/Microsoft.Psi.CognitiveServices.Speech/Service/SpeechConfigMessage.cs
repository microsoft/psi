// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents a speech.config message from the service.
    /// </summary>
    internal class SpeechConfigMessage : SpeechServiceTextMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechConfigMessage"/> class.
        /// </summary>
        public SpeechConfigMessage()
            : base("speech.config")
        {
        }

        /// <summary>
        /// Gets or sets the system element of the speech.config message.
        /// </summary>
        public SystemInfo System { get; set; } = new SystemInfo();

        /// <summary>
        /// Gets or sets the os element of the speech.config message.
        /// </summary>
        public OsInfo Os { get; set; } = new OsInfo();

        /// <summary>
        /// Gets or sets the device element of the speech.config message.
        /// </summary>
        public DeviceInfo Device { get; set; } = new DeviceInfo();

        /// <summary>
        /// Represents the system element of the speech.config message.
        /// </summary>
        internal class SystemInfo
        {
            /// <summary>
            /// Gets the version of the speech SDK software used by the client.
            /// </summary>
            public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        /// <summary>
        /// Represents the os element of the speech.config message.
        /// </summary>
        internal class OsInfo
        {
            /// <summary>
            /// Gets the OS platform that hosts the application, for example, Windows, Android, iOS, or Linux.
            /// </summary>
            public string Platform
            {
                get
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        return "Windows";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        return "Linux";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        return "OSX";
                    }
                    else
                    {
                        return "Unknown";
                    }
                }
            }

            /// <summary>
            /// Gets the OS product name, for example, Debian or Windows 10.
            /// </summary>
            public string Name => RuntimeInformation.OSDescription;

            /// <summary>
            /// Gets the version of the OS in the form major.minor.build.branch.
            /// </summary>
            public string Version => Environment.OSVersion.Version.ToString();
        }

        /// <summary>
        /// Represents the device element of the speech.config message.
        /// </summary>
        internal class DeviceInfo
        {
            /// <summary>
            /// Gets the device hardware manufacturer.
            /// </summary>
            public string Manufacturer => "Unknown";

            /// <summary>
            /// Gets the device model.
            /// </summary>
            public string Model => "Unknown";

            /// <summary>
            /// Gets the device software version provided by the device manufacturer.
            /// </summary>
            public string Version => "Unknown";
        }
    }
}
