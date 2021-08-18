using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    /// <summary>
    /// Tries to ensure the process is terminated when a test finishes.
    /// Usage:
    ///   using var cleanHelper = new ProcessCleanupHelper(process);
    /// </summary>
    /// <remarks>Disposing a process does not terminate it, so we can't rely on
    /// "using var process = ..." to clean up properly.
    /// Instead, we'll try to fetch the process by id then "Kill" it.
    /// </remarks>
    public sealed class ProcessCleanupHelper : IDisposable
    {
        private readonly int id;
        private readonly string mainModuleFilName;
        private readonly string processName;

        public Process RealProcess { get; }

        public ProcessCleanupHelper(Process process)
        {
            RealProcess = process;

            id = process.Id;
            mainModuleFilName = process.MainModule.FileName;
            processName = process.ProcessName;
        }

        public void Dispose()
        {
            try
            {
                var process = Process.GetProcessById(id);

                // Make sure we don't accidentally kill a newly-created process
                // with the same id...
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
