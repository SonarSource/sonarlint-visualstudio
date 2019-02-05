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

using System.IO;
using Newtonsoft.Json;
using SonarQube.Client.Messages;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V5_20
{
    public class GetRoslynExportProfileRequest : RequestBase<RoslynExportProfileResponse>, IGetRoslynExportProfileRequest
    {
        [JsonProperty("language")]
        public virtual string LanguageKey { get; set; }

        [JsonProperty("name")]
        public virtual string QualityProfileName { get; set; }

        [JsonProperty("organization")]
        public virtual string OrganizationKey { get; set; }

        [JsonProperty("exporterKey", Required = Required.Always)]
        public virtual string ExporterKey => $"roslyn-{LanguageKey}";

        protected override string Path => "api/qualityprofiles/export";

        protected override RoslynExportProfileResponse ParseResponse(string response)
        {
            using (var reader = new StringReader(response))
            {
                // Consider not returning the xml directly
                return RoslynExportProfileResponse.Load(reader);
            }
        }
    }
}
