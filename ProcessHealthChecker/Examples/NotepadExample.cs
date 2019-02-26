using System;
using System.Diagnostics;
using System.Threading;

namespace ProcessHealthChecker.Examples
{
    public class NotepadExample
    {
        public void RunHealthCheckerExample()
        {
            var process = Process.Start("notepad.exe");
            var processHealthChecker = ProcessHealthCheckerFactory.Instantiate();
            processHealthChecker.ProcessNotFound += (int processId, string processName) =>
            {
                Console.WriteLine("This will be fired when notepad.exe will be killed");
            };
            processHealthChecker.AddToProcessHealthControlling(process);
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000 * 10); // 10 secs
            // Check periodically if process was killed or not
            processHealthChecker.StartControlProcessHealth(cancellationTokenSource.Token, delayBetweenChecking: 100);
            processHealthChecker.RemoveOnRootProcessExit(process);
            
            // ProcessNotFound will be fired
            // If Kill will fail, then process will be killed anyway
            process.Kill();
        }
    }
}