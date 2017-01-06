/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

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