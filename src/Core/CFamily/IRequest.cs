/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System.IO;

namespace SonarLint.VisualStudio.Core.CFamily
{
    /// <summary>
    /// Abstraction describing low-level analysis requests to the CFamily subprocess
    /// </summary>
    /// <remarks>The subprocess supports several different protocols, each of which requires a different set of inputs.
    /// This abstraction exposes the common data items that are required on the SonarLint side to be able to process
    /// the different request types in a generic way e.g. filtering the results</remarks>
    public interface IRequest
    {
        // Note: the language and RulesConfiguration aren't passed as part of the request to the
        // CLang analyzer, but it is by SVLS used when filtering the returned issues.
        string CFamilyLanguage { get; }

        ICFamilyRulesConfig RulesConfiguration { get; }

        /// <summary>
        /// The full path to the file being analyzed
        /// </summary>
        string File { get; }

        /// <summary>
        /// Full path to the precompiled header file (also called the "preamble" file)
        /// </summary>
        /// <remarks>The file may not exist. If it does, it might be out of date or for a different file.
        /// However, it is the responsibility of the CFamily subprocess to handle all of those scenarios.</remarks>
        string PchFile{ get; }

        /// <summary>
        /// Additional analysis options
        /// </summary>
        CFamilyAnalyzerOptions AnalyzerOptions { get; }

        /// <summary>
        /// Serializes the request in the form required by the subprocess
        /// </summary>
        void WriteRequest(BinaryWriter writer);

        /// <summary>
        /// Serializes the request for diagnostic purposes
        /// </summary>
        void WriteRequestDiagnostics(TextWriter writer);
    }
}
