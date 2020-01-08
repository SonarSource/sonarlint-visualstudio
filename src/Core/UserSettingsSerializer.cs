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
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// Loads and saves settings files to disc.
    /// Logs user-friendly messages and suppresses non-critical exceptions.
    /// </summary>
    public class UserSettingsSerializer
    {
        private readonly IFile fileWrapper;
        private readonly ILogger logger;

        public UserSettingsSerializer(IFile fileWrapper, ILogger logger)
        {
            this.fileWrapper = fileWrapper ?? throw new ArgumentNullException(nameof(fileWrapper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public UserSettings SafeLoad(string filePath)
        {
            UserSettings userSettings = null;
            if (!fileWrapper.Exists(filePath))
            {
                logger?.WriteLine(CoreStrings.Settings_NoSettingsFile, filePath);
            }
            else
            {
                try
                {
                    logger?.WriteLine(CoreStrings.Settings_LoadedSettingsFile, filePath);
                    var data = fileWrapper.ReadAllText(filePath);
                    userSettings = JsonConvert.DeserializeObject<UserSettings>(data);
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger?.WriteLine(CoreStrings.Settings_ErrorLoadingSettingsFile, filePath, ex.Message);
                }
            }
            return userSettings;
        }

        public void SafeSave(string filePath, UserSettings data)
        {
            try
            {
                string dataAsText = JsonConvert.SerializeObject(data, Formatting.Indented);
                fileWrapper.WriteAllText(filePath, dataAsText);
                logger?.WriteLine(CoreStrings.Settings_SavedSettingsFile, filePath);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger?.WriteLine(CoreStrings.Settings_ErrorSavingSettingsFile, filePath, ex.Message);
            }
        }
    }
}
