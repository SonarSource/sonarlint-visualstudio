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

namespace SonarQube.Client.Models
{
    public class SonarQubeLanguage
    {
        public static readonly SonarQubeLanguage CSharp = new SonarQubeLanguage("cs", "C#", "SonarC#");
        public static readonly SonarQubeLanguage VbNet = new SonarQubeLanguage("vbnet", "VB.NET", "SonarVB");
        public static readonly SonarQubeLanguage Cpp = new SonarQubeLanguage("cpp", "C++", "SonarCFamily");
        public static readonly SonarQubeLanguage C = new SonarQubeLanguage("c", "C", "SonarCFamily");

        public string Key { get; }

        public string Name { get; }

        public string PluginName { get; }

        public SonarQubeLanguage(string key, string name)
            : this(key, name, string.Empty)
        {
        }

        private SonarQubeLanguage(string key, string name, string pluginName)
        {
            Key = key;
            Name = name;
            PluginName = pluginName;
        }
    }
}
