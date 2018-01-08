/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
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

using Newtonsoft.Json;

namespace SonarQube.Client.Messages
{
    public class RoslynExportProfileRequest
    {
        [JsonProperty("language")]
        public string LanguageKey { get; set; }
        [JsonProperty("name")]
        public virtual string QualityProfileName { get; set; }
        [JsonProperty("exporterKey", Required = Required.Always)]
        public string ExporterKey => $"roslyn-{LanguageKey}";
    }

    public class RoslynExportProfileRequestV66Plus : RoslynExportProfileRequest
    {
        [JsonProperty("qualityProfile")]
        public override string QualityProfileName { get; set; }
    }
}