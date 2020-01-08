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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.Core.CFamily
{
    public class CFamilyBindingConfigFile : IBindingConfigFile
    {
        private readonly IFile fileWrapper;

        public CFamilyBindingConfigFile(UserSettings userSettings)
            : this (userSettings, new FileWrapper())
        {
        }

        public CFamilyBindingConfigFile(UserSettings userSettings, IFile fileWrapper)
        {
            this.UserSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));
            this.fileWrapper = fileWrapper;
        }

        internal /* for testing */ UserSettings UserSettings { get; }

        #region IBindingConfigFile implementation

        public void Save(string fullFilePath)
        {
            string dataAsText = JsonConvert.SerializeObject(this.UserSettings, Formatting.Indented);
            fileWrapper.WriteAllText(fullFilePath, dataAsText);
        }

        #endregion IBindingConfigFile implementation
    }
}
