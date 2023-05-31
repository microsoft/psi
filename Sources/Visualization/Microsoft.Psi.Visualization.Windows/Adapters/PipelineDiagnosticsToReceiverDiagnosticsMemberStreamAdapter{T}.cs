// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Linq;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from pipeline diagnostics to a specific receiver statistic.
    /// </summary>
    /// <typeparam name="T">The type of the diagnostic statistic.</typeparam>
    public class PipelineDiagnosticsToReceiverDiagnosticsMemberStreamAdapter<T> : StreamAdapter<PipelineDiagnostics, T?>
        where T : struct
    {
        private readonly int receiverId = -1;
        private readonly Func<PipelineDiagnostics.ReceiverDiagnostics, T> memberFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnosticsToReceiverDiagnosticsMemberStreamAdapter{T}"/> class.
        /// </summary>
        /// <param name="receiverId">The receiver id.</param>
        /// <param name="memberFunc">A function that given the receiver diagnostics provides the statistic of interest.</param>
        public PipelineDiagnosticsToReceiverDiagnosticsMemberStreamAdapter(int receiverId, Func<PipelineDiagnostics.ReceiverDiagnostics, T> memberFunc)
        {
            this.receiverId = receiverId;
            this.memberFunc = memberFunc;
        }

        /// <inheritdoc/>
        public override T? GetAdaptedValue(PipelineDiagnostics source, Envelope envelope)
        {
            var receiver = source?.GetAllReceiverDiagnostics()?.FirstOrDefault(rd => rd.Id == this.receiverId);
            if (receiver == null)
            {
                return null;
            }
            else
            {
                return this.memberFunc(receiver);
            }
        }
    }
}
