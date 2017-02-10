/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
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