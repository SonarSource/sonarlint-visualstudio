/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.IO.Abstractions;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    internal interface IDefaultTsConfigPathProvider
    {
        string GetFilePath();
    }

    internal class DefaultTsConfigPathProvider : IDefaultTsConfigPathProvider
    {
        internal static readonly string DefaultTsConfigFilePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "tsconfig.json"));
        
        private readonly IFileSystem fileSystem;

        public DefaultTsConfigPathProvider()
            : this(new FileSystem())
        {
        }

        internal DefaultTsConfigPathProvider(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public string GetFilePath()
        {
            if (!fileSystem.File.Exists(DefaultTsConfigFilePath))
            {
                fileSystem.File.WriteAllText(DefaultTsConfigFilePath, GenerateDefaultTsConfig());
            }

            return DefaultTsConfigFilePath;
        }

        /// <summary>
        /// Returns a stringified json of a tsconfig with default parameters
        /// </summary>
        /// <remarks>
        /// Java-side code: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/sonar-javascript-plugin/src/main/java/org/sonar/plugins/javascript/eslint/JavaScriptEslintBasedSensor.java#L89
        /// </remarks>
        private string GenerateDefaultTsConfig()
        {
            var tsConfig = new TsConfig
            {
                Files = Array.Empty<string>(), // todo: what should be here?
                CompilerOptions = new Dictionary<string, object>
                {
                    {"allowJs", true},
                    {"noImplicitAny", true}
            }
            };

            return JsonConvert.SerializeObject(tsConfig, Formatting.Indented);
        }

        private class TsConfig
        {
            [JsonProperty("files")]
            public string[] Files { get; set; }

            [JsonProperty("compilerOptions")]
            public Dictionary<string, object> CompilerOptions { get; set; }
        }
    }
}
