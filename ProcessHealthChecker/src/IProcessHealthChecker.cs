using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessHealthChecker
{
    /// <summary>
    /// Implement this interface for processes check and interactions
    /// </summary>
    public interface IProcessHealthChecker : IDisposable
    {
        /// <summary>
        /// Start control health of processes. Will raise ProcessNotFound if any of the processes exited
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="delayBetweenChecking">Delay between checking processes health</param>
        /// <returns>Task for awaiting</returns>
        Task StartControlProcessHealth(CancellationToken cancellationToken = default(CancellationToken), int delayBetweenChecking = 100);
        
        /// <summary>
        /// Adds process to tracking, so this process will be killed after root process exit
        /// </summary>
        /// <param name="process"></param>
        void RemoveOnRootProcessExit(Process process);
        
        /// <summary>
        /// Add process to process health controlling task
        /// </summary>
        /// <param name="process"></param>
        void AddToProcessHealthControlling(Process process);

        /// <summary>
        /// Removes process from health controlling task
        /// </summary>
        /// <param name="process"></param>
        void RemoveProcessFromHealthControlling(Process process);

        /// <summary>
        /// Removes process from health controlling task by predicate
        /// </summary>
        /// <param name="predicate"></param>
        void RemoveProcessFromHealthControlling(Func<Process, bool> predicate);
        
        /// <summary>
        /// Will be fired if any of the processes specified in StartControlProcessHealth function exit
        /// </summary>
        event Action<int, string> ProcessNotFound;
    }
}