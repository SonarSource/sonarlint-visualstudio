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


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using SharpCompress.Compressors.Xz;

namespace SonarJsConfig
{
    internal static class Common
    {
        public static void EnsureWorkingDirectoryExist(string localWorkingFolder, ILogger logger)
        {
            if (!Directory.Exists(localWorkingFolder))
            {
                LogMessage($"Creating working folder: {localWorkingFolder}", logger);
                Directory.CreateDirectory(localWorkingFolder);
            }
        }

        public static void DownloadJarFile(string url, string targetFilePath, ILogger logger)
        {
            LogMessage($"Download url: {url}", logger);

            if (File.Exists(targetFilePath))
            {
                // Downloading the file is slow so skip if possible
                LogMessage($"Jar file already exists at {targetFilePath}", logger);
                return;
            }

            LogMessage($"Downloading file from {url} to {targetFilePath}...", logger);

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
                    logger.LogError($"Failed to download the CFamily plugin: {response.Content}");
                }
            }
            timer.Stop();
            LogElapsedTime("Download completed. ", timer, logger);
        }

        public static string ExtractPluginFileNameFromUrl(string url, ILogger logger)
        {
            if (!url.EndsWith(".jar", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("Expecting the url to end with '.jar'");
            }

            var fileName = url.Split('/').Last();

            LogMessage($"Plugin file name: {fileName}", logger);
            return fileName;
        }


        public static void UnzipJar(string jarFilePath, string destinationFolder, ILogger logger)
        {
            if (Directory.GetFiles(destinationFolder).Length > 1)
            {
                // Unzipping the jar is slow so skip if possible
                LogMessage($"Skipping unzipping the jar because the destination folder already contains multiple files.", logger);
                return;
            }

            LogMessage($"Unzipping jar file...", logger);
            var timer = Stopwatch.StartNew();

            ZipFile.ExtractToDirectory(jarFilePath, destinationFolder);

            timer.Stop();
            LogElapsedTime("Unzipped jar file", timer, logger);
        }

        public static void UncompressAndUnzipTgx(string tarFilePath, string destinationFolder, ILogger logger)
        {
            // The txz file is compressed using XZ compression and zipped using the tar format.
            // There is no built-in framework support for these format so we are using two
            // open source libraries, both licensed under the MIT license.
            Common.EnsureWorkingDirectoryExist(destinationFolder, logger);

            var uncompresssedFile = Common.DecompressXZFile(tarFilePath, destinationFolder, logger);
            Common.ExtractTarToDirectory(uncompresssedFile, destinationFolder, logger);
        }

        public static string DecompressXZFile(string sourceFilePath, string destinationDirectory, ILogger logger)
        {
            var destFile = Path.Combine(destinationDirectory, Path.GetFileName(sourceFilePath)) + ".uncompressed";
            if (File.Exists(destFile))
            {
                LogMessage($"Uncompressed tar file already exists: {destFile}", logger);
                return destFile;
            }

            using (Stream xz = new XZStream(File.OpenRead(sourceFilePath)))
            using (Stream outputStream = new FileStream(destFile, FileMode.CreateNew))
            {
                xz.CopyTo(outputStream);
            }
            return destFile;
        }

        public static void UncompressAndUnzipTgz(string tarFilePath, string destinationFolder, ILogger logger)
        {
            // The txz file is compressed using XZ compression and zipped using the tar format.
            // There is no built-in framework support for these format so we are using two
            // open source libraries, both licensed under the MIT license.
            EnsureWorkingDirectoryExist(destinationFolder, logger);

            var uncompresssedFile = DecompressGZipFile(tarFilePath, destinationFolder, logger);
            ExtractTarToDirectory(uncompresssedFile, destinationFolder, logger);
        }

        public static string DecompressGZipFile(string sourceFilePath, string destinationDirectory, ILogger logger)
        {
            var destFile = Path.Combine(destinationDirectory, Path.GetFileName(sourceFilePath)) + ".uncompressed";
            if (File.Exists(destFile))
            {
                LogMessage($"Uncompressed tar file already exists: {destFile}", logger);
                return destFile;
            }

            using (Stream xz = new GZipStream(File.OpenRead(sourceFilePath), CompressionMode.Decompress))
            using (Stream outputStream = new FileStream(destFile, FileMode.CreateNew))
            {
                xz.CopyTo(outputStream);
            }
            return destFile;
        }

        public static void ExtractTarToDirectory(string sourceFilePath, string destinationDirectory, ILogger logger)
        {
            if (Directory.GetDirectories(destinationDirectory).Length > 0)
            {
                // Unzipping the jar is slow so skip if possible
                LogMessage($"Skipping unzipping the tar because the destination folder already contains a sub-folder.", logger);
                return;
            }

            using (var outputStream = new FileStream(sourceFilePath, FileMode.Open))
            {
                ICSharpCode.SharpZipLib.Tar.TarArchive tarArchive = ICSharpCode.SharpZipLib.Tar.TarArchive.CreateInputTarArchive(outputStream);
                tarArchive.ExtractContents(destinationDirectory);
            }
        }

        public static List<string> FindSingleFiles(string searchRoot, IEnumerable<string> patterns, ILogger logger)
        {
            var files = new List<string>();

            foreach (var file in patterns)
            {
                files.Add(FindSingleFile(searchRoot, file, logger));
            }

            return files;
        }

        public static List<string> FindMultipleFiles(string searchRoot, IEnumerable<string> patterns, ILogger logger)
        {
            var files = new List<string>();

            foreach (var pattern in patterns)
            {
                var matches = Directory.GetFiles(searchRoot, pattern, SearchOption.AllDirectories);
                if (matches.Any())
                {
                    LogMessage($"Found {matches.Count()} files matching for '{pattern}'", logger);
                    files.AddRange(matches);
                }
                else
                {
                    throw new InvalidOperationException($"Failed to find any files matching the pattern '{pattern}'");
                }
            }

            return files;
        }

        public static string FindSingleFile(string searchFolder, string pattern, ILogger logger)
        {
            var files = Directory.GetFiles(searchFolder, pattern, SearchOption.AllDirectories);

            if (files.Length != 1)
            {
                throw new InvalidOperationException($"Failed to locate one and only one file matching pattern {pattern} in {searchFolder}. Matching files: {files.Length}");
            }

            LogMessage($"Located file: {files[0]}", logger);

            return files[0];
        }

        private static void LogElapsedTime(string message, Stopwatch timer, ILogger logger)
        {
            LogMessage($"{message} {timer.Elapsed.ToString("g")}", logger);
        }

        private static void LogMessage(string message, ILogger logger)
        {
            logger.LogMessage(message);
        }
    }
}
