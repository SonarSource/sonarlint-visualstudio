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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SonarLint.VisualStudio.Integration
{
    internal static class PathHelper
    {
        /// <summary>
        /// Replace all invalid file path characters with the underscore ("_").
        /// </summary>
        public static string EscapeFileName(string unescapedName)
        {
            const char safeChar = '_';

            if (string.IsNullOrWhiteSpace(unescapedName))
            {
                return unescapedName;
            }

            IEnumerable<char> invalidChars = Enumerable.Union(Path.GetInvalidPathChars(), Path.GetInvalidFileNameChars());
            var escapedName = new StringBuilder(unescapedName);
            foreach (var invalidChar in invalidChars)
            {
                escapedName.Replace(invalidChar, safeChar);
            }

            return escapedName.ToString();
        }

        /// <summary>
        /// Append a <see cref="Path.DirectorySeparatorChar"/> to the end of the string if one does not already exist.
        /// </summary>
        public static string ForceDirectoryEnding(string str)
        {
            if (!str.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return str + Path.DirectorySeparatorChar;
            }

            return str;
        }

        /// <summary>
        /// Compute the path of <paramref name="toFullPath"/>, relative to <paramref name="fromFullPath"/>.
        /// </summary>
        /// <param name="fromFullPath">Path with which to make <paramref name="toFullPath"/> relative to</param>
        /// <param name="toFullPath">Absolute path</param>
        /// <returns>Relative path if one exists, otherwise the original <paramref name="toFullPath"/></returns>
        public static string CalculateRelativePath(string fromFullPath, string toFullPath)
        {
            if (string.IsNullOrWhiteSpace(toFullPath))
            {
                throw new ArgumentNullException(nameof(toFullPath));
            }

            if (string.IsNullOrWhiteSpace(fromFullPath))
            {
                throw new ArgumentNullException(nameof(fromFullPath));
            }

            Uri absoluteUri;
            Uri relativeToUri;

            if (!Uri.TryCreate(toFullPath, UriKind.Absolute, out absoluteUri))
            {
                throw new ArgumentException(Resources.Strings.PathHelperAbsolutePathExpected, nameof(toFullPath));
            }

            if (!Uri.TryCreate(fromFullPath, UriKind.Absolute, out relativeToUri))
            {
                throw new ArgumentException(Resources.Strings.PathHelperAbsolutePathExpected, nameof(fromFullPath));
            }


            Uri relativeUri = relativeToUri.MakeRelativeUri(absoluteUri);
            return ToFilePathString(relativeUri);
        }

        /// <summary>
        /// Resolve a relative path against the given root path.
        /// </summary>
        /// <param name="relativePath">Relative path to resolve</param>
        /// <param name="resolutionRootFullPath">Root full path for the resolution of <paramref name="relativePath"/></param>
        /// <returns>Full absolute path</returns>
        public static string ResolveRelativePath(string relativePath, string resolutionRootFullPath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (string.IsNullOrWhiteSpace(resolutionRootFullPath))
            {
                throw new ArgumentNullException(nameof(resolutionRootFullPath));
            }

            Uri rootUri;

            if (!Uri.TryCreate(resolutionRootFullPath, UriKind.Absolute, out rootUri))
            {
                throw new ArgumentException(Resources.Strings.PathHelperAbsolutePathExpected, nameof(resolutionRootFullPath));
            }

            Uri expandedUri = new Uri(rootUri, relativePath);
            return ToFilePathString(expandedUri);
        }

        /// <summary>
        /// Gets whether or not the given <paramref name="path"/> has exists under the given <paramref name="rootDirectory"/>.
        /// </summary>
        public static bool IsPathRootedUnderRoot(string path, string rootDirectory)
        {
            Debug.Assert(Path.IsPathRooted(path) || Path.IsPathRooted(rootDirectory), "Path should be absolute");

            string expandedPath = Path.GetFullPath(path);
            string expandedRootDirectory = Path.GetFullPath(rootDirectory);

            return expandedPath.StartsWith(expandedRootDirectory, StringComparison.OrdinalIgnoreCase);
        }

        private static string ToFilePathString(Uri uri)
        {
            string escapedPath = uri.OriginalString.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return Uri.UnescapeDataString(escapedPath);
        }
    }
}
