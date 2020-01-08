/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Linq;
using System.Text;
using System.Threading;

// Note: copied from the S4MSB
// https://github.com/SonarSource/sonar-scanner-msbuild/blob/b28878e21cbdda9aca6bd08d90c3364cca882861/src/SonarScanner.MSBuild.Common/ProcessRunnerArguments.cs#L33

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    /// <summary>
    /// Data class containing parameters required to execute a new process
    /// </summary>
    public class ProcessRunnerArguments
    {
        // NOTE: the list of sensitive keys is hard-coded in the S4MSB. 
        // We've made it a property here to limit the amount of refactoring required to use the
        // class here, although currently SLVS doesn't need to pass any sensitive arguments.

        /// <summary>
        /// Strings that are used to indicate arguments that contain
        /// sensitive data that should not be logged
        /// </summary>
        public IEnumerable<string> SensitivePropertyKeys { get; set; } = Enumerable.Empty<string>();

        public ProcessRunnerArguments(string exeName, bool isBatchScript)
        {
            if (string.IsNullOrWhiteSpace(exeName))
            {
                throw new ArgumentNullException(nameof(exeName));
            }

            ExeName = exeName;
            IsBatchScript = isBatchScript;

            TimeoutInMilliseconds = Timeout.Infinite;
        }

        #region Public properties

        public string ExeName { get; }

        /// <summary>
        /// Non-sensitive command line arguments (i.e. ones that can safely be logged). Optional.
        /// </summary>
        public IEnumerable<string> CmdLineArgs { get; set; }

        public string WorkingDirectory { get; set; }

        public int TimeoutInMilliseconds { get; set; }

        private bool IsBatchScript { get; set; }

        /// <summary>
        /// Additional environments variables that should be set/overridden for the process. Can be null.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }

        public string GetEscapedArguments()
        {
            if (CmdLineArgs == null)
            { return null; }

            var result = string.Join(" ", CmdLineArgs.Select(a => EscapeArgument(a)));

            if (IsBatchScript)
            {
                result = ShellEscape(result);
            }

            return result;
        }

        /// <summary>
        /// Returns the string that should be used when logging command line arguments
        /// (sensitive data will have been removed)
        /// </summary>
        public string AsLogText()
        {
            if (CmdLineArgs == null)
            { return null; }

            var hasSensitiveData = false;

            var sb = new StringBuilder();

            foreach (var arg in CmdLineArgs)
            {
                if (ContainsSensitiveData(arg))
                {
                    hasSensitiveData = true;
                }
                else
                {
                    sb.Append(arg);
                    sb.Append(" ");
                }
            }

            if (hasSensitiveData)
            {
                sb.Append(CFamilyStrings.MSG_CmdLine_SensitiveCmdLineArgsAlternativeText);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines whether the text contains sensitive data that
        /// should not be logged/written to file
        /// </summary>
        private bool ContainsSensitiveData(string text)
        {
            Debug.Assert(SensitivePropertyKeys != null, "SensitiveDataMarkers array should not be null");

            if (text == null)
            {
                return false;
            }

            return SensitivePropertyKeys.Any(marker => text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) > -1);
        }

        /// <summary>
        /// The CreateProcess Win32 API call only takes 1 string for all arguments.
        /// Ultimately, it is the responsibility of each program to decide how to split this string into multiple arguments.
        ///
        /// See:
        /// https://blogs.msdn.microsoft.com/oldnewthing/20100917-00/?p=12833/
        /// https://blogs.msdn.microsoft.com/twistylittlepassagesallalike/2011/04/23/everyone-quotes-command-line-arguments-the-wrong-way/
        /// http://www.daviddeley.com/autohotkey/parameters/parameters.htm
        /// </summary>
        private static string EscapeArgument(string arg)
        {
            Debug.Assert(arg != null, "Not expecting an argument to be null");

            var sb = new StringBuilder();

            sb.Append("\"");
            for (var i = 0; i < arg.Length; i++)
            {
                var numberOfBackslashes = 0;
                for (; i < arg.Length && arg[i] == '\\'; i++)
                {
                    numberOfBackslashes++;
                }

                if (i == arg.Length)
                {
                    //
                    // Escape all backslashes, but let the terminating
                    // double quotation mark we add below be interpreted
                    // as a meta-character.
                    //
                    sb.Append('\\', numberOfBackslashes * 2);
                }
                else if (arg[i] == '"')
                {
                    //
                    // Escape all backslashes and the following
                    // double quotation mark.
                    //
                    sb.Append('\\', numberOfBackslashes * 2 + 1);
                    sb.Append(arg[i]);
                }
                else
                {
                    //
                    // Backslashes aren't special here.
                    //
                    sb.Append('\\', numberOfBackslashes);
                    sb.Append(arg[i]);
                }
            }
            sb.Append("\"");

            return sb.ToString();
        }

        /// <summary>
        /// Batch scripts are evil.
        /// The escape character in batch is '^'.
        ///
        /// Example:
        /// script.bat : echo %*
        /// cmd.exe: script.bat foo^>out.txt
        ///
        /// This passes the argument "foo >out.txt" to script.bat.
        /// Variable expansion happen before execution (i.e. it is preprocessing), so the script becomes:
        ///
        /// echo foo>out.txt
        ///
        /// which will write "foo" into the file "out.txt"
        ///
        /// To avoid this, one must call:
        /// cmd.exe: script.bat foo^^^>out.txt
        ///
        /// which gets rewritten into: echo foo^>out.txt
        /// and then executed.
        ///
        /// Note: Delayed expansion is not available for %*, %1
        /// set foo=%* and set foo="%*" with echo !foo!
        /// will only move the command injection away from the "echo" to the "set" itself.
        /// </summary>
        private static string ShellEscape(string argLine)
        {
            var sb = new StringBuilder();
            foreach (var c in argLine)
            {
                // This escape is required after %* is expanded to prevent command injections
                sb.Append('^');
                sb.Append('^');

                // This escape is required only to pass the argument line to the batch script
                sb.Append('^');
                sb.Append(c);
            }
            return sb.ToString();
        }

        #endregion Public properties
    }
}
