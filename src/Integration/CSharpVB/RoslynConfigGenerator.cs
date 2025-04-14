﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using System.IO;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.Integration.CSharpVB;

[Export(typeof(IRoslynConfigGenerator))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynConfigGenerator(
    IFileSystemService fileSystem,
    IGlobalConfigGenerator globalConfigGenerator,
    ISonarLintConfigGenerator sonarLintConfigGenerator,
    ISonarLintConfigurationXmlSerializer  sonarLintConfigurationXmlSerializer)
    : IRoslynConfigGenerator
{
    /// <summary>
    /// internal sonar-dotnet format used to provide rule parameters and file exclusions to the analyzer
    /// </summary>
    private const string SonarlintConfigFileName = "SonarLint.xml";

    public void GenerateAndSaveConfiguration(
        Language language,
        string baseDirectory,
        IDictionary<string, string> properties,
        IFileExclusions fileExclusions,
        IReadOnlyCollection<IRoslynRuleStatus> ruleStatuses,
        IReadOnlyCollection<IRuleParameters> ruleParameters)
    {
        var roslynGlobalConfig = globalConfigGenerator.Generate(ruleStatuses);
        Save(roslynGlobalConfig, Path.Combine(baseDirectory, language.SettingsFileNameAndExtension));
        var sonarLintConfiguration = sonarLintConfigGenerator.Generate(ruleParameters, properties, fileExclusions, language);
        Save(sonarLintConfigurationXmlSerializer.Serialize(sonarLintConfiguration), Path.Combine(baseDirectory, language.Id, SonarlintConfigFileName));
    }

    private void Save(string config, string filePath)
    {
        EnsureParentDirectoryExists(filePath);
        fileSystem.File.WriteAllText(filePath, config);
    }

    private void EnsureParentDirectoryExists(string filePath)
    {
        var parentDirectory = Path.GetDirectoryName(filePath);
        fileSystem.Directory.CreateDirectory(parentDirectory); // will no-op if exists
    }
}
