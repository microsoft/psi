// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Interop.Rendezvous;

    /// <summary>
    /// Defines an interface for communication streams between client and server applications.
    /// </summary>
    public interface IClientServerCommunicationStreams
    {
        /// <summary>
        /// Writes the streams to an exporter.
        /// </summary>
        /// <param name="prefix">The prefix to write the streams under.</param>
        /// <param name="exporter">The exporter.</param>
        public void Write(string prefix, Exporter exporter);

        /// <summary>
        /// Writes the streams to a rendezvous process.
        /// </summary>
        /// <param name="rendezvousProcess">The rendezvous process.</param>
        /// <param name="address">The address.</param>
        /// <param name="prefix">An optional prefix to write the streams under.</param>
        public void WriteToRendezvousProcess(Rendezvous.Process rendezvousProcess, string address, string prefix = null);
    }
}
