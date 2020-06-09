/*
 * SonarLint for Visual Studio
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Core.CFamily
{
    internal class RulesLoader
    {
        private readonly string rulesDirectoryPath;

        public RulesLoader(string rulesDirectoryPath)
        {
            this.rulesDirectoryPath = rulesDirectoryPath ?? throw new ArgumentNullException(nameof(rulesDirectoryPath));
        }

        public IEnumerable<string> ReadRulesList()
        {
            var rulesList = LoadCFamilyJsonFile<List<string>>("RulesList.json");
            Debug.Assert(rulesList != null, "The CFamily RulesList.json should exist and not be empty");

            return rulesList;
        }

        public IEnumerable<string> ReadActiveRulesList()
        {
            var rulesProfile = LoadCFamilyJsonFile<RulesProfile>("Sonar_way_profile.json");
            Debug.Assert(rulesProfile != null, "The CFamily Sonar_way_profile.json should exist and not be empty");

            return rulesProfile.RuleKeys;
        }

        public IDictionary<string, string> ReadRuleParams(String ruleKey)
        {
            var ruleParams = LoadCFamilyJsonFile<RuleParameter[]>(ruleKey + "_params.json");

            if (ruleParams == null)
            {
                return new Dictionary<string, string>();
            }

            var result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var param in ruleParams)
            {
                result.Add(param.Key, param.DefaultValue);
            }
            return result;
        }

        public RuleMetadata ReadRuleMetadata(String ruleKey)
        {
            var ruleMetadata = LoadCFamilyJsonFile<RuleMetadata>(ruleKey + ".json");

            if (ruleMetadata == null)
            {
                throw new FileNotFoundException("Unable to find metadata of rule: " + ruleKey);
            }

            return ruleMetadata;
        }

        private T LoadCFamilyJsonFile<T>(string fileName) where T : class
        {
            string path = Path.Combine(this.rulesDirectoryPath, fileName);
            if (!File.Exists(path))
            {
                return default(T);
            }

            var data = JsonConvert.DeserializeObject<T>(File.ReadAllText(path, Encoding.UTF8), new SonarTypeConverter());
            return data;
        }

        private class RulesProfile
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("ruleKeys")]
            public List<string> RuleKeys { get; set; }
        }

        private class RuleParameter
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("defaultValue")]
            public string DefaultValue { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }
        }

        /// <summary>
        /// Custom converter to the protobuf issue Type enum
        /// </summary>
        internal class SonarTypeConverter : Newtonsoft.Json.JsonConverter
        {
            public override bool CanConvert(Type objectType) =>
                objectType == typeof(IssueType);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var serializedString = (string)reader.Value;

                // The names of the CodeSmell enum doesn't map directly to the serialized string so
                // we can't use the default JSON StringEnumSerializer
                if (serializedString.Equals("CODE_SMELL", StringComparison.OrdinalIgnoreCase))
                {
                    return IssueType.CodeSmell;
                }

                if (serializedString.Equals("SECURITY_HOTSPOT", StringComparison.OrdinalIgnoreCase))
                {
                    return IssueType.SecurityHotspot;
                }

                if (Enum.TryParse<IssueType>(serializedString, true /* ignore case */, out IssueType data))
                {
                    return data;
                }

                throw new JsonSerializationException($"Unrecognized IssueType value: {serializedString}");
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
