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
using System.Collections.Generic;
using Microsoft.VisualStudio.Settings;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSettingsManager : SettingsManager
    {
        public WritableSettingsStore WritableSettingsStore { get; private set; }

        public bool StoreFailsToLoad { get; set; }

        public ConfigurableSettingsManager(WritableSettingsStore store)
        {
            this.WritableSettingsStore = store;
        }

        #region SettingsManager

        public override string GetApplicationDataFolder(ApplicationDataFolder folder)
        {
            throw new NotImplementedException();
        }

        public override EnclosingScopes GetCollectionScopes(string collectionPath)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetCommonExtensionsSearchPaths()
        {
            throw new NotImplementedException();
        }

        public override EnclosingScopes GetPropertyScopes(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override SettingsStore GetReadOnlySettingsStore(SettingsScope scope)
        {
            throw new NotImplementedException();
        }

        public override WritableSettingsStore GetWritableSettingsStore(SettingsScope scope)
        {
            return this.StoreFailsToLoad ? null : this.WritableSettingsStore;
        }

        #endregion SettingsManager
    }
}