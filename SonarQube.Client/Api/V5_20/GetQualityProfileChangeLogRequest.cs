/*
 * SonarQube Client
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
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V5_20
{
    public class GetQualityProfileChangeLogRequest : PagedRequestBase<DateTime>, IGetQualityProfileChangeLogRequest
    {
        [JsonProperty("profileKey")]
        public virtual string QualityProfileKey { get; set; }

        /// <summary>
        /// This property does not exist before SonarQube 6.5 and is deliberately implemented to always return
        /// the default value in order to prevent it from serialization.
        /// </summary>
        [JsonProperty("qualityProfile", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(null)]
        public virtual string QualityProfileName
        {
            get { return null; }
            set { /* not supported in this implementation */ }
        }

        /// <summary>
        /// This property does not exist before SonarQube 6.5 and is deliberately implemented to always return
        /// the default value in order to prevent it from serialization.
        /// </summary>
        [JsonProperty("language", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(null)]
        public virtual string LanguageName
        {
            get { return null; }
            set { /* not supported in this implementation */ }
        }

        /// <summary>
        /// This property does not exist before SonarQube 6.4 and is deliberately implemented to always return
        /// the default value in order to prevent it from serialization.
        /// </summary>
        [JsonProperty("organization", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(null)]
        public virtual string OrganizationKey
        {
            get { return null; }
            set { /* not supported in this implementation */ }
        }

        protected override string Path => "api/qualityprofiles/changelog";

        protected override DateTime[] ParseResponse(string response) =>
            JObject.Parse(response)["events"]
                .ToObject<QualityProfileChangeItemResponse[]>()
                .Select(x => x.Date)
                .ToArray();

        private class QualityProfileChangeItemResponse
        {
            [JsonProperty("date")]
            public DateTime Date { get; set; }
        }
    }
}
