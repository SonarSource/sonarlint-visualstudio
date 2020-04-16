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
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurationMock : Configuration
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

        public string ConfigurationName => configurationName;

        #region Configuration

        ConfigurationManager Configuration.Collection => throw new NotImplementedException();

        string Configuration.ConfigurationName => configurationName;

        DTE Configuration.DTE => throw new NotImplementedException();

        string Configuration.ExtenderCATID => throw new NotImplementedException();

        object Configuration.ExtenderNames => throw new NotImplementedException();

        bool Configuration.IsBuildable => throw new NotImplementedException();

        bool Configuration.IsDeployable => throw new NotImplementedException();

        bool Configuration.IsRunable => throw new NotImplementedException();

        object Configuration.Object => throw new NotImplementedException();

        OutputGroups Configuration.OutputGroups => throw new NotImplementedException();

        object Configuration.Owner => throw new NotImplementedException();

        string Configuration.PlatformName => "x64";

        Properties Configuration.Properties => Properties;

        vsConfigurationType Configuration.Type => throw new NotImplementedException();

        object Configuration.get_Extender(string ExtenderName)
        {
            throw new NotImplementedException();
        }

        #endregion Configuration
    }
}
