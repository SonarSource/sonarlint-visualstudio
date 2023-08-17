/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace SonarLint.VisualStudio.Core
{
    public interface IGlobPatternMatcher
    {
        bool IsMatch(string pattern, string input);
    }

    public class GlobPatternMatcher : IGlobPatternMatcher
    {
        private readonly ILogger logger;

        public GlobPatternMatcher(ILogger logger)
        {
            this.logger = logger;
        }

        public bool IsMatch(string pattern, string input)
        {
            var wildcardPattern = WildcardPattern.create(pattern);
            var isMatch = wildcardPattern.match(input);

            logger.LogVerbose("[GlobPatternMatcher]" +
                            $"\n  Pattern: {pattern}" +
                            $"\n  Regex: {wildcardPattern.pattern}" +
                            $"\n  Input: {input}" +
                            $"\n  Is match: {isMatch}");

            return isMatch;
        }

        /// <summary>
        /// Copied as-is from https://github.com/SonarSource/sonar-plugin-api/blob/a9bd7ff48f0f77811ed909070030678c443c975a/sonar-plugin-api/src/main/java/org/sonar/api/utils/WildcardPattern.java
        /// </summary>
        private class WildcardPattern
        {
            private static readonly ConcurrentDictionary<string, WildcardPattern> CACHE = new ConcurrentDictionary<string, WildcardPattern>();
            private static readonly string SPECIAL_CHARS = "()[]^$.{}+|";
            internal readonly Regex pattern;

            private WildcardPattern(string pattern, string directorySeparator)
            {
                this.pattern = new Regex(toRegexp(pattern, directorySeparator),
                                RegexOptions.Compiled,
                                RegexConstants.DefaultTimeout);
            }

            private static string toRegexp(string antPattern, string directorySeparator)
            {
                var escapedDirectorySeparator = '\\' + directorySeparator;
                var sb = new StringBuilder(antPattern.Length);

                sb.Append('^');

                int i = antPattern.StartsWith("/") || antPattern.StartsWith("\\") ? 1 : 0;
                while (i < antPattern.Length)
                {
                    var ch = antPattern[i];

                    if (SPECIAL_CHARS.IndexOf(ch) != -1)
                    {
                        // Escape regexp-specific characters
                        sb.Append('\\').Append(ch);
                    }
                    else if (ch == '*')
                    {
                        if (i + 1 < antPattern.Length && antPattern[i + 1] == '*')
                        {
                            // Double asterisk
                            // Zero or more directories
                            if (i + 2 < antPattern.Length && isSlash(antPattern[i + 2]))
                            {
                                sb.Append("(?:.*").Append(escapedDirectorySeparator).Append("|)");
                                i += 2;
                            }
                            else
                            {
                                sb.Append(".*");
                                i += 1;
                            }
                        }
                        else
                        {
                            // Single asterisk
                            // Zero or more characters excluding directory separator
                            sb.Append("[^").Append(escapedDirectorySeparator).Append("]*?");
                        }
                    }
                    else if (ch == '?')
                    {
                        // Any single character excluding directory separator
                        sb.Append("[^").Append(escapedDirectorySeparator).Append("]");
                    }
                    else if (isSlash(ch))
                    {
                        // Directory separator
                        sb.Append(escapedDirectorySeparator);
                    }
                    else
                    {
                        // Single character
                        sb.Append(ch);
                    }

                    i++;
                }

                sb.Append('$');

                return sb.ToString();
            }

            private static bool isSlash(char ch)
            {
                return ch == '/' || ch == '\\';
            }

            /**
             * Returns true if specified value matches this pattern.
             */
            public bool match(string value)
            {
                value = value.TrimStart('/');
                value = value.TrimEnd('/');
                return pattern.IsMatch(value);
            }
     
            /**
             * Creates pattern with "/" as a directory separator.
             * 
             * @see #create(string, string)
             */
            public static WildcardPattern create(string pattern)
            {
                return create(pattern, "/");
            }

            /**
             * Creates pattern with specified separator for directories.
             * <p>
             * This is used to match Java-classes, i.e. <code>org.foo.Bar</code> against <code>org/**</code>.
             * <b>However usage of character other than "/" as a directory separator is misleading and should be avoided,
             * so method {@link #create(string)} is preferred over this one.</b>
             * 
             * <p>
             * Also note that no matter whether forward or backward slashes were used in the <code>antPattern</code>
             * the returned pattern will use <code>directorySeparator</code>.
             * Thus to match Windows-style path "dir\file.ext" against pattern "dir/file.ext" normalization should be performed.
             * 
             */
            public static WildcardPattern create(string pattern, string directorySeparator)
            {
                string key = pattern + directorySeparator;
                return CACHE.GetOrAdd(key, k=> new WildcardPattern(pattern, directorySeparator));
            }
        }
    }
}
