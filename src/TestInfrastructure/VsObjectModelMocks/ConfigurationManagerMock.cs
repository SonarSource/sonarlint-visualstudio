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

using EnvDTE;
using System;
using System.Collections;
using System.Collections.Generic;

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
        #endregion
    }
}
