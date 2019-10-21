/*
 * SonarLint for Visual Studio
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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal interface IUserSettingsProvider
    {
        event EventHandler SettingsChanged;

        UserSettings UserSettings { get; }
        void DisableRule(string ruleId);
    }

    [Export(typeof(IUserSettingsProvider))]
    internal class UserSettingsProvider : IUserSettingsProvider
    {
        // Note: the data is stored in the roaming profile so it will be sync across machines
        // for domain-joined users.
        public static readonly string UserSettingsFilePath = Path.GetFullPath(
            Path.Combine(Environment.GetEnvironmentVariable("APPDATA"), "SonarLint for Visual Studio", "settings.json"));

        private readonly ISingleFileMonitor settingsFileMonitor;

        private readonly IFile fileWrapper;
        private readonly ILogger logger;

        [ImportingConstructor]
        public UserSettingsProvider(ILogger logger)
            : this(logger, new FileWrapper(),
                  new SingleFileMonitor(UserSettingsFilePath, logger))
        {
        }

        internal /* for testing */ UserSettingsProvider(ILogger logger,
            IFile fileWrapper, ISingleFileMonitor settingsFileMonitor)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileWrapper = fileWrapper ?? throw new ArgumentNullException(nameof(fileWrapper));
            this.settingsFileMonitor = settingsFileMonitor ?? throw new ArgumentNullException(nameof(settingsFileMonitor));

            UserSettings = SafeLoadUserSettings(settingsFileMonitor.MonitoredFilePath, fileWrapper, logger);
            settingsFileMonitor.FileChanged += OnFileChanged;
        }

        private void OnFileChanged(object sender, EventArgs e)
        {
            UserSettings = SafeLoadUserSettings(settingsFileMonitor.MonitoredFilePath, fileWrapper, logger);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        #region IUserSettingsProvider implementation

        public UserSettings UserSettings { get; private set; }

        public event EventHandler SettingsChanged;

        public void DisableRule(string ruleId)
        {
            Debug.Assert(!string.IsNullOrEmpty(ruleId), "DisableRule: ruleId should not be null/empty");

            if (UserSettings.Rules.TryGetValue(ruleId, out var ruleConfig))
            {
                ruleConfig.Level = RuleLevel.Off;
            }
            else
            {
                UserSettings.Rules[ruleId] = new RuleConfig { Level = RuleLevel.Off };
            }

            SafeSaveUserSettings(settingsFileMonitor.MonitoredFilePath, UserSettings, fileWrapper, logger);
        }

        #endregion

        internal /* for testing */ static UserSettings SafeLoadUserSettings(string filePath, IFile fileWrapper, ILogger logger)
        {
            UserSettings userSettings = null;
            if (!fileWrapper.Exists(filePath))
            {
                logger.WriteLine(DaemonStrings.Settings_NoUserSettings, filePath);
            }
            else
            {
                try
                {
                    logger.WriteLine(DaemonStrings.Settings_LoadedUserSettings, filePath);
                    var data = fileWrapper.ReadAllText(filePath);
                    userSettings = JsonConvert.DeserializeObject<UserSettings>(data);
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger.WriteLine(DaemonStrings.Settings_ErrorLoadingSettings, filePath, ex.Message);
                }
            }
            return userSettings ?? new UserSettings();
        }

        internal /* for testing */ static void SafeSaveUserSettings(string filePath, UserSettings data, IFile fileWrapper, ILogger logger)
        {
            try
            {
                string dataAsText = JsonConvert.SerializeObject(data, Formatting.Indented);
                fileWrapper.WriteAllText(filePath, dataAsText);
                logger.WriteLine(DaemonStrings.Settings_SavedUserSettings, filePath);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(DaemonStrings.Settings_ErrorSavingSettings, filePath, ex.Message);
            }
        }
    }
}
