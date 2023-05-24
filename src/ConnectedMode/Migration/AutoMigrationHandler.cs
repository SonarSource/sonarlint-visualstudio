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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(AutoMigrationHandler))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AutoMigrationHandler
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly IObsoleteConfigurationProvider obsoleteConfigurationProvider;
        private readonly IUnintrusiveBindingController bindingController;

        [ImportingConstructor]
        public AutoMigrationHandler(IUnintrusiveBindingController bindingController, IConfigurationProvider configurationProvider, IObsoleteConfigurationProvider obsoleteConfigurationProvider)
        {
            this.bindingController = bindingController;
            this.configurationProvider = configurationProvider;
            this.obsoleteConfigurationProvider = obsoleteConfigurationProvider;
        }

        internal /* for testing */ void Migrate()
        {
            var obsoleteConfiguration = obsoleteConfigurationProvider.GetConfiguration();

            // If the user has the old files but not the new files it means they are bound and we can migrate over to the new binding.
            if (obsoleteConfiguration.Mode != SonarLintMode.Standalone
                && configurationProvider.GetConfiguration().Mode == SonarLintMode.Standalone)
            {
                bindingController.Bind(obsoleteConfiguration.Project);
            }
        }
    }
}
