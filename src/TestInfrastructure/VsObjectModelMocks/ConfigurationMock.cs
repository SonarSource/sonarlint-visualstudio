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
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurationMock : Configuration
    {
        private readonly string configurationName;

        public ConfigurationMock(string name)
        {
            this.configurationName = name;
            this.Properties = new PropertiesMock(this);
        }

        public PropertiesMock Properties
        {
            get;
        }

        public string ConfigurationName
        {
            get { return this.configurationName; }
        }

        #region Configuration

        ConfigurationManager Configuration.Collection
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string Configuration.ConfigurationName
        {
            get
            {
                return this.configurationName;
            }
        }

        DTE Configuration.DTE
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string Configuration.ExtenderCATID
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object Configuration.ExtenderNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool Configuration.IsBuildable
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool Configuration.IsDeployable
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool Configuration.IsRunable
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object Configuration.Object
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        OutputGroups Configuration.OutputGroups
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object Configuration.Owner
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string Configuration.PlatformName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Properties Configuration.Properties
        {
            get
            {
                return this.Properties;
            }
        }

        vsConfigurationType Configuration.Type
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object Configuration.get_Extender(string ExtenderName)
        {
            throw new NotImplementedException();
        }

        #endregion Configuration
    }
}