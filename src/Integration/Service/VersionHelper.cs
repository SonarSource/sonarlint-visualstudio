//-----------------------------------------------------------------------
// <copyright file="VersionHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.Service
{
    internal static class VersionHelper
    {
        private const char PrereleaseSeparator = '-';

        /// <summary>
        /// Compare two SonarQube version strings in the format:<para/>
        ///   \d\.\d(\.\d){0,1}(\.\d){0,1}(-[a-zA-Z\d]+){0,1}<para/>
        /// Up to four numerical components, and an optional prerelease
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

            // Strip out prerelease strings (we will ignore them)
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