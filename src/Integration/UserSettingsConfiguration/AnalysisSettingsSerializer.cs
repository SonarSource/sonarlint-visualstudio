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
using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Integration.UserSettingsConfiguration;

/// <summary>
/// Loads and saves rules settings to disc.
/// Logs user-friendly messages and suppresses non-critical exceptions.
/// </summary>
[Export(typeof(IAnalysisSettingsSerializer))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class AnalysisSettingsSerializer(IFileSystemService fileSystem, ILogger logger) : IAnalysisSettingsSerializer
{
    private readonly IFileSystem fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public T SafeLoad<T>(string filePath) where T : class
    {
        if (!fileSystem.File.Exists(filePath))
        {
            logger?.WriteLine(CoreStrings.Settings_NoSettingsFile, filePath);
            return null;
        }

        try
        {
            logger?.WriteLine(CoreStrings.Settings_LoadedSettingsFile, filePath);
            var data = fileSystem.File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(data);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger?.WriteLine(CoreStrings.Settings_ErrorLoadingSettingsFile, filePath, ex.Message);
            return null;
        }
    }

    public void SafeSave<T>(string filePath, T data)
    {
        try
        {
            var dataAsText = JsonConvert.SerializeObject(data, Formatting.Indented);
            fileSystem.File.WriteAllText(filePath, dataAsText);
            logger?.WriteLine(CoreStrings.Settings_SavedSettingsFile, filePath);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger?.WriteLine(CoreStrings.Settings_ErrorSavingSettingsFile, filePath, ex.Message);
        }
    }
}
