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
using System.IO.Abstractions;
using System.Text;
using Microsoft.VisualStudio;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;

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

    internal class VsDevCmdEnvironmentVarsProvider : IVsDevCmdEnvironmentProvider
    {
        /// <summary>
        /// Relative path from the VS installation root to the batch file
        /// </summary>
        /// <remarks>This path works for VS2015+</remarks>
        private const string RelativePathToBatchFile = "Common7\\Tools\\VsDevCmd.bat";

        private const int SCRIPT_TIMEOUT_MS = 4000;

        private readonly IVsInfoService vsInfoService;
        private readonly ILogger logger;

        // Interfaces for testing
        private readonly IProcessFactory processFactory;
        private readonly IFileSystem fileSystem;

        public VsDevCmdEnvironmentVarsProvider(IVsInfoService vsInfoService, ILogger logger)
            : this(vsInfoService, logger, new ProcessFactory(), new FileSystem())
        {
        }

        internal /* for testing */ VsDevCmdEnvironmentVarsProvider(IVsInfoService vsInfoService, ILogger logger,
            IProcessFactory processFactory, IFileSystem fileSystem)
        {
            this.vsInfoService = vsInfoService;
            this.logger = logger;
            this.processFactory = processFactory;
            this.fileSystem = fileSystem;
        }

        public IReadOnlyDictionary<string, string> Get(string scriptParams)
        {
            try
            {
                string filePath = Path.Combine(vsInfoService.InstallRootDir, RelativePathToBatchFile);
                LogDebug($"VsDevCmd location: {filePath}");

                if (!fileSystem.File.Exists(filePath))
                {
                    logger.WriteLine(Resources.VsDevCmd_FileNotFound, filePath);
                    return null;
                }

                var capturedOutput = ExecuteVsDevCmd(filePath, scriptParams);
                var settings = ParseOutput(capturedOutput);

                if (settings == null || settings.Count == 0)
                {
                    logger.WriteLine(Resources.VsDevCmd_NoSettingsFound);
                    return null;
                }

                LogDebug($"Settings fetched successfully. Count: {settings.Count}");
                return settings;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.VsDevCmd_ErrorFetchingSettings, ex);
            }

            return null;
        }

        internal /* for testing */ string UniqueId { get; private set;}

        private IList<string> ExecuteVsDevCmd(string batchFilePath, string scriptParams)
        {
            UniqueId = Guid.NewGuid().ToString();
            var beginToken = "SONARLINT_BEGIN_CAPTURE " + UniqueId;
            var endToken = "SONARLINT_END_CAPTURE " + UniqueId;

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = Environment.GetEnvironmentVariable("COMSPEC"),
                Arguments = "/U " +                             // Unicode input / output
                "/K " +                                         // Execute the command and remain active. Need to do this or we can't fetch the env vars 
                "set VSCMD_SKIP_SENDTELEMETRY=1 && " +          // Disable VS telemetry
                "\"" + batchFilePath + "\" " + scriptParams +   // Call VsDevCmd with any additional parameters
                " && echo " + beginToken +                      // Output a marker so we known when to start processing the env vars
                " && set" +                                     // Write out the env vars
                " && echo " + endToken + " \"",                 // Output a marker to say we are done

                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                RedirectStandardError = false,
                StandardOutputEncoding = Encoding.Unicode,
                StandardErrorEncoding = Encoding.Unicode
            };

            var capturedOutput = new List<string>();

            var capturingOutput = false;

            using (var process = processFactory.Start(startInfo))
            {
                process.HandleOutputDataReceived = data =>
                {
                    if (data == null) { return; }

                    if (!capturingOutput && data.StartsWith(beginToken))
                    {
                        capturingOutput = true;
                        return;
                    }
                    else if (capturingOutput && data.StartsWith(endToken))
                    {
                        capturingOutput = false;
                        SafeKillProcess(process);
                    }

                    if (capturingOutput)
                    {
                        capturedOutput.Add(data);
                    }
                };

                LogDebug($"Started process. Id: {process.Id}");
                process.BeginOutputReadLine();

                // Timeout in case something goes wrong.
                process.WaitForExit(SCRIPT_TIMEOUT_MS);
                if (process.HasExited)
                {
                    LogDebug("Process completed successfully");
                }
                else
                {
                    logger.WriteLine(Resources.VsDevCmd_TimedOut);
                    capturedOutput = null;
                    SafeKillProcess(process);
                }
            }

            return capturedOutput;
        }

        private static Dictionary<string, string> ParseOutput(IList<string> capturedOutput)
        {
            if (capturedOutput == null)
            {
                return null;
            }

            var settings = new Dictionary<string, string>();

            foreach (var item in capturedOutput)
            {
                var equalsIndex = item.IndexOf("=", StringComparison.OrdinalIgnoreCase);
                if (equalsIndex > -1)
                {
                    var key = item.Substring(0, equalsIndex);
                    var newValue = item.Substring(equalsIndex + 1);

                    settings.Add(key, newValue);
                }
            }

            return settings;
        }

        private void SafeKillProcess(IProcess process)
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
                LogDebug($"Error terminating VsDevCmd.bat: {ex.Message}");
            }
        }

        private void LogDebug(string message)
        {
            logger.LogDebug("[CMake:VsDevCmd] " + message);
        }
    }
}
