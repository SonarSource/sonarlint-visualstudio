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
