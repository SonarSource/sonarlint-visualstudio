//-----------------------------------------------------------------------
// <copyright file="ConfigurationMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurationMock : Configuration
    {
        private string configurationName;

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
        #endregion  
    }
}
