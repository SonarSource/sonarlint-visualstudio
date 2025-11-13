/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.CFamily.CompilationDatabase;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

internal interface ICompilationDatabaseEntryGenerator
{
    CompilationDatabaseEntry CreateOrNull(string filePath);
}

[Export(typeof(ICompilationDatabaseEntryGenerator))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class CompilationDatabaseEntryGenerator : ICompilationDatabaseEntryGenerator
{
    private const string IncludeEntryName = "INCLUDE";
    private const string IsHeaderEntryName = "SONAR_CFAMILY_CAPTURE_PROPERTY_isHeaderFile";
    private readonly Lazy<ImmutableList<EnvironmentEntry>> staticEnvironmentVariableEntries;
    private readonly IFileConfigProvider fileConfigProvider;
    private readonly ILogger logger;

    [ImportingConstructor]
    public CompilationDatabaseEntryGenerator(IFileConfigProvider fileConfigProvider, IEnvironmentVariableProvider environmentVariableProvider, ILogger logger)
    {
        this.fileConfigProvider = fileConfigProvider;
        this.logger = logger.ForVerboseContext(nameof(CompilationDatabaseEntryGenerator));
        staticEnvironmentVariableEntries = new Lazy<ImmutableList<EnvironmentEntry>>(() => ImmutableList.CreateRange(environmentVariableProvider.GetAll().Select(x => new EnvironmentEntry(x.name, x.value))));
    }

    public CompilationDatabaseEntry CreateOrNull(string filePath)
    {
        if (fileConfigProvider.Get(filePath) is { } fileConfig)
        {
            return new CompilationDatabaseEntry
            {
                File = fileConfig.CDFile,
                Directory = fileConfig.CDDirectory,
                Command = fileConfig.CDCommand,
                Environment = GetEnvironmentEntries(fileConfig)
                    .Select(x => x.FormattedEntry)
            };
        }

        logger.LogVerbose(CFamilyStrings.CompilationDatabaseEntryGenerator_NotAVcxFile, filePath);
        return null;
    }

    private ImmutableList<EnvironmentEntry> GetEnvironmentEntries(IFileConfig fileConfig)
    {
        ImmutableList<EnvironmentEntry> environmentEntries = staticEnvironmentVariableEntries.Value;
        if (!string.IsNullOrEmpty(fileConfig.EnvInclude))
        {
            environmentEntries = UpdateEnvironmentWithEntry(fileConfig.CDFile, environmentEntries, new EnvironmentEntry(IncludeEntryName, fileConfig.EnvInclude));
        }
        if (fileConfig.IsHeaderFile)
        {
            environmentEntries = UpdateEnvironmentWithEntry(fileConfig.CDFile, environmentEntries, new EnvironmentEntry(IsHeaderEntryName, "true"));
        }
        return environmentEntries;
    }

    private ImmutableList<EnvironmentEntry> UpdateEnvironmentWithEntry(string fileName, ImmutableList<EnvironmentEntry> environmentEntries, EnvironmentEntry newEntry)
    {
        EnvironmentEntry oldEntry = environmentEntries.FirstOrDefault(x => x.Name == newEntry.Name);

        if (oldEntry.Name != null)
        {
            logger.LogVerbose(CFamilyStrings.CompilationDatabaseEntryGenerator_FilePropertyOverridesEnvironmentVariable, fileName, newEntry.Name, oldEntry.Value, newEntry.Value);
        }
        else
        {
            logger.LogVerbose(CFamilyStrings.CompilationDatabaseEntryGenerator_FilePropertyDefined, fileName, newEntry.Name, newEntry.Value);
        }
        return environmentEntries.RemoveAll(x => x.Name == newEntry.Name).Add(newEntry);
    }

    private readonly struct EnvironmentEntry(string name, string value)
    {
        public string Name { get; } = name;
        public string Value { get; } = value;
        public string FormattedEntry { get; } = $"{name}={value}";
    }
}
