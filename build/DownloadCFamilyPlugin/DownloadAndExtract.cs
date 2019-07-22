/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpCompress.Compressors.Xz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;

namespace DownloadCFamilyPlugin
{
    /// <summary>
    /// Downloads the CFamily jar and extracts the files that need to be
    /// embedded in the SLVS vsix
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// * we are downloading the CFamily plugin jar e.g. https://binaries.sonarsource.com/CommercialDistribution/sonar-cfamily-plugin/sonar-cfamily-plugin-6.3.0.11371.jar
    /// * the jar contains a .tarxz archive containing the Windows version of SubProcess.exe
    /// * the jar contains various .json files, some with fixed names, some that we'll match using wildcards
    /// 
    /// We want the task to fail if it cannot locate all of the expected files (otherwise we'll build an invalid VSIX).
    /// 
    /// Downloading and extracting the files can be slow so we want to skip those steps if possible.
    /// </remarks>
    public class DownloadAndExtract: Task
    {
        // The txz archive containing the subprocess.exe
        private const string WindowsTxzFilePattern = "clang-*-win.txz";
        private const string CLangExeFilePattern = "SubProcess.exe";

        private readonly string[] KnownJsonFiles = new string[]
        {
                "Sonar_way_profile.json",
                "RulesList.json"
        };

        private readonly string[] AdditionalFilesToReturnPattern = new string[]
        {
            "S*_params.json"
        };

        #region MSBuild input / output properties (set/read in the project file)

        // Example: https://binaries.sonarsource.com/CommercialDistribution/sonar-cfamily-plugin/sonar-cfamily-plugin-6.3.0.11371.jar
        [Required]
        public string DownloadUrl { get; set; }

        [Output]
        public string[] FilesToEmbed { get; private set; }

        #endregion

        public override bool Execute()
        {
            LogMessage($"Download url: {DownloadUrl}");

            var pluginFileName = ExtractPluginFileName(DownloadUrl);

            // Ensure working directories exists
            var localWorkingFolder = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "SLVS_CFamily_Build");
            var perVersionPluginFolder = Path.Combine(localWorkingFolder, Path.GetFileNameWithoutExtension(pluginFileName));
            EnsureWorkingDirectoryExist(perVersionPluginFolder);

            // Download and unzip the jar
            var jarFilePath = Path.Combine(perVersionPluginFolder, pluginFileName);
            DownloadJarFile(DownloadUrl, jarFilePath);
            UnzipJar(jarFilePath, perVersionPluginFolder);

            // Locate the required files from the jar
            var fileList = FindJsonFiles(perVersionPluginFolder);

            // Uncompress and extract the windows tar archive to get the subprocess exe
            var tarFilePath = FindSingleFile(perVersionPluginFolder, WindowsTxzFilePattern);
            var tarSubFolder = Path.Combine(perVersionPluginFolder, "tar_xz");
            UncompressAndUnzipTar(tarFilePath, tarSubFolder);
            var subProcessExeFile = FindSingleFile(tarSubFolder, CLangExeFilePattern);
            fileList.Add(subProcessExeFile);

            FilesToEmbed = fileList.ToArray();

            return !Log.HasLoggedErrors;
        }

        private string ExtractPluginFileName(string url)
        {
            if (!url.EndsWith(".jar", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("Expecting the url to end with '.jar'");
            }

            var fileName = url.Split('/').Last();

            LogMessage($"Plugin file name: {fileName}");
            return fileName;
        }

        private List<string> FindJsonFiles(string searchRoot)
        {
            var files = new List<string>();

            foreach(var file in KnownJsonFiles)
            {
                files.Add(FindSingleFile(searchRoot, file));
            }

            foreach(var pattern in AdditionalFilesToReturnPattern)
            {
                var matches = Directory.GetFiles(searchRoot, pattern, SearchOption.AllDirectories);
                if (matches.Any())
                {
                    LogMessage($"Found {matches.Count()} files matching for '{pattern}'");
                    files.AddRange(matches);
                }
                else
                {
                    throw new InvalidOperationException($"Failed to find any files matching the pattern '{pattern}'");
                }
            }
            
            return files;
        }
            
        private void EnsureWorkingDirectoryExist(string localWorkingFolder)
        {
            if (!Directory.Exists(localWorkingFolder))
            {
                LogMessage($"Creating working folder: {localWorkingFolder}");
                Directory.CreateDirectory(localWorkingFolder);
            }
        }

        private void DownloadJarFile(string url, string targetFilePath)
        {
            if (File.Exists(targetFilePath))
            {
                // Downloading the file is slow so skip if possible
                LogMessage($"Jar file already exists at {targetFilePath}");
                return;
            }

            LogMessage($"Downloading file from {url} to {targetFilePath}...");

            var timer = Stopwatch.StartNew();

            using (var httpClient = new HttpClient())
            using (var response = httpClient.GetAsync(url).Result)
            {
                if (response.IsSuccessStatusCode)
                {
                    using (var fileStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write))
                    {
                        response.Content.CopyToAsync(fileStream).Wait();
                    }
                }
                else
                {
                    Log.LogError($"Failed to download the CFamily plugin: {response.Content}");
                }
            }
            timer.Stop();
            LogElapsedTime("Download completed. ", timer);
        }

        private void UnzipJar(string jarFilePath, string destinationFolder)
        {
            if (Directory.GetFiles(destinationFolder).Length > 1)
            {
                // Unzipping the jar is slow so skip if possible
                LogMessage($"Skipping unzipping the jar because the destination folder already contains multiple files.");
                return;
            }

            LogMessage($"Unzipping jar file...");
            var timer = Stopwatch.StartNew();

            ZipFile.ExtractToDirectory(jarFilePath, destinationFolder);

            timer.Stop();
            LogElapsedTime("Unzipped jar file", timer);
        }

        private void UncompressAndUnzipTar(string tarFilePath, string destinationFolder)
        {
            // The txz file is compressed using XZ compression and zipped using the tar format.
            // There is no built-in framework support for these format so we are using two
            // open source libraries, both licensed under the MIT license.
            EnsureWorkingDirectoryExist(destinationFolder);

            var uncompresssedFile = DecompressXZFile(tarFilePath, destinationFolder);
            ExtractTarToDirectory(uncompresssedFile, destinationFolder);
        }

        private string DecompressXZFile(string sourceFilePath, string destinationDirectory)
        {
            var destFile = Path.Combine(destinationDirectory, Path.GetFileName(sourceFilePath)) + ".uncompressed";
            if (File.Exists(destFile))
            {
                LogMessage($"Uncompressed tar file already exists: {destFile}");
                return destFile;
            }

            using (Stream xz = new XZStream(File.OpenRead(sourceFilePath)))
            using (Stream outputStream = new FileStream(destFile, FileMode.CreateNew))
            {
                xz.CopyTo(outputStream);
            }
            return destFile;
        }

        public static void ExtractTarToDirectory(string sourceFilePath, string destinationDirectory)
        {
            using (var outputStream = new FileStream(sourceFilePath, FileMode.Open))
            {
                ICSharpCode.SharpZipLib.Tar.TarArchive tarArchive = ICSharpCode.SharpZipLib.Tar.TarArchive.CreateInputTarArchive(outputStream);
                tarArchive.ExtractContents(destinationDirectory);
            }
        }

        private string FindSingleFile(string searchFolder, string pattern)
        {
            var files = Directory.GetFiles(searchFolder, pattern, SearchOption.AllDirectories);

            if (files.Length != 1)
            {
                throw new InvalidOperationException($"Failed to locate one and only one file matching pattern {pattern} in {searchFolder}. Matching files: {files.Length}");
            }

            LogMessage($"Located file: {files[0]}");

            return files[0];
        }

        private void LogMessage(string message)
        {
            Log.LogMessage(MessageImportance.High, message);
        }

        private void LogElapsedTime(string message, Stopwatch timer)
        {
            LogMessage($"{message} {timer.Elapsed.ToString("g")}");
        }
    }
}
