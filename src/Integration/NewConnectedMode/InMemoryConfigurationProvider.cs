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
    /// As a hacky workaround, this config provider saves data to a well-known location (i.e.
    /// the static "data" field of this class). The package can get/set this data simply
    /// by creating its own copy of this class.</remarks>
    public class InMemoryConfigurationProvider : IConfigurationProvider
    {
        private static BindingConfiguration data = BindingConfiguration.Standalone;

        public void DeleteConfiguration()
        {
#pragma warning disable S2696 // Instance members should not write to "static" fields
            data = BindingConfiguration.Standalone;
#pragma warning restore S2696 // Instance members should not write to "static" fields
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

#pragma warning disable S2696 // Instance members should not write to "static" fields
            data = configuration;
#pragma warning restore S2696 // Instance members should not write to "static" fields

            return true;
        }
    }
}