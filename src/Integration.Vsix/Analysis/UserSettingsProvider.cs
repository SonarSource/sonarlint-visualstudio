/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    [Export(typeof(IUserSettingsProvider))]
    internal sealed class UserSettingsProvider : IUserSettingsProvider, IDisposable
    {
        // Note: the data is stored in the roaming profile so it will be sync across machines
        // for domain-joined users.
        public static readonly string UserSettingsFilePath = Path.GetFullPath(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SonarLint for Visual Studio", "settings.json"));

        private readonly ISingleFileMonitor settingsFileMonitor;

        private readonly IFile fileWrapper;
        private readonly ILogger logger;
        private readonly UserSettingsSerializer serializer;

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

            this.serializer = new UserSettingsSerializer(fileWrapper, logger);

            SettingsFilePath = settingsFileMonitor.MonitoredFilePath;
            UserSettings = SafeLoadUserSettings(SettingsFilePath, logger);
            settingsFileMonitor.FileChanged += OnFileChanged;
        }

        private void OnFileChanged(object sender, EventArgs e)
        {
            UserSettings = SafeLoadUserSettings(SettingsFilePath, logger);
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

            serializer.SafeSave(SettingsFilePath, UserSettings);
        }

        public string SettingsFilePath { get; }

        public void EnsureFileExists()
        {
            if (!fileWrapper.Exists(SettingsFilePath))
            {
                serializer.SafeSave(SettingsFilePath, UserSettings);
            }
        }

        #endregion

        private UserSettings SafeLoadUserSettings(string filePath, ILogger logger)
        {
            var settings = serializer.SafeLoad(filePath);
            if (settings == null)
            {
                logger.WriteLine(AnalysisStrings.Settings_UsingDefaultSettings);
                settings = new UserSettings();
            }
            return settings;
        }

        public void Dispose()
        {
            settingsFileMonitor.FileChanged -= OnFileChanged;
            settingsFileMonitor.Dispose();
        }
    }
}
