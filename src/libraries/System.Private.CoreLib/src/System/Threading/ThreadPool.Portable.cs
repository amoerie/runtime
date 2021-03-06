// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    //
    // Portable implementation of ThreadPool
    //

    public sealed partial class RegisteredWaitHandle : MarshalByRefObject
    {
        /// <summary>
        /// Unregisters this wait handle registration from the wait threads.
        /// </summary>
        /// <param name="waitObject">The event to signal when the handle is unregistered.</param>
        /// <returns>If the handle was successfully marked to be removed and the provided wait handle was set as the user provided event.</returns>
        /// <remarks>
        /// This method will only return true on the first call.
        /// Passing in a wait handle with a value of -1 will result in a blocking wait, where Unregister will not return until the full unregistration is completed.
        /// </remarks>
        public bool Unregister(WaitHandle waitObject) => UnregisterPortable(waitObject);
    }

    internal sealed partial class CompleteWaitThreadPoolWorkItem : IThreadPoolWorkItem
    {
        void IThreadPoolWorkItem.Execute() => PortableThreadPool.CompleteWait(_registeredWaitHandle, _timedOut);
    }

    public static partial class ThreadPool
    {
        // Time-sensitive work items are those that may need to run ahead of normal work items at least periodically. For a
        // runtime that does not support time-sensitive work items on the managed side, the thread pool yields the thread to the
        // runtime periodically (by exiting the dispatch loop) so that the runtime may use that thread for processing
        // any time-sensitive work. For a runtime that supports time-sensitive work items on the managed side, the thread pool
        // does not yield the thread and instead processes time-sensitive work items queued by specific APIs periodically.
        internal const bool SupportsTimeSensitiveWorkItems = true;

        internal static readonly bool EnableWorkerTracking =
            AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.EnableWorkerTracking", false);

        internal static bool CanSetMinIOCompletionThreads(int ioCompletionThreads) => true;
        internal static void SetMinIOCompletionThreads(int ioCompletionThreads) { }

        internal static bool CanSetMaxIOCompletionThreads(int ioCompletionThreads) => true;
        internal static void SetMaxIOCompletionThreads(int ioCompletionThreads) { }

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads) =>
            PortableThreadPool.ThreadPoolInstance.SetMaxThreads(workerThreads, completionPortThreads);

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            // Note that worker threads and completion port threads share the same thread pool.
            // The total number of threads cannot exceed MaxPossibleThreadCount.
            workerThreads = PortableThreadPool.ThreadPoolInstance.GetMaxThreads();
            completionPortThreads = 1;
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads) =>
            PortableThreadPool.ThreadPoolInstance.SetMinThreads(workerThreads, completionPortThreads);

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            workerThreads = PortableThreadPool.ThreadPoolInstance.GetMinThreads();
            completionPortThreads = 1;
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            workerThreads = PortableThreadPool.ThreadPoolInstance.GetAvailableThreads();
            completionPortThreads = 0;
        }

        /// <summary>
        /// Gets the number of thread pool threads that currently exist.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of threads, the count includes all types.
        /// </remarks>
        public static int ThreadCount => PortableThreadPool.ThreadPoolInstance.ThreadCount;

        /// <summary>
        /// Gets the number of work items that have been processed by the thread pool so far.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of work items, the count includes all types.
        /// </remarks>
        public static long CompletedWorkItemCount => PortableThreadPool.ThreadPoolInstance.CompletedWorkItemCount;

        /// <summary>
        /// This method is called to request a new thread pool worker to handle pending work.
        /// </summary>
        internal static void RequestWorkerThread() => PortableThreadPool.ThreadPoolInstance.RequestWorker();

        /// <summary>
        /// Called from the gate thread periodically to perform runtime-specific gate activities
        /// </summary>
        /// <param name="cpuUtilization">CPU utilization as a percentage since the last call</param>
        /// <returns>True if the runtime still needs to perform gate activities, false otherwise</returns>
        internal static bool PerformRuntimeSpecificGateActivities(int cpuUtilization) => false;

        internal static void NotifyWorkItemProgress() => PortableThreadPool.ThreadPoolInstance.NotifyWorkItemProgress();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(object? threadLocalCompletionCountObject, int currentTimeMs) =>
            PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete(threadLocalCompletionCountObject, currentTimeMs);

        internal static object GetOrCreateThreadLocalCompletionCountObject() =>
            PortableThreadPool.ThreadPoolInstance.GetOrCreateThreadLocalCompletionCountObject();

        private static void RegisterWaitForSingleObjectCore(WaitHandle? waitObject, RegisteredWaitHandle registeredWaitHandle) =>
            PortableThreadPool.ThreadPoolInstance.RegisterWaitHandle(registeredWaitHandle);

        internal static void UnsafeQueueWaitCompletion(CompleteWaitThreadPoolWorkItem completeWaitWorkItem) =>
            UnsafeQueueUserWorkItemInternal(completeWaitWorkItem, preferLocal: false);
    }
}
