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

using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    internal interface IMacroEvaluationService
    {
        /// <summary>
        /// Evaluates any macros and environment variables in the input and returns the expanded
        /// value
        /// </summary>
        /// <param name="input">The string to be evaluated</param>
        /// <param name="evaluationContext">Information used for the evaluation</param>
        /// <returns>The evaluated result, or null if the input contained properties that could not be evaluated.</returns>
        string Evaluate(string input, IEvaluationContext evaluationContext);
    }

    internal class MacroEvaluationService : IMacroEvaluationService
    {
        private readonly ILogger logger;
        private readonly IMacroEvaluator macroEvaluator;

        // Looks for patterns like "${name}" and "${prefix.name}" and captures the [prefix, name] tuple.
        private static readonly Regex CMakeSettingsMacroRegex = new Regex(
            "\\${" +                 // Match opening literals "${". $ is a special Regex symbol that needs be escaped,
                                     // and "\" needs to be escaped in the C# string.
            "((?<prefix>\\w+)\\.)" + // One or more word chars followed by ".". Like "$" above, the "." needs to be escaped with "\\".
                                     // The word chars are captured in a group called "prefix".
            "?" +                    // Match the previous group 0 or 1 times i.e. the prefix is optional.
            "(?<name>\\w+)" +        // Capture one or more word consecutive word chars in a group called "name".
            "}",                     // Match the closing literal.
            RegexOptions.Compiled,
            Core.RegexConstants.DefaultTimeout);

        public MacroEvaluationService(ILogger logger)
            :this(logger, new MacroEvaluator()) {}

        internal MacroEvaluationService(ILogger logger, IMacroEvaluator macroEvaluator)
        {
            this.logger = logger;
            this.macroEvaluator = macroEvaluator;
        }

        public string Evaluate(string input, IEvaluationContext evaluationContext)
        {
            if (input == null)
            {
                return null;
            }

            var sb = new StringBuilder(input);

            foreach(Match match in CMakeSettingsMacroRegex.Matches(input))
            {
                var prefix= match.Groups["prefix"].Value; // will be String.Empty if not found
                var name = match.Groups["name"].Value;

                var evaluatedProperty = macroEvaluator.TryEvaluate(prefix, name, evaluationContext);

                if (evaluatedProperty == null)
                {
                    // Give up if we failed to evaluate any property
                    logger.WriteLine(Resources.MacroEval_FailedToEvaluateMacro, match.Value);
                    return null;
                }

                LogDebug($"{prefix}.{name} = {evaluatedProperty}");
                sb.Replace(match.Value, evaluatedProperty);
            }
            
            return sb.ToString();
        }

        private void LogDebug(string message)
        {
            logger.LogVerbose($"[CMake:MacroEval] [Thread id: {Thread.CurrentThread.ManagedThreadId}] {message}");
        }
    }
}
