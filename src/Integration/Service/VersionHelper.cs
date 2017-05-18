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
using System.Globalization;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.Service
{
    internal static class VersionHelper
    {
        private const char PrereleaseSeparator = '-';

        /// <summary>
        /// Compare two SonarQube version strings in the format:<para/>
        ///   \d\.\d(\.\d){0,1}(\.\d){0,1}(-[a-zA-Z\d]+){0,1}<para/>
        /// Up to four numerical components, and an optional pre-release
        /// identifier string suffix "-STR".
        /// </summary>
        /// <returns>
        /// &lt;0 if <paramref name="versionA"/> is less than <paramref name="versionB"/>,<para/>
        /// &gt;0 if <paramref name="versionA"/> is greater than <paramref name="versionB"/>,<para/>
        /// 0 if they are equal.
        /// </returns>
        public static int Compare(string versionA, string versionB)
        {
            if (string.IsNullOrWhiteSpace(versionA))
            {
                throw new ArgumentNullException(nameof(versionA));
            }

            if (string.IsNullOrWhiteSpace(versionB))
            {
                throw new ArgumentNullException(nameof(versionB));
            }

            // Strip out pre-release strings (we will ignore them)
            // https://github.com/Microsoft/vso-agent-tasks/blob/3d84e41/Tasks/SonarQubePreBuild/SonarQubePreBuildImpl.ps1#L126
            string numericA = StripPrereleaseString(versionA);
            string numericB = StripPrereleaseString(versionB);

            Version a;
            if (!Version.TryParse(numericA, out a))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.CannotCompareVersionStrings, versionA), nameof(versionA));
            }

            Version b;
            if (!Version.TryParse(numericB, out b))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.CannotCompareVersionStrings, versionB), nameof(versionB));
            }

            return a.CompareTo(b);
        }

        private static string StripPrereleaseString(string input)
        {
            bool isPrereleaseVersion = input.Contains(PrereleaseSeparator.ToString());
            if (isPrereleaseVersion)
            {
                return input.Split(PrereleaseSeparator)[0];
            }

            return input;
        }
    }
}