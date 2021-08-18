using System;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
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
            Process = process;

            // Cache the properties we need now as we won't be able to access them
            // later if the process has been disposed at the point we want to clean up.
            id = process.Id;
            mainModuleFilName = process.MainModule.FileName;
            processName = process.ProcessName;
        }

        public void Dispose()
        {
            try
            {
                var process = Process.GetProcessById(id);

                // Make sure we don't accidentally kill a newly-created process with the same id
                if (process.MainModule.FileName == mainModuleFilName &&
                    process.ProcessName == processName)
                {
                    process.Kill();
                }
            }
            catch (Exception)
            {
                // Do nothing
            }
        }
    }
}
