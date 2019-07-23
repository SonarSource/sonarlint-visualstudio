/*
 * SonarLint for Visual Studio
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal class RulesLoader
    {
        private static readonly string CFamilyFilesDirectory = Path.Combine(
            Path.GetDirectoryName(typeof(RulesLoader).Assembly.Location),
            ".CFamilyEmbedded");

        public static List<string> ReadRulesList()
        {
            var rulesList = LoadCFamilyJsonFile<List<string>>("RulesList.json");
            Debug.Assert(rulesList != null, "The CFamily RulesList.json should exist and not be empty");

            return rulesList;
        }

        public static List<string> ReadActiveRulesList()
        {
            var rulesProfile = LoadCFamilyJsonFile<RulesProfile>("Sonar_way_profile.json");
            Debug.Assert(rulesProfile != null, "The CFamily Sonar_way_profile.json should exist and not be empty");

            return rulesProfile.RuleKeys;
        }

        public static Dictionary<string, string> ReadRuleParams(String ruleKey)
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

        private static T LoadCFamilyJsonFile<T>(string fileName) where T: class
        {
            string path = Path.Combine(CFamilyFilesDirectory, fileName);
            if (!File.Exists(path))
            {
                return default(T);
            }

            var data = JsonConvert.DeserializeObject<T>(File.ReadAllText(path, Encoding.UTF8));
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
    }
}
