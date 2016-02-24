//-----------------------------------------------------------------------
// <copyright file="ConfigurationManagerMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurationManagerMock : ConfigurationManager
    {
        private List<ConfigurationMock> configurations = new List<ConfigurationMock>();

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
        #endregion
    }
}
