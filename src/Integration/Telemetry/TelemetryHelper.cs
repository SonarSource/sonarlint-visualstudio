/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SonarLint.VisualStudio.Integration
{
    public static class TelemetryHelper
    {
        public static readonly string SonarLintVersion = GetSonarLintVersion();
        public static readonly string VisualStudioVersion = GetVisualStudioVersion();

        private static string GetSonarLintVersion()
        {
            return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        }

        private static string GetVisualStudioVersion()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "msenv.dll");
            return File.Exists(path)
                ? FileVersionInfo.GetVersionInfo(path).ProductVersion
                : string.Empty;
        }
    }
}
