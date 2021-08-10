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

using System;
using System.IO;
using Newtonsoft.Json;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.CMake
{
    /// <summary>
    /// Encapsulates a low-level analysis request for the CFamily compilation database entry protocol
    /// </summary>
    internal class CompilationDatabaseRequest : IRequest
    {
        private readonly CompilationDatabaseEntry databaseEntry;

        public CompilationDatabaseRequest(CompilationDatabaseEntry databaseEntry, RequestContext context)
        {
            this.databaseEntry = databaseEntry ?? throw new ArgumentNullException(nameof(databaseEntry));
            Context = context ?? throw new ArgumentNullException(nameof(context));

            // Must have a Command or Arguments but not both
            var hasArgs = !string.IsNullOrEmpty(databaseEntry.Arguments);
            var hasCmd = !string.IsNullOrEmpty(databaseEntry.Command);

            if (!hasArgs && !hasCmd || (hasArgs && hasCmd))
            {
                throw new ArgumentException(CFamilyStrings.ERROR_InvalidCompilationEntry);
            }
        }

        public RequestContext Context { get; }

        public void WriteRequest(BinaryWriter writer)
        {
            WriteHeader(writer);

            // Required inputs
            WriteSetting(writer, "File", databaseEntry.File);
            WriteSetting(writer, "Directory", databaseEntry.Directory);

            if(databaseEntry.Arguments == null)
            {
                WriteSetting(writer, "Command", databaseEntry.Command);
            }
            else
            {
                WriteSetting(writer, "Arguments", databaseEntry.Arguments);
            }

            WriteQualityProfile(writer);

            // Optional inputs
            WriteSetting(writer, "PreambleFile", Context.PchFile);
            WriteSetting(writer, "CreateReproducer", Context?.AnalyzerOptions?.CreateReproducer ?? false ? "true" : "false");
            WriteSetting(writer, "BuildPreamble", Context?.AnalyzerOptions?.CreatePreCompiledHeaders ?? false ? "true" : "false");

            // TODO - only supplied if analysing a header file
//            WriteSetting(writer, "HeaderFilerLanguage", Context.CFamilyLanguage);
            WriteRuleSettings(writer);

            WriteFooter(writer);
        }

        private static void WriteHeader(BinaryWriter writer)
            => Protocol.WriteUTF(writer, "SL-IN");

        private static void WriteFooter(BinaryWriter writer)
            => Protocol.WriteUTF(writer, "SL-END");

        public void WriteRequestDiagnostics(TextWriter writer)
        {
            var data = JsonConvert.SerializeObject(databaseEntry, Formatting.Indented);
            writer.Write(data);
        }

        private void WriteQualityProfile(BinaryWriter writer)
        {
            // Comma-separated list of active rule ids
            string ids = string.Empty;

            // Optimisation - no point in calculating the active rules if we're
            // creating a pre-compiled header, as they won't be used.
            // However, the QualityProfile is an essential setting so we still
            // have to write it.
            if (!Context?.AnalyzerOptions?.CreatePreCompiledHeaders ?? true)
            {
                ids = string.Join(",", Context.RulesConfiguration.ActivePartialRuleKeys);
            }

            WriteSetting(writer, "QualityProfile", ids);
        }

        private void WriteRuleSettings(BinaryWriter writer)
        {
            // The key for each individual setting is in the form {ruleId}.{configname}

            foreach(var ruleId in Context.RulesConfiguration.ActivePartialRuleKeys)
            {
                if (Context.RulesConfiguration.RulesParameters.TryGetValue(ruleId, out var parameters))
                {
                    foreach(var kvp in parameters)
                    {
                        var key = ruleId + "." + kvp.Key;
                        WriteSetting(writer, key, kvp.Value);
                    }
                }
            }
        }

        private static void WriteSetting(BinaryWriter writer, string key, string value)
        {
            Protocol.WriteUTF(writer, key);
            Protocol.WriteUTF(writer, value);
        }
    }
}
