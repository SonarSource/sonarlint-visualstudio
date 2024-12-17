/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

[Export(typeof(IVCXCompilationDatabaseProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class VCXCompilationDatabaseProvider : IVCXCompilationDatabaseProvider
{
    private const string IncludeEntryName = "INCLUDE";
    private const string IsHeaderEntryName = "SONAR_CFAMILY_CAPTURE_PROPERTY_isHeaderFile";
    private readonly ImmutableList<EnvironmentEntry> staticEnvironmentVariableEntries;
    private readonly IVCXCompilationDatabaseStorage storage;
    private readonly IFileConfigProvider fileConfigProvider;
    private readonly ILogger logger;

    [method: ImportingConstructor]
    public VCXCompilationDatabaseProvider(
        IVCXCompilationDatabaseStorage storage,
        IEnvironmentVariableProvider environmentVariableProvider,
        IFileConfigProvider fileConfigProvider,
        ILogger logger)
    {
        this.storage = storage;
        this.fileConfigProvider = fileConfigProvider;
        this.logger = logger;
        staticEnvironmentVariableEntries = ImmutableList.CreateRange(environmentVariableProvider.GetAll().Select(x => new EnvironmentEntry(x.name, x.value)));
    }

    public ICompilationDatabaseHandle CreateOrNull(string filePath) =>
        fileConfigProvider.Get(filePath, null) is { } fileConfig
            ? storage.CreateDatabase(fileConfig.CDFile, fileConfig.CDDirectory, fileConfig.CDCommand, GetEnvironmentEntries(fileConfig).Select(x => x.FormattedEntry))
            : null;

    private ImmutableList<EnvironmentEntry> GetEnvironmentEntries(IFileConfig fileConfig)
    {
        ImmutableList<EnvironmentEntry> environmentEntries = staticEnvironmentVariableEntries;
        if (!string.IsNullOrEmpty(fileConfig.EnvInclude))
        {
            environmentEntries = UpdateEnvironmentWithEntry(environmentEntries, new EnvironmentEntry(IncludeEntryName, fileConfig.EnvInclude));
        }
        if (fileConfig.IsHeaderFile)
        {
            environmentEntries = UpdateEnvironmentWithEntry(environmentEntries, new EnvironmentEntry(IsHeaderEntryName, "true"));
        }
        return environmentEntries;
    }

    private ImmutableList<EnvironmentEntry> UpdateEnvironmentWithEntry(ImmutableList<EnvironmentEntry> environmentEntries, EnvironmentEntry newEntry)
    {
        EnvironmentEntry oldEntry = environmentEntries.FirstOrDefault(x => x.Name == newEntry.Name);

        if (oldEntry.Name != null)
        {
            logger.LogVerbose($"[VCXCompilationDatabaseProvider] Overwriting the value of environment variable \"{newEntry.Name}\". Old value: \"{oldEntry.Value}\", new value: \"{newEntry.Value}\"");
        }
        else
        {
            logger.LogVerbose($"[VCXCompilationDatabaseProvider] Setting environment variable \"{newEntry.Name}\". Value: \"{newEntry.Value}\"");
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
