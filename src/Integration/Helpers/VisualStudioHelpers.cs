/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration
{
    internal static class VisualStudioHelpers
    {
        private static readonly Version visualStudio2015Update3Version = Version.Parse("14.0.25420.00");

        public static string VisualStudioVersion
        {
            get;
            /* for testing purpose only */ internal set;
        } = GetVisualStudioVersion();

        public static bool IsVisualStudioBeforeUpdate3()
        {
            Version vsVersion;
            return Version.TryParse(VisualStudioVersion, out vsVersion) &&
                vsVersion < visualStudio2015Update3Version;
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
