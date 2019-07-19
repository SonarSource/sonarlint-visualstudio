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
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{

    internal class RulesLoader
    {
        private const string CFamilyFiles = ".CFamilyEmbedded";

        public static List<string> ReadRulesList()
        {
            var extensionFolder = Path.GetDirectoryName(typeof(RulesLoader).Assembly.Location);
            string path = Path.Combine(extensionFolder, CFamilyFiles, "RulesList.json");
            using (StreamReader r = new StreamReader(path, Encoding.UTF8))
            {
                string json = r.ReadToEnd();
                dynamic array = JsonConvert.DeserializeObject(json);
                List<string> result = new List<string>();
                foreach (var item in array)
                {
                    result.Add(item.Value);
                }
                return result;
            }
        }

        public static List<string> ReadActiveRulesList()
        {
            var extensionFolder = Path.GetDirectoryName(typeof(RulesLoader).Assembly.Location);
            string path = Path.Combine(extensionFolder, CFamilyFiles, "Sonar_way_profile.json");
            using (StreamReader r = new StreamReader(path, Encoding.UTF8))
            {
                string json = r.ReadToEnd();
                dynamic profile = JsonConvert.DeserializeObject(json);
                List<string> result = new List<string>();
                foreach (var item in profile.ruleKeys)
                {
                    result.Add(item.Value);
                }
                return result;
            }
        }

        public static Dictionary<string, string> ReadRuleParams(String ruleKey)
        {
            var extensionFolder = Path.GetDirectoryName(typeof(RulesLoader).Assembly.Location);
            string path = Path.Combine(extensionFolder, CFamilyFiles, ruleKey + "_params.json");
            if (!File.Exists(path))
            {
                return new Dictionary<string, string>();
            }
            using (StreamReader r = new StreamReader(path, Encoding.UTF8))
            {
                string json = r.ReadToEnd();
                dynamic array = JsonConvert.DeserializeObject(json);
                Dictionary<string, string> result = new Dictionary<string, string>();
                foreach (var param in array)
                {
                    result.Add(param.key.Value, param.defaultValue.Value);
                }
                return result;
            }
        }
    }
}
