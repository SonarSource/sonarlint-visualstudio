/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using SonarLint.VisualStudio.CFamily.Rules;

namespace SonarLint.VisualStudio.CFamily.Analysis
{
    /// <summary>
    /// Data class containing information about an analysis request
    /// </summary>
    public class RequestContext
    {
        public RequestContext(string language, ICFamilyRulesConfig rulesConfig, string file, string pchFile, CFamilyAnalyzerOptions analyzerOptions, bool isHeaderFile)
        {
            CFamilyLanguage = language;
            RulesConfiguration = rulesConfig;
            File = file;
            PchFile = pchFile;
            AnalyzerOptions = analyzerOptions;
            IsHeaderFile = isHeaderFile;
        }

        // Note: the language and RulesConfiguration aren't passed as part of the request to the
        // CLang analyzer, but it is by SVLS used when filtering the returned issues.
        public string CFamilyLanguage { get; }

        public ICFamilyRulesConfig RulesConfiguration { get; }

        /// <summary>
        /// The full path to the file being analyzed
        /// </summary>
        public string File { get; }

        /// <summary>
        /// Full path to the precompiled header file (also called the "preamble" file)
        /// </summary>
        /// <remarks>The file may not exist. If it does, it might be out of date or for a different file.
        /// However, it is the responsibility of the CFamily subprocess to handle all of those scenarios.</remarks>
        public string PchFile { get; }

        /// <summary>
        /// Additional analysis options
        /// </summary>
        public CFamilyAnalyzerOptions AnalyzerOptions { get; }

        public bool IsHeaderFile { get; }
    }
}
