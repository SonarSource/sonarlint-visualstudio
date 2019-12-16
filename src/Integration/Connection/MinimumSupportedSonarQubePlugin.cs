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

using System.Collections.Generic;
using System.Linq;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Connection
{
    internal class MinimumSupportedSonarQubePlugin
    {
        public static readonly MinimumSupportedSonarQubePlugin CSharp = new MinimumSupportedSonarQubePlugin("csharp", "SonarC#", "5.0", Language.CSharp);
        public static readonly MinimumSupportedSonarQubePlugin VbNet = new MinimumSupportedSonarQubePlugin("vbnet", "SonarVB", "3.0", Language.VBNET);
        
        // Note: there is no specific technical reason for the choice of cpp v6.0 as the minimum supported version.
        // Howver, that was the first version that uses CLang so it's close to the version embedded in SLVS
        // i.e. the analysis rules implementations should be similar so the issues shown in the IDE should be
        // similar to those reported on the server.
        public static readonly MinimumSupportedSonarQubePlugin CFamily = new MinimumSupportedSonarQubePlugin("cpp", "SonarCFamily", "6.0", Language.Cpp, Language.C);
        public static readonly IEnumerable<MinimumSupportedSonarQubePlugin> All = new[] { CSharp, VbNet, CFamily };

        private MinimumSupportedSonarQubePlugin(string key, string pluginName, string minimumVersion, params Language[] languages)
        {
            Key = key;
            PluginName = pluginName; // Note: plugin names are not localized - they are fixed
            MinimumVersion = minimumVersion;
            Languages = languages.ToArray();
        }

        public string Key { get; }
        public string PluginName{ get; }
        public string MinimumVersion { get; }
        public IEnumerable<Language> Languages { get; }
    }
}
