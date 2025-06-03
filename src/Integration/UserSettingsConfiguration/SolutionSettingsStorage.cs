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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.FileMonitor;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;

namespace SonarLint.VisualStudio.Integration.UserSettingsConfiguration;

[Export(typeof(ISolutionSettingsStorage))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class SolutionSettingsStorage : ISolutionSettingsStorage
{
    private const string SolutionSettingsFolderName = "SolutionSettings";
    private const string SettingsFileName = "settings.json";
    private readonly IActiveSolutionTracker activeSolutionTracker;
    private readonly ISingleFileMonitorFactory fileMonitorFactory;
    private readonly IAnalysisSettingsSerializer serializer;

    private string appDataRoot;
    private bool disposed;
    private (string settingsFile, string generatedConfigsBaseDirectory)? solutionFilePaths;
    private ISingleFileMonitor fileMonitor;

    [ImportingConstructor]
    public SolutionSettingsStorage(
        IActiveSolutionTracker activeSolutionTracker,
        ISingleFileMonitorFactory singleFileMonitorFactory,
        IEnvironmentVariableProvider environmentVariableProvider,
        IAnalysisSettingsSerializer serializer,
        IInitializationProcessorFactory processorFactory)
    {
        this.activeSolutionTracker = activeSolutionTracker;
        this.serializer = serializer;
        fileMonitorFactory = singleFileMonitorFactory;
        InitializationProcessor = processorFactory.CreateAndStart<SolutionSettingsStorage>(
            [activeSolutionTracker],
            () =>
            {
                if (disposed)
                {
                    return;
                }

                // Note: the data is stored in the roaming profile so it will be sync across machines for domain-joined users.
                appDataRoot = environmentVariableProvider.GetSLVSAppDataRootPath();
                CreateSolutionSettingsMonitorAndSubscribe(activeSolutionTracker.CurrentSolutionName);
                activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTrackerOnActiveSolutionChanged;
            });
    }

    public IInitializationProcessor InitializationProcessor { get; }
    public string SettingsFilePath => solutionFilePaths?.settingsFile;
    public string ConfigurationBaseDirectory => solutionFilePaths?.generatedConfigsBaseDirectory;
    public event EventHandler SettingsFileChanged;

    public void SaveSettingsFile(SolutionRawAnalysisSettings settings) => serializer.SafeSave(SettingsFilePath, settings);

    public SolutionRawAnalysisSettings LoadSettingsFile() => serializer.SafeLoad<SolutionRawAnalysisSettings>(SettingsFilePath);

    public void Dispose()
    {
        if (!disposed)
        {
            if (InitializationProcessor.IsFinalized)
            {
                DisposeSettingsMonitor();
                activeSolutionTracker.ActiveSolutionChanged -= ActiveSolutionTrackerOnActiveSolutionChanged;
            }
            disposed = true;
        }
    }

    private void ActiveSolutionTrackerOnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
    {
        DisposeSettingsMonitor();

        if (!e.IsSolutionOpen || e.SolutionName == null)
        {
            return;
        }

        CreateSolutionSettingsMonitorAndSubscribe(e.SolutionName);
        RaiseSettingsFileChanged();
    }

    private void CreateSolutionSettingsMonitorAndSubscribe(string solutionName)
    {
        if (solutionName == null)
        {
            return;
        }
        var solutionSettingsParentFolder = Path.Combine(appDataRoot, SolutionSettingsFolderName, solutionName);
        var solutionSettingsFilePath = Path.GetFullPath(Path.Combine(solutionSettingsParentFolder, SettingsFileName));
        solutionFilePaths = (solutionSettingsFilePath, solutionSettingsParentFolder);
        fileMonitor = fileMonitorFactory.Create(SettingsFilePath);
        fileMonitor.FileChanged += OnFileChanged;
    }

    private void OnFileChanged(object sender, EventArgs e) => RaiseSettingsFileChanged();

    private void RaiseSettingsFileChanged() => SettingsFileChanged?.Invoke(this, EventArgs.Empty);

    private void DisposeSettingsMonitor()
    {
        if (fileMonitor == null)
        {
            return;
        }

        solutionFilePaths = null;
        fileMonitor.FileChanged -= OnFileChanged;
        fileMonitor.Dispose();
    }
}
