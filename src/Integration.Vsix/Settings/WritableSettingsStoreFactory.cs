/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.Settings
{
    internal interface IWritableSettingsStoreFactory
    {
        /// <summary>
        /// Creates and returns a new instance of the VS settings store to persist
        /// SonarLint settings between sessions
        /// </summary>
        /// <remarks>Each call will create a new instance and ensure that the 
        /// specified collection has been initialized.
        /// The class follows the VS threading rules.</remarks>
        WritableSettingsStore Create(string settingsRoot);
    }

    [Export(typeof(IWritableSettingsStoreFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class WritableSettingsStoreFactory : IWritableSettingsStoreFactory
    {
        #region Test infrastructure
        // We need to create a real instance of the VS concrete class ShellSettingsManager
        // at runtime. However, we want to be able to unit test our code, so we define a 
        // delegate for a factory method the tests can inject replace the VS class with a mock.

        internal delegate SettingsManager VSSettingsFactoryMethod(IServiceProvider settingsService);
        private readonly VSSettingsFactoryMethod settingsManagerFactoryMethod;

        private static SettingsManager CreateRealVSSettingsManager(IServiceProvider serviceProvider)
            => new ShellSettingsManager(serviceProvider);

        #endregion

        private readonly IServiceProvider serviceProvider;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public WritableSettingsStoreFactory(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IThreadHandling threadHandling)
            : this(serviceProvider,
                  threadHandling,
                  CreateRealVSSettingsManager)
        {}

        internal /* for testing */ WritableSettingsStoreFactory(IServiceProvider serviceProvider,
            IThreadHandling threadHandling,
            VSSettingsFactoryMethod createSettingsManager)
        {
            this.serviceProvider = serviceProvider;
            this.threadHandling = threadHandling;
            this.settingsManagerFactoryMethod = createSettingsManager;
        }

        public WritableSettingsStore Create(string settingsRoot)
        {
            WritableSettingsStore store = null;
            threadHandling.RunOnUIThread(() =>
            {
                // At runtime we'll be creating the real VS class.
                // When testing, the tests will inject a dummy method returning a mock.
                var settingsManager = settingsManagerFactoryMethod(serviceProvider);

                store = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                if (store != null &&
                    !store.CollectionExists(settingsRoot))
                {
                    store.CreateCollection(settingsRoot);
                }

            });
            return store;
        }
    }
}
