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

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    /// <summary>
    /// Egregious hack to enable saving the configuration in .suo files
    /// </summary>
    /// <remarks>Only VS packages can save data in .suo files. However, the serialization
    /// classes depend on internal classes and MEF-created classes that are in this assembly.
    /// As a hacky workaround, this config provider is a singleton that is visible to the
    /// package assembly.
    public class InMemoryConfigurationProvider : IConfigurationProvider
    {
        private BindingConfiguration data = BindingConfiguration.Standalone;

        public static InMemoryConfigurationProvider Instance => new InMemoryConfigurationProvider();

        private InMemoryConfigurationProvider()
        {
            // Singleton
        }

        public void DeleteConfiguration()
        {
            data = BindingConfiguration.Standalone;
        }

        public BindingConfiguration GetConfiguration()
        {
            return data;
        }

        public bool WriteConfiguration(BindingConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            data = configuration;
            return true;
        }
    }
}