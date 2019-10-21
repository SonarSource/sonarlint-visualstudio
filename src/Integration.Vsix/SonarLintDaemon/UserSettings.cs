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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SonarLint.VisualStudio.Integration.Vsix
{

    /*
     // Example config file - same format as the VS Code settings.json file
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
            "level": "on"
        },
    }
...
}
     */

    // Json-serializable data class
    internal class UserSettings
    {
        [JsonProperty("sonarlint.rules")]
        public Dictionary<string, RuleConfig> Rules { get; set; } = new Dictionary<string, RuleConfig>();
    }

    internal class RuleConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RuleLevel Level { get; set; }
    }

    internal enum RuleLevel
    {
        On,
        Off
    }
}
