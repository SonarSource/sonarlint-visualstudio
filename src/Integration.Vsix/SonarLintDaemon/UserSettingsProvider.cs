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
    }

    [Export(typeof(IUserSettingsProvider))]
    internal class UserSettingsProvider : IUserSettingsProvider
    {
        // Note: the data is stored in the roaming profile so it will be sync across machines
        // for domain-joined users.
        public static readonly string UserSettingsFilePath = Path.GetFullPath(
            Path.Combine(Environment.GetEnvironmentVariable("APPDATA"), "SonarLint for Visual Studio", "settings.json"));

        private readonly IFile fileSystem;
        private readonly ILogger logger;
        private readonly string userSettingsFilePath;

        [ImportingConstructor]
        public UserSettingsProvider(ILogger logger)
            : this(UserSettingsProvider.UserSettingsFilePath, logger, new FileWrapper())
        {
        }

        internal /* for testing */ UserSettingsProvider(string userSettingsFilePath, ILogger logger, IFile fileWrapper)
        {
            this.userSettingsFilePath = userSettingsFilePath ?? throw new ArgumentNullException(nameof(userSettingsFilePath));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileSystem = fileWrapper ?? throw new ArgumentNullException(nameof(fileWrapper));

            UserSettings = SafeLoadUserSettings();
        }

        #region IUserSettingsProvider implementation

        public UserSettings UserSettings { get; private set; }

        public event EventHandler SettingsChanged;

        #endregion

        private UserSettings SafeLoadUserSettings()
        {
            UserSettings userSettings = null;

            if (!fileSystem.Exists(userSettingsFilePath))
            {
                logger.WriteLine(DaemonStrings.Settings_NoUserSettings, userSettingsFilePath);
            }
            else
            {
                try
                {
                    logger.WriteLine(DaemonStrings.Settings_LoadedUserSettings, userSettingsFilePath);
                    var data = fileSystem.ReadAllText(userSettingsFilePath);
                    userSettings = JsonConvert.DeserializeObject<UserSettings>(data);
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger.WriteLine(DaemonStrings.Settings_ErrorLoadingSettings, userSettingsFilePath, ex.Message);
                }
            }
            return userSettings ?? new UserSettings();
        }
    }
}
