using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessHealthChecker
{
    internal class ProcessHealthChecker : IProcessHealthChecker
    {
        private volatile IList<Process> _processes = new List<Process>();

        internal ProcessHealthChecker()
        {
            
        }

        static ProcessHealthChecker()
        {
            Int32 extendedLimitInformation = 9;
            if (Environment.OSVersion.Version < new Version(6, 2))
                return;

            string jobName = "ChildProcessTracker" + Process.GetCurrentProcess().Id;
            s_jobHandle = CreateJobObject(IntPtr.Zero, jobName);

            var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION();
            info.LimitFlags = JOBOBJECTLIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            extendedInfo.BasicLimitInformation = info;

            int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

                if (!SetInformationJobObject(s_jobHandle, extendedLimitInformation,
                    extendedInfoPtr, (uint) length))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(extendedInfoPtr);
            }
        }

        public void Dispose()
        {
            foreach (var process in _processes)
            {
                try
                {
                    process?.Kill();
                }
                catch (InvalidOperationException)
                {
                    // Process has already exited
                }
                catch (Win32Exception)
                {
                    // Process is disposing currently
                }
            }
            _processes = new List<Process>();
        }

        public Task StartControlProcessHealth(CancellationToken cancellationToken = default(CancellationToken), int delayBetweenChecking = 100)
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(delayBetweenChecking);
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (var process in _processes)
                    {
                        var processExited = true;
                        string processName = null;
                        try
                        {
                            processExited = process.HasExited;
                            processName = process.ProcessName;
                        }
                        catch (Win32Exception)
                        {
                            // The exit code for the process could not be retrieved.
                        }
                        catch (InvalidOperationException)
                        {
                            // There is no process associated with the object.
                        }
                        if (processExited)
                        {
                            ProcessNotFound?.Invoke(process.Id, processName);
                        }
                    }
                }
            }, cancellationToken);
        }

        public void RemoveOnRootProcessExit(Process process)
        {
            AddProcess(process);
        }

        public void AddToProcessHealthControlling(Process process)
        {
            _processes.Add(process);
        }

        public void RemoveProcessFromHealthControlling(Process process)
        {
            _processes.Remove(process);
        }

        public void RemoveProcessFromHealthControlling(Func<Process, bool> predicate)
        {
            var process = _processes.FirstOrDefault(predicate);
            if (process != null)
                _processes.Remove(process);
        }

        public event Action<int, string> ProcessNotFound;

        private static void AddProcess(Process process)
        {
            if (s_jobHandle != IntPtr.Zero)
            {
                bool success = AssignProcessToJobObject(s_jobHandle, process.Handle);
                if (!success && !process.HasExited)
                    throw new Win32Exception();
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string name);

        [DllImport("kernel32.dll")]
        private static extern bool SetInformationJobObject(IntPtr job, int infoType,
            IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        private static readonly IntPtr s_jobHandle;

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public Int64 PerProcessUserTimeLimit;
            public Int64 PerJobUserTimeLimit;
            public JOBOBJECTLIMIT LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public UInt32 ActiveProcessLimit;
            public Int64 Affinity;
            public UInt32 PriorityClass;
            public UInt32 SchedulingClass;
        }

        [Flags]
        private enum JOBOBJECTLIMIT : uint
        {
            JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public UInt64 ReadOperationCount;
            public UInt64 WriteOperationCount;
            public UInt64 OtherOperationCount;
            public UInt64 ReadTransferCount;
            public UInt64 WriteTransferCount;
            public UInt64 OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
    }
}