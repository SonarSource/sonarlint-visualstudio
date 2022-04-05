﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.IO;

namespace SonarLint.VisualStudio.Core.CFamily
{
    /// <summary>
    /// Returns file paths for various CFamily directories/files
    /// </summary>
    public static class SubProcessFilePaths
    {

        static SubProcessFilePaths()
        {
            Directory.CreateDirectory(PchFileDirectory);
        }

        private static string PchFileName = Guid.NewGuid().ToString();
        private static string PchFileDirectory = Path.Combine(WorkingDirectory, "SLVS", "PCH");
        public static string WorkingDirectory => Path.GetTempPath();
        public static string PchFilePath => Path.Combine(PchFileDirectory, PchFileName);
        public static string RequestConfigFilePath => Path.Combine(WorkingDirectory, "sonar-cfamily.request.reproducer");
        public static string ReproducerFilePath => Path.Combine(WorkingDirectory, "sonar-cfamily.reproducer");

        private static readonly string CFamilyFilesDirectory = Path.Combine(
            Path.GetDirectoryName(typeof(SubProcessFilePaths).Assembly.Location),
            "lib");

        public static readonly string AnalyzerExeFilePath = Path.Combine(CFamilyFilesDirectory, "subprocess.exe");
    }
}
