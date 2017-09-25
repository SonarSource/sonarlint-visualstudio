/*
 * SonarQube Client
 * Copyright (C) 2016-2017 SonarSource SA
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
using Newtonsoft.Json;

namespace SonarQube.Client.Models
{
    public class QualityProfile
    {
        // Ordinal comparer, similar to project key comparer
        public static readonly StringComparer KeyComparer = StringComparer.Ordinal;

        public string Key { get; }
        public string Name { get; }
        public string Language { get; }
        public bool IsDefault { get; }
        public DateTime TimeStamp { get; }

        public QualityProfile(string key, string name, string language, bool isDefault, DateTime timeStamp)
        {
            Key = key;
            Name = name;
            Language = language;
            IsDefault = isDefault;
            TimeStamp = timeStamp;
        }

        public static QualityProfile FromDto(QualityProfileDTO dto, DateTime timeStamp)
        {
            return new QualityProfile(dto.Key, dto.Name, dto.Language, dto.IsDefault, timeStamp);
        }
    }

    public class QualityProfileDTO
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("isDefault")]
        public bool IsDefault { get; set; }
    }

    public class QualityProfileChangeLogEventDTO
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }

    public class QualityProfileChangeLogDTO
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("ps")]
        public int PageSize { get; set; }

        [JsonProperty("p")]
        public int Page { get; set; }

        [JsonProperty("events")]
        public QualityProfileChangeLogEventDTO[] Events { get; set; }
    }

    public class QualityProfileRequest
    {
        public string ProjectKey { get; set; }
    }

    public class QualityProfileChangeLogRequest
    {
        public string QualityProfileKey { get; set; }
        public int PageSize { get; set; }
    }
}
