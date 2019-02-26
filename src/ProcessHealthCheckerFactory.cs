using System.Diagnostics;

namespace ProcessHealthChecker
{
    public class ProcessHealthCheckerFactory
    {
        public static IProcessHealthChecker Instantiate()
        {
            return new ProcessHealthChecker();
        }
    }
}