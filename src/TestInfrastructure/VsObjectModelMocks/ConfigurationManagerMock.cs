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
using System.Collections;
using System.Collections.Generic;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurationManagerMock : ConfigurationManager
    {
        private readonly List<ConfigurationMock> configurations = new List<ConfigurationMock>();

        public ConfigurationMock ActiveConfiguration
        {
            get;
            set;
        }

        public List<ConfigurationMock> Configurations
        {
            get
            {
                return this.configurations;
            }
        }

        #region ConfigurationManager

        Configuration ConfigurationManager.ActiveConfiguration
        {
            get
            {
                return this.ActiveConfiguration;
            }
        }

        object ConfigurationManager.ConfigurationRowNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        int ConfigurationManager.Count
        {
            get
            {
                return this.configurations.Count;
            }
        }

        DTE ConfigurationManager.DTE
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object ConfigurationManager.Parent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object ConfigurationManager.PlatformNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object ConfigurationManager.SupportedPlatforms
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Configurations ConfigurationManager.AddConfigurationRow(string NewName, string ExistingName, bool Propagate)
        {
            throw new NotImplementedException();
        }

        Configurations ConfigurationManager.AddPlatform(string NewName, string ExistingName, bool Propagate)
        {
            throw new NotImplementedException();
        }

        Configurations ConfigurationManager.ConfigurationRow(string Name)
        {
            throw new NotImplementedException();
        }

        void ConfigurationManager.DeleteConfigurationRow(string Name)
        {
            throw new NotImplementedException();
        }

        void ConfigurationManager.DeletePlatform(string Name)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.configurations.GetEnumerator();
        }

        IEnumerator ConfigurationManager.GetEnumerator()
        {
            return this.configurations.GetEnumerator();
        }

        Configuration ConfigurationManager.Item(object index, string Platform)
        {
            throw new NotImplementedException();
        }

        Configurations ConfigurationManager.Platform(string Name)
        {
            throw new NotImplementedException();
        }

        #endregion ConfigurationManager
    }
}