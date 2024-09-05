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

using System.Windows.Input;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Progress.MVVM;

namespace SonarLint.VisualStudio.Integration.Vsix.Settings;

public class GeneralOptionsDialogControlViewModel : ViewModelBase
{
    private string jreLocation;
    private DaemonLogLevel selectedDaemonLogLevel;
    private bool isActivateMoreEnabled;
    private readonly ISonarLintSettings slSettings;

    public ICommand OpenSettingsFileCommand { get; }
    public ICommand ShowWikiCommand { get; }
    public IEnumerable<DaemonLogLevel> DaemonLogLevels { get; } = Enum.GetValues(typeof(DaemonLogLevel)).Cast<DaemonLogLevel>();

    public string JreLocation
    {
        get => jreLocation;
        set
        {
            jreLocation = value;
            RaisePropertyChanged();
        }
    }

    public DaemonLogLevel SelectedDaemonLogLevel
    {
        get => selectedDaemonLogLevel;
        set
        {
            selectedDaemonLogLevel = value;
            RaisePropertyChanged();
        }
    }

    public bool IsActivateMoreEnabled
    {
        get => isActivateMoreEnabled;
        set
        {
            isActivateMoreEnabled = value;
            RaisePropertyChanged();
        }
    }

    public GeneralOptionsDialogControlViewModel(ISonarLintSettings slSettings, IBrowserService browserService, ICommand openSettingsFileCommand)
    {
        OpenSettingsFileCommand = openSettingsFileCommand ?? throw new ArgumentNullException(nameof(openSettingsFileCommand));
        ShowWikiCommand = CreateShowWikiCommand(browserService);

        this.slSettings = slSettings;
        SelectedDaemonLogLevel = slSettings.DaemonLogLevel;
        IsActivateMoreEnabled = slSettings.IsActivateMoreEnabled;
        JreLocation = slSettings.JreLocation;
    }

    public void SaveSettings()
    {
        slSettings.DaemonLogLevel = SelectedDaemonLogLevel;
        slSettings.IsActivateMoreEnabled = IsActivateMoreEnabled;
        slSettings.JreLocation = JreLocation?.Trim();
    }

    private static RelayCommand CreateShowWikiCommand(IBrowserService browserService)
    {
        if (browserService == null)
        {
            throw new ArgumentNullException(nameof(browserService));
        }
        return new RelayCommand(() => browserService.Navigate(DocumentationLinks.DisablingARule));
    }
}
