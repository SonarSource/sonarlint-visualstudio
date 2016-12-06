//-----------------------------------------------------------------------
// <copyright file="IntegrationSettings.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(IIntegrationSettings))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class IntegrationSettings : Component, IIntegrationSettings, IProfileManager
    {
        public const string SettingsRoot = "SonarLintForVisualStudio";
        public const string ShowServerNuGetTrustWarningKey = "ShowServerNuGetTrustWarning";
        public const string AllowNuGetPackageInstallKey = "AllowNuGetPackageInstall";

        private readonly WritableSettingsStore store;

        #region Constructors and initialization

        [ImportingConstructor]
        public IntegrationSettings([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : this(serviceProvider, new ShellSettingsManager(serviceProvider))
        {
        }

        internal /* for testing purposes */ IntegrationSettings(IServiceProvider serviceProvider, SettingsManager settingsManager)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (settingsManager == null)
            {
                throw new ArgumentNullException(nameof(settingsManager));
            }

            this.store = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            Debug.Assert(this.store != null, "Could not get WritableSettingsStore, no settings will be persisted!");
            if (this.store != null)
            {
                this.Initialize();
            }
        }

        private void Initialize()
        {
            if (!this.store.CollectionExists(SettingsRoot))
            {
                this.store.CreateCollection(SettingsRoot);
            }
        }


        #endregion

        #region IProfileManager

        public void SaveSettingsToXml(IVsSettingsWriter writer)
        {
            // We do not support import/export
        }

        public void LoadSettingsFromXml(IVsSettingsReader reader)
        {
            // We do not support import/export
        }

        public void SaveSettingsToStorage()
        {
            // No-op; settings are live
        }

        public void LoadSettingsFromStorage()
        {
            // No-op; settings are live
        }

        public void ResetSettings()
        {
            // Not supported
        }

        #endregion

        #region Read/Write helpers

        internal /* testing purposes */ bool GetValueOrDefault(string key, bool defaultValue)
        {
            return this.store?.GetBoolean(SettingsRoot, key, defaultValue) ?? defaultValue;
        }

        internal /* testing purposes */ void SetValue(string key, bool value)
        {
            this.store?.SetBoolean(SettingsRoot, key, value);
        }

        #endregion

        #region Settings

        [LocalizedCategory(nameof(Strings.WarningsSettingsCategory))]
        [LocalizedDisplayName(nameof(Strings.ShowServerNuGetTrustWarningSettingName))]
        [LocalizedDescription(nameof(Strings.ShowServerNuGetTrustWarningSettingDescription))]
        public bool ShowServerNuGetTrustWarning
        {
            get { return this.GetValueOrDefault(ShowServerNuGetTrustWarningKey, true); }
            set { this.SetValue(ShowServerNuGetTrustWarningKey, value); }
        }

        [LocalizedCategory(nameof(Strings.NugetSettingsCategory))]
        [LocalizedDisplayName(nameof(Strings.AllowNuGetPackageInstallationName))]
        [LocalizedDescription(nameof(Strings.AllowNuGetPackageInstallationSettingDescription))]
        public bool AllowNuGetPackageInstall
        {
            get { return this.GetValueOrDefault(AllowNuGetPackageInstallKey, true); }
            set { this.SetValue(AllowNuGetPackageInstallKey, value); }
        }

        #endregion
    }
}
