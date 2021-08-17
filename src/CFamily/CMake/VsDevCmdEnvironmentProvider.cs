/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using SonarLint.VisualStudio.CFamily.SystemAbstractions;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    internal interface IVsDevCmdEnvironmentProvider
    {
        /// <summary>
        /// Returns the environment settings set by VsDevCmd.bat
        /// </summary>
        /// <param name="scriptParams">Any additional parameters to pass to the batch file. Can be null.</param>
        IReadOnlyDictionary<string, string> Get(string scriptParams);
    }

    internal class VsDevCmdEnvironmentProvider : IVsDevCmdEnvironmentProvider
    {
        /// <summary>
        /// Relative path from the VS installation root to the batch file
        /// </summary>
        /// <remarks>This path works for VS2015+</remarks>
        private const string RelativePathToBatchFile = "Common7\\Tools\\VsDevCmd.bat";
        private const int SCRIPT_TIMEOUT_MS = 3000;

        private readonly IVsInfoService vsInfoService;
        private readonly ILogger logger;
        private readonly IProcessFactory processFactory;

        public VsDevCmdEnvironmentProvider(IVsInfoService vsInfoService, ILogger logger)
            : this(vsInfoService, logger, new ProcessFactory())
        {
            this.vsInfoService = vsInfoService;
            this.logger = logger;
        }

        internal /* for testing */ VsDevCmdEnvironmentProvider(IVsInfoService vsInfoService, ILogger logger,
            IProcessFactory processFactory)
        {
            this.vsInfoService = vsInfoService;
            this.logger = logger;
            this.processFactory = processFactory;
        }

        public IReadOnlyDictionary<string, string> Get(string scriptParams)
        {
            var capturedOutput = ExecuteVsDevCmd(scriptParams);
            return ParseAndFilterOutput(capturedOutput);
        }

        private IList<string> ExecuteVsDevCmd(string scriptParams)
        {
            string path = Path.Combine(vsInfoService.InstallRootDir, RelativePathToBatchFile);

            var uniqueId = Guid.NewGuid().ToString();
            var beginToken = "SONARLINT_BEGIN_CAPTURE " + uniqueId;
            var endToken = "SONARLINT_END_CAPTURE " + uniqueId;

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = Environment.GetEnvironmentVariable("COMSPEC"),
                Arguments = "/U " +                         // Unicode input / output
                "/K " +                                     // Execute the command and remain active. Need to do this or we can't fetch the env vars 
                "set VSCMD_SKIP_SENDTELEMETRY=1 && " +    // Disable VS telemetry
                "\"" + path + "\" " + scriptParams +        // Call VsDevCmd with any additional parameters
                " && echo " + beginToken +                  // Output a marker so we known when to start processing the env vars
                " && set" +                                 // Write out the env vars
                " && echo " + endToken + " \"",             // Output a marker to say we are done

                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.Unicode,
                StandardErrorEncoding = Encoding.Unicode
            };

            var output = new List<string>();
            var allOutput = new StringBuilder();

            var capturingOutput = false;

            using (var process = processFactory.Start(startInfo))
            {
                process.HandleOutputDataReceived = data =>
                {
                    allOutput.AppendLine(data);
                    if (data == null) { return; }

                    if (!capturingOutput && data.StartsWith(beginToken))
                    {
                        capturingOutput = true;
                    }
                    else if (capturingOutput && data.StartsWith(endToken))
                    {
                        SafeKillProcess(process);
                    }

                    if (capturingOutput)
                    {
                        output.Add(data);
                    }
                };

                process.BeginOutputReadLine();

                // Timeout in case something goes wrong.
                process.WaitForExit(SCRIPT_TIMEOUT_MS);
                SafeKillProcess(process);
            }

            return output;
        }

        private static Dictionary<string, string> ParseAndFilterOutput(IList<string> capturedOutput)
        {
            var settings = new Dictionary<string, string>();

            foreach (var item in capturedOutput)
            {
                var equalsIndex = item.IndexOf("=", StringComparison.OrdinalIgnoreCase);
                if (equalsIndex > -1)
                {
                    var key = item.Substring(0, equalsIndex);
                    var newValue = item.Substring(equalsIndex + 1);

                    if (Environment.GetEnvironmentVariable(key) != newValue)
                    {
                        settings.Add(key, newValue);
                    }
                }
            }

            return settings;
        }

        private static void SafeKillProcess(IProcess process)
        {
            // Ignore any errors when terminating the process
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
