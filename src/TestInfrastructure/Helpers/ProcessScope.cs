using System.Diagnostics;

namespace SonarLint.VisualStudio.TestInfrastructure.Helpers
{
    /// <summary>
    /// Ensure the process is terminated when it goes out of scope.
    /// Usage:
    ///   using var processScope = new ProcessScope(process);
    /// </summary>
    /// <remarks>Disposing a process does not terminate it so we can't rely on
    /// "using var process = ..." to clean up properly.
    /// Instead, we'll try to fetch the process by id then "Kill" it.
    /// </remarks>
    public sealed class ProcessScope : IDisposable
    {
        private readonly int id;
        private readonly string mainModuleFilName;
        private readonly string processName;

        public Process Process { get; }

        public ProcessScope(Process process)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));

            // Cache the properties we need now as we won't be able to access them
            // later if the process has been disposed at the point we want to clean up.
            id = process.Id;

            (mainModuleFilName, processName) = TryGetProcessDetails(process);
        }

        public void Dispose()
        {
            try
            {
                var runningProcess = Process.GetProcessById(id);
                Log("Process is running.  Id: " + id);

                // Make sure we don't accidentally kill a newly-created process with the same id
                (string runningModuleName, string runningProcessName) = TryGetProcessDetails(runningProcess);

                if (runningModuleName == mainModuleFilName &&
                    runningProcessName == processName)
                {
                    Log("Terminating process. Id: " + id);
                    runningProcess.Kill();
                }
                else
                {
                    Log("Process details do not match. Process will not be terminated.");
                }
            }
            catch(ArgumentException ex)
            {
                Log($"Process is probably not running. Id: {id}. Error message: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private static (string moduleFileName, string processName) TryGetProcessDetails(Process process)
        {
            Log("Fetching details for process: " + process.Id);

            string  moduleFileName = SafeGetProperty(() => process.MainModule?.FileName);
            string processName = SafeGetProperty(() => process.ProcessName);

            Log("  Main module file name: " + moduleFileName);
            Log("  Process name: " + processName);

            return (moduleFileName, processName);
        }

        private static string SafeGetProperty(Func<string> op)
        {
            try
            {
                return op();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return "{unknown}";
        }

        private static void Log(string message) =>
                Console.WriteLine("[ProcessScope] " + message);

        private static void LogError(Exception ex) =>
                Log("Error: " + ex.ToString());
    }
}
