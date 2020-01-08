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

using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.Binding
{
    /// <summary>
    /// Contract to provide the binding-related configuration for one or more languages
    /// </summary>
    public interface IBindingConfigProvider
    {
        bool IsLanguageSupported(Language language);

        /// <summary>
        /// Returns a configuration file for the specified language
        /// </summary>
        Task<IBindingConfigFile> GetConfigurationAsync(SonarQubeQualityProfile qualityProfile, string organizationKey, Language language, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Abstraction of the various types of configuration file used to store binding configuration information
    /// </summary>
    /// <remarks>e.g. for C# and VB.NET the configuration will be in a ruleset file.
    /// For C++ it will be in a json file in a Sonar-specific format</remarks>
    public interface IBindingConfigFile
    {
        /// <summary>
        /// Saves the file, replacing any existing file
        /// </summary>
        void Save(string fullFilePath);
    }
}
