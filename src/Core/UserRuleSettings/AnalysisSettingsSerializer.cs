﻿/*
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

using System.IO.Abstractions;
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Core.UserRuleSettings
{
    public interface IAnalysisSettingsSerializer
    {
        AnalysisSettings SafeLoad(string filePath);
        void SafeSave(string filePath, AnalysisSettings data);
    }

    /// <summary>
    /// Loads and saves rules settings to disc.
    /// Logs user-friendly messages and suppresses non-critical exceptions.
    /// </summary>
    public class AnalysisSettingsSerializer : IAnalysisSettingsSerializer
    {
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;

        public AnalysisSettingsSerializer(IFileSystem fileSystem, ILogger logger)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public AnalysisSettings SafeLoad(string filePath)
        {
            AnalysisSettings settings = null;
            if (!fileSystem.File.Exists(filePath))
            {
                logger?.WriteLine(CoreStrings.Settings_NoSettingsFile, filePath);
            }
            else
            {
                try
                {
                    logger?.WriteLine(CoreStrings.Settings_LoadedSettingsFile, filePath);
                    var data = fileSystem.File.ReadAllText(filePath);
                    settings = JsonConvert.DeserializeObject<AnalysisSettings>(data);
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger?.WriteLine(CoreStrings.Settings_ErrorLoadingSettingsFile, filePath, ex.Message);
                }
            }
            return settings;
        }

        public void SafeSave(string filePath, AnalysisSettings data)
        {
            try
            {
                string dataAsText = JsonConvert.SerializeObject(data, Formatting.Indented);
                fileSystem.File.WriteAllText(filePath, dataAsText);
                logger?.WriteLine(CoreStrings.Settings_SavedSettingsFile, filePath);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger?.WriteLine(CoreStrings.Settings_ErrorSavingSettingsFile, filePath, ex.Message);
            }
        }
    }
}
