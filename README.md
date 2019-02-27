# ProcessHealthChecker
Use this library to check health of child processes, track them, fire event if any of the processes exited and **always kill them if root process exits**

**How does it work**

**Usage #1 - track and kill all child processes in your program process exit**

It creates a new process tracker using windows' `kernel32.dll` function `CreateJobProcess` and assigns all child process to it by `RemoveOnRootProcessExit` function call

**Example with selenium**


```
var chromeService = ChromeDriverService.CreateDefaultService();
var chromeDriver = new ChromeDriver(chromeService);
Int32 childProcessId = chromeService.ProcessId;
Process childProcess = Process.GetProcessById(childProcessId);
IProcessHealthChecker processHealthChecker = ProcessHealthCheckerFactory.Instantiate();
processHealthChecker.RemoveOnRootProcessExit(childProcess);
```

Thats it. If your application process will be killed or will exit chromeDriver process will exit automatically.

**WARNING**
For Windows 7 and Windows Vista support you need to add the following lines to you app.manifest

```
<compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
<application>
  <!--The ID below indicates application support for Windows Vista -->
  <supportedOS Id="{e2011457-1546-43c5-a5fe-008deee3d3f0}"/>
  <!--The ID below indicates application support for Windows 7 -->
  <supportedOS Id="{35138b9a-5d96-4fbd-8e2d-a2440225f93a}"/>
</application>
</compatibility>
```


**Usage #2 - periodically checks if processes are alive and healthy**

**Example with selenium**

```
var chromeService = ChromeDriverService.CreateDefaultService();
var chromeDriver = new ChromeDriver(chromeService);
Int32 childProcessId = chromeService.ProcessId;
Process childProcess = Process.GetProcessById(childProcessId);
IProcessHealthChecker processHealthChecker = ProcessHealthCheckerFactory.Instantiate();
processHealthChecker.AddToProcessHealthControlling(process);
processHealthChecker.ProcessNotFound += (int processId, string processName) => 
{
  Console.WriteLine($"Exited process id {processId}, process name {processName}");
};
var cancellationTokenSource = new CancellationTokenSource();
processHealthChecker.StartControlProcessHealth(cancellationToken: cancellationTokenSource.Token, delayBetweenChecking=100);
```
