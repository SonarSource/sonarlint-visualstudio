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
    private readonly ImmutableList<EnvironmentEntry> staticEnvironmentVariableEntries;
    private readonly IVCXCompilationDatabaseStorage storage;
    private readonly IFileConfigProvider fileConfigProvider;

    [method: ImportingConstructor]
    public VCXCompilationDatabaseProvider(
        IVCXCompilationDatabaseStorage storage,
        IEnvironmentVariableProvider environmentVariableProvider,
        IFileConfigProvider fileConfigProvider)
    {
        this.storage = storage;
        this.fileConfigProvider = fileConfigProvider;
        staticEnvironmentVariableEntries = ImmutableList.CreateRange(environmentVariableProvider.GetAll().Select(x => new EnvironmentEntry(x.name, x.value)));
    }

    public ICompilationDatabaseHandle CreateOrNull(string filePath) =>
        fileConfigProvider.Get(filePath, null) is { } fileConfig
            ? storage.CreateDatabase(fileConfig.CDFile, fileConfig.CDDirectory, fileConfig.CDCommand, GetEnvironmentEntries(fileConfig.EnvInclude).Select(x => x.FormattedEntry))
            : null;

    private ImmutableList<EnvironmentEntry> GetEnvironmentEntries(string fileConfigEnvInclude) =>
        string.IsNullOrEmpty(fileConfigEnvInclude)
            ? staticEnvironmentVariableEntries
            : staticEnvironmentVariableEntries
                .RemoveAll(x => x.Name == IncludeEntryName)
                .Add(new EnvironmentEntry(IncludeEntryName, fileConfigEnvInclude));

    private readonly struct EnvironmentEntry(string name, string value)
    {
        public string Name { get; } = name;
        public string FormattedEntry { get; } = $"{name}={value}";
    }
}
