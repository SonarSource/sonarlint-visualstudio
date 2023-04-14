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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract
{
    internal class JsTsAnalysisResponse
    {
        // Other fields omitted
        // i.e. Highlight[], HighlightedSymbol[], Metrics, CpdToken[]

        [JsonProperty("issues")]
        public IEnumerable<Issue> Issues { get; set; }

        [JsonProperty("parsingError")]
        public ParsingError ParsingError { get; set; }
    }

    internal class ParsingError
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public ParsingErrorCode Code { get; set; }
    }

    internal enum ParsingErrorCode
    {
        PARSING,
        MISSING_TYPESCRIPT,
        UNSUPPORTED_TYPESCRIPT,
        FAILING_TYPESCRIPT,
        GENERAL_ERROR,
        LINTER_INITIALIZATION
    }
}
