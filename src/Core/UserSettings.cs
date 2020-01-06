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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SonarLint.VisualStudio.Core
{

    /*
     // Example config file - same format as the VS Code settings.json file, with the addition of "parameters"
     // and "severity", both of which are optional.
{
...
    "sonarlint.rules": {
        "typescript:S2685": {
            "level": "on"
        },
        "javascript:EqEqEq": {
            "level": "on"
        },

        "cpp:S967": {
            "level": "off"
        },
        "c:CommentedCode": {
            "level": "on",
            "Parameters": {
              "key1": "value1",
              "key2": "value2"
            },
            "severity": "Critical"
        },
    }
...
}
     */

    // Json-serializable data class
    public class UserSettings
    {
        [JsonProperty("sonarlint.rules", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public Dictionary<string, RuleConfig> Rules { get; set; } = new Dictionary<string, RuleConfig>(StringComparer.OrdinalIgnoreCase);
    }

    public class RuleConfig
    {
        [JsonProperty("level")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RuleLevel Level { get; set; }

        // Note: property will be null if "parameters" is missing from the file.
        // This is what we want: most rules won't have parameters and we want to avoid
        // creating hundreds of unnecessary empty dictionaries.
        // The only downside is that the dictionary that is created will use the default
        // comparer, which is case-sensitive.
        [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Parameters { get; set; }

        [JsonProperty("severity", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public IssueSeverity? Severity { get; set; }
    }

    public enum RuleLevel
    {
        On,
        Off
    }

    public enum IssueSeverity
    {
        Blocker = 0,
        Critical = 1,
        Major = 2,
        Minor = 3,
        Info = 4,
    }
}
