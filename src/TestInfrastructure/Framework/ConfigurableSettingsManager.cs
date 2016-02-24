//-----------------------------------------------------------------------
// <copyright file="ConfigurableSettingsManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Settings;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableSettingsManager : SettingsManager
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

        #endregion
    }

}
