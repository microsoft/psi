// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents diagnostic information about a pipeline.
    /// </summary>
    public static class DiagnosticsQueryExtensions
    {
        /// <summary>
        /// Gets all pipeline diagnostics (including descendant subpipelines).
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <returns>All pipeline diagnostics.</returns>
        public static IEnumerable<PipelineDiagnostics> GetAllPipelineDiagnostics(this PipelineDiagnostics pipeline)
        {
            yield return pipeline;
            foreach (var child in pipeline.SubpipelineDiagnostics)
            {
                foreach (var descendant in child.GetAllPipelineDiagnostics())
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Gets all pipeline element diagnostics within a collection of pipeline diagnostics.
        /// </summary>
        /// <param name="pipelines">Collection of pipeline diagnostics.</param>
        /// <returns>All pipeline element diagnostics within.</returns>
        public static IEnumerable<PipelineDiagnostics.PipelineElementDiagnostics> GetAllPipelineElements(this IEnumerable<PipelineDiagnostics> pipelines)
        {
            foreach (var p in pipelines)
            {
                foreach (var pe in p.PipelineElements)
                {
                    yield return pe;
                }
            }
        }

        /// <summary>
        /// Gets all pipeline element diagnostics within a pipeline diagnostics (and all descendant subpipelines).
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <returns>Collection of all pipeline element diagnostics within.</returns>
        public static IEnumerable<PipelineDiagnostics.PipelineElementDiagnostics> GetAllPipelineElementDiagnostics(this PipelineDiagnostics pipeline)
        {
            return pipeline.GetAllPipelineDiagnostics().GetAllPipelineElements();
        }

        /// <summary>
        /// Gets all emitter diagnostics within a collection of pipeline element diagnostics.
        /// </summary>
        /// <param name="pipelineElements">Collection of pipeline element diagnostics.</param>
        /// <returns>Collection of all emitter diagnostics within.</returns>
        public static IEnumerable<PipelineDiagnostics.EmitterDiagnostics> GetAllEmitterDiagnostics(this IEnumerable<PipelineDiagnostics.PipelineElementDiagnostics> pipelineElements)
        {
            foreach (var pe in pipelineElements)
            {
                foreach (var e in pe.Emitters)
                {
                    yield return e;
                }
            }
        }

        /// <summary>
        /// Gets all emitter diagnostics within a collection of pipeline diagnostics.
        /// </summary>
        /// <param name="pipelines">Collection of pipeline diagnostics.</param>
        /// <returns>Collection of all emitter diagnostics within.</returns>
        public static IEnumerable<PipelineDiagnostics.EmitterDiagnostics> GetAllEmitterDiagnostics(this IEnumerable<PipelineDiagnostics> pipelines)
        {
            return pipelines.GetAllPipelineElements().GetAllEmitterDiagnostics();
        }

        /// <summary>
        /// Gets all emitter diagnostics within a pipeline diagnostics (and all descendant subpipelines).
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <returns>Collection of all emitter diagnostics within.</returns>
        public static IEnumerable<PipelineDiagnostics.EmitterDiagnostics> GetAllEmitterDiagnostics(this PipelineDiagnostics pipeline)
        {
            return pipeline.GetAllPipelineDiagnostics().GetAllEmitterDiagnostics();
        }

        /// <summary>
        /// Collection of all receiver diagnostics within a collection of pipeline element diagnostics.
        /// </summary>
        /// <param name="pipelineElements">Collection of pipeline element diagnostics.</param>
        /// <returns>Collection of all receiver diagnostics within.</returns>
        public static IEnumerable<PipelineDiagnostics.ReceiverDiagnostics> GetAllReceiverDiagnostics(this IEnumerable<PipelineDiagnostics.PipelineElementDiagnostics> pipelineElements)
        {
            foreach (var pe in pipelineElements)
            {
                foreach (var r in pe.Receivers)
                {
                    yield return r;
                }
            }
        }

        /// <summary>
        /// Collection of all receiver diagnostics within a collection of pipeline diagnostics.
        /// </summary>
        /// <param name="pipelines">Collection of pipeline diagnostics.</param>
        /// <returns>Collection of all receiver diagnostics within.</returns>
        public static IEnumerable<PipelineDiagnostics.ReceiverDiagnostics> GetAllReceiverDiagnostics(this IEnumerable<PipelineDiagnostics> pipelines)
        {
            return pipelines.GetAllPipelineElements().GetAllReceiverDiagnostics();
        }

        /// <summary>
        /// Gets all receiver diagnostics within a pipeline diagnostics (and all descendant subpipelines).
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <returns>Collection of all receiver diagnostics within.</returns>
        public static IEnumerable<PipelineDiagnostics.ReceiverDiagnostics> GetAllReceiverDiagnostics(this PipelineDiagnostics pipeline)
        {
            return pipeline.GetAllPipelineDiagnostics().GetAllReceiverDiagnostics();
        }

        /// <summary>
        /// Gets count of pipelines.
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <param name="predicate">Predicate expression filtering pipeline diagnostics.</param>
        /// <returns>Pipeline count.</returns>
        public static int GetPipelineCount(this PipelineDiagnostics pipeline, Func<PipelineDiagnostics, bool> predicate = null)
        {
            return pipeline.GetAllPipelineDiagnostics().Where(p => predicate == null ? true : predicate(p)).Count();
        }

        /// <summary>
        /// Gets count of pipeline elements.
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <param name="predicate">Predicate expression filtering pipeline element diagnostics.</param>
        /// <returns>Pipeline element count.</returns>
        public static int GetPipelineElementCount(this PipelineDiagnostics pipeline, Func<PipelineDiagnostics.PipelineElementDiagnostics, bool> predicate = null)
        {
            return pipeline.GetAllPipelineElementDiagnostics().Where(e => predicate == null ? true : predicate(e)).Count();
        }

        /// <summary>
        /// Gets emitter count within pipeline and descendant.
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <param name="predicate">Predicate expression filtering emitter diagnostics.</param>
        /// <returns>Emitter count.</returns>
        public static int GetEmitterCount(this PipelineDiagnostics pipeline, Func<PipelineDiagnostics.EmitterDiagnostics, bool> predicate = null)
        {
            return pipeline.GetAllEmitterDiagnostics().Where(e => predicate == null ? true : predicate(e)).Count();
        }

        /// <summary>
        /// Gets receiver count within pipeline and descendant.
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <param name="predicate">Predicate expression filtering receiver diagnostics.</param>
        /// <returns>Receiver count.</returns>
        public static int GetReceiverCount(this PipelineDiagnostics pipeline, Func<PipelineDiagnostics.ReceiverDiagnostics, bool> predicate = null)
        {
            return pipeline.GetAllReceiverDiagnostics().Where(r => predicate == null ? true : predicate(r)).Count();
        }

        /// <summary>
        /// Gets throttled receiver count within pipeline and descendant.
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <param name="predicate">Predicate expression filtering receiver diagnostics.</param>
        /// <returns>Average queued message count.</returns>
        public static double GetAverageQueuedMessageCount(this PipelineDiagnostics pipeline, Func<PipelineDiagnostics.ReceiverDiagnostics, bool> predicate = null)
        {
            return pipeline.GetAllReceiverDiagnostics().Where(r => predicate == null ? true : predicate(r)).Select(r => r.AvgDeliveryQueueSize).Sum();
        }

        /// <summary>
        /// Gets dropped message count across receivers within pipeline and descendant.
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <param name="predicate">Predicate expression filtering receiver diagnostics.</param>
        /// <returns>Dropped message count.</returns>
        public static int GetDroppedMessageCount(this PipelineDiagnostics pipeline, Func<PipelineDiagnostics.ReceiverDiagnostics, bool> predicate = null)
        {
            return pipeline.GetAllReceiverDiagnostics().Where(r => predicate == null ? true : predicate(r)).Select(r => r.TotalMessageDroppedCount).Sum();
        }

        /// <summary>
        /// Gets dropped message count in last averaging time span across receivers within pipeline and descendant.
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <param name="predicate">Predicate expression filtering receiver diagnostics.</param>
        /// <returns>Dropped message count.</returns>
        public static int GetDroppedMessageAveragePerTimeSpan(this PipelineDiagnostics pipeline, Func<PipelineDiagnostics.ReceiverDiagnostics, bool> predicate = null)
        {
            return pipeline.GetAllReceiverDiagnostics().Where(r => predicate == null ? true : predicate(r)).Select(r => r.WindowMessageDroppedCount).Sum();
        }

        /// <summary>
        /// Gets processed message count across receivers within pipeline and descendant.
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <param name="predicate">Predicate expression filtering receiver diagnostics.</param>
        /// <returns>Processed message count.</returns>
        public static int GetProcessedMessageCount(this PipelineDiagnostics pipeline, Func<PipelineDiagnostics.ReceiverDiagnostics, bool> predicate = null)
        {
            return pipeline.GetAllReceiverDiagnostics().Where(r => predicate == null ? true : predicate(r)).Select(r => r.TotalMessageProcessedCount).Sum();
        }

        /// <summary>
        /// Gets processed message count in last averaging time span across receivers within pipeline and descendant.
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <param name="predicate">Predicate expression filtering receiver diagnostics.</param>
        /// <returns>Processed message count.</returns>
        public static int GetProcessedMessageAveragePerTimeSpan(this PipelineDiagnostics pipeline, Func<PipelineDiagnostics.ReceiverDiagnostics, bool> predicate = null)
        {
            return pipeline.GetAllReceiverDiagnostics().Where(r => predicate == null ? true : predicate(r)).Select(r => r.WindowMessageProcessedCount).Sum();
        }

        /// <summary>
        /// Gets throttled receiver count across receivers within pipeline and descendant.
        /// </summary>
        /// <param name="pipeline">Root pipeline diagnostics.</param>
        /// <param name="predicate">Predicate expression filtering receiver diagnostics.</param>
        /// <returns>Throttled receiver count.</returns>
        public static int GetThrottledReceiverCount(this PipelineDiagnostics pipeline, Func<PipelineDiagnostics.ReceiverDiagnostics, bool> predicate = null)
        {
            return pipeline.GetAllReceiverDiagnostics().Where(r => r.ReceiverIsThrottled && (predicate == null ? true : predicate(r))).Count();
        }

        /// <summary>
        /// Compute average time from a sequence of time spans (e.g. ProcessingTimeHistory).
        /// </summary>
        /// <param name="times">Sequence of time spans.</param>
        /// <returns>Average time (zero if empty).</returns>
        public static TimeSpan AverageTime(this IEnumerable<(TimeSpan, DateTime)> times)
        {
            return TimeSpan.FromTicks(times.Count() > 0 ? (long)times.Select(t => t.Item1.Ticks).Average() : 0L);
        }

        /// <summary>
        /// Compute average size from a sequence of sizes (e.g. QueueSize).
        /// </summary>
        /// <param name="sizes">Sequence of sizes.</param>
        /// <returns>Average size (zero if empty).</returns>
        public static double AverageSize(this IEnumerable<(int, DateTime)> sizes)
        {
            return sizes.Count() > 0 ? sizes.Average(s => s.Item1) : 0;
        }
    }
}