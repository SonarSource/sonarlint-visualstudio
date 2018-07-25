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

namespace SonarQube.Client.Api.Requests.V6_50
{
    public class GetQualityProfileChangeLogRequest : V5_20.GetQualityProfileChangeLogRequest
    {
        [JsonIgnore]
        public override string QualityProfileKey { get; set; }

        public override string QualityProfileName { get; set; }

        public override string LanguageName { get; set; }

        // Strictly speaking this parameter was added in v6.4. However,
        // it is only relevant for SonarCloud which is now on a later version
        // than v6.5, so there is no point an adding a v6.4 version of this class.
        public override string OrganizationKey { get; set; }
    }
}
