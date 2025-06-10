/*
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
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.FileMonitor;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;

namespace SonarLint.VisualStudio.Integration.UserSettingsConfiguration;

[Export(typeof(IGlobalSettingsStorage))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class GlobalSettingsStorage : IGlobalSettingsStorage
{
    private const string SettingsFileName = "settings.json";
    private const string GeneratedGlobalSettingsFolderName = ".global";
    private readonly IEnvironmentVariableProvider environmentVariableProvider;
    private readonly ISingleFileMonitorFactory fileMonitorFactory;
    private readonly IFileSystem fileSystem;
    private readonly IAnalysisSettingsSerializer serializer;

    private bool disposed;
    private (string settingsFile, string generatedConfigsBaseDirectory) globalFilePaths;
    private ISingleFileMonitor globalSettingsFileMonitor;

    public string GlobalAnalysisSettingsFilePath => globalFilePaths.settingsFile;

    [ImportingConstructor]
    public GlobalSettingsStorage(
        ISingleFileMonitorFactory singleFileMonitorFactory,
        IFileSystemService fileSystem,
        IEnvironmentVariableProvider environmentVariableProvider,
        IAnalysisSettingsSerializer serializer,
        IInitializationProcessorFactory processorFactory)
    {
        this.fileSystem = fileSystem;
        this.environmentVariableProvider = environmentVariableProvider;
        this.serializer = serializer;
        fileMonitorFactory = singleFileMonitorFactory;
        InitializationProcessor = processorFactory.CreateAndStart<GlobalSettingsStorage>(
            [],
            () =>
            {
                if (disposed)
                {
                    return;
                }

                CreateGlobalSettingsMonitorAndSubscribe();
            });
    }

    public IInitializationProcessor InitializationProcessor { get; }
    public event EventHandler SettingsFileChanged;
    public string SettingsFilePath => globalFilePaths.settingsFile;
    public string ConfigurationBaseDirectory => globalFilePaths.generatedConfigsBaseDirectory;

    public void SaveSettingsFile(GlobalRawAnalysisSettings settings) => serializer.SafeSave(SettingsFilePath, settings);

    public GlobalRawAnalysisSettings LoadSettingsFile() => serializer.SafeLoad<GlobalRawAnalysisSettings>(SettingsFilePath);

    public void Dispose()
    {
        if (!disposed)
        {
            if (InitializationProcessor.IsFinalized)
            {
                globalSettingsFileMonitor.FileChanged -= OnFileChanged;
                globalSettingsFileMonitor.Dispose();
            }
            disposed = true;
        }
    }

    private void OnFileChanged(object sender, EventArgs e) => SettingsFileChanged?.Invoke(this, EventArgs.Empty);

    private void CreateGlobalSettingsMonitorAndSubscribe()
    {
        // Note: the data is stored in the roaming profile so it will be sync across machines for domain-joined users.
        var appDataRoot = environmentVariableProvider.GetSLVSAppDataRootPath();
        var globalAnalysisSettingsFilePath = Path.GetFullPath(Path.Combine(appDataRoot, SettingsFileName));
        var generatedGlobalSettingsFolder = Path.Combine(appDataRoot, GeneratedGlobalSettingsFolderName);
        globalFilePaths = (globalAnalysisSettingsFilePath, generatedGlobalSettingsFolder);
        globalSettingsFileMonitor = fileMonitorFactory.Create(GlobalAnalysisSettingsFilePath);
        EnsureSettingsFileExists();
        globalSettingsFileMonitor.FileChanged += OnFileChanged;
    }

    private void EnsureSettingsFileExists()
    {
        if (!fileSystem.File.Exists(GlobalAnalysisSettingsFilePath))
        {
            serializer.SafeSave(GlobalAnalysisSettingsFilePath, new GlobalRawAnalysisSettings());
        }
    }
}
