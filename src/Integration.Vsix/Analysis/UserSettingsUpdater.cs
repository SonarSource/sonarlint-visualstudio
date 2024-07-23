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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Rules;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis;

public interface IUserSettingsUpdater
{
    /// <summary>
    /// Notification that one or more settings have changed
    /// </summary>
    event EventHandler SettingsChanged;

}

[Export(typeof(IUserSettingsUpdater))]
internal sealed class UserSettingsUpdater : IUserSettingsUpdater, IDisposable
{

    private readonly ISingleFileMonitor settingsFileMonitor;
    private readonly ISLCoreServiceProvider slCoreServiceProvider;
    private readonly ISLCoreRuleSettings slCoreRuleSettings;
    private readonly ILogger logger;
    private readonly IUserSettingsProvider userSettingsProvider;

    [ImportingConstructor]
    public UserSettingsUpdater(ILogger logger, 
        ISLCoreServiceProvider slCoreServiceProvider, 
        ISLCoreRuleSettings slCoreRuleSettings, 
        IUserSettingsProvider userSettingsProvider)
        : this(logger, new SingleFileMonitor(UserSettingsConstants.UserSettingsFilePath, logger), slCoreServiceProvider, slCoreRuleSettings, userSettingsProvider)
    {
    }

    internal /* for testing */ UserSettingsUpdater(ILogger logger, 
        ISingleFileMonitor settingsFileMonitor, 
        ISLCoreServiceProvider slCoreServiceProvider, 
        ISLCoreRuleSettings slCoreRuleSettings, 
        IUserSettingsProvider userSettingsProvider)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.userSettingsProvider = userSettingsProvider ?? throw new ArgumentNullException(nameof(userSettingsProvider));
        this.settingsFileMonitor = settingsFileMonitor ?? throw new ArgumentNullException(nameof(settingsFileMonitor));
        this.slCoreServiceProvider = slCoreServiceProvider;
        this.slCoreRuleSettings = slCoreRuleSettings;

        settingsFileMonitor.FileChanged += OnFileChanged;
    }

    private void OnFileChanged(object sender, EventArgs e)
    {
        userSettingsProvider.SafeLoadUserSettings();
        UpdateStandaloneRulesConfiguration();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    #region IUserSettingsUpdater implementation

    public event EventHandler SettingsChanged;

    #endregion

    private void UpdateStandaloneRulesConfiguration()
    {
        if (!slCoreServiceProvider.TryGetTransientService(out IRulesSLCoreService rulesSlCoreService))
        {
            logger.WriteLine($"[{nameof(UserSettingsUpdater)}] {SLCoreStrings.ServiceProviderNotInitialized}");
            return;
        }

        try
        {
            rulesSlCoreService.UpdateStandaloneRulesConfiguration(new UpdateStandaloneRulesConfigurationParams(slCoreRuleSettings.RulesSettings));
        }
        catch (Exception e)
        {
            logger.WriteLine(e.ToString());
        }
    }

    public void Dispose()
    {
        settingsFileMonitor.FileChanged -= OnFileChanged;
        settingsFileMonitor.Dispose();
    }
}
