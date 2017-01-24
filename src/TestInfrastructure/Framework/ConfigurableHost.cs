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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System;
using System.Linq;
using System.Windows.Threading;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableHost : IHost
    {
        private readonly ConfigurableServiceProvider serviceProvider;

        public ConfigurableHost()
            : this(new ConfigurableServiceProvider(), Dispatcher.CurrentDispatcher)
        {
        }

        public ConfigurableHost(ConfigurableServiceProvider sp, Dispatcher dispatcher)
        {
            this.serviceProvider = sp;
            this.UIDispatcher = dispatcher;
            this.VisualStateManager = new ConfigurableStateManager { Host = this };
        }

        #region IHost

        public event EventHandler ActiveSectionChanged;

        public ISectionController ActiveSection
        {
            get;
            private set;
        }

        public ISonarQubeServiceWrapper SonarQubeService
        {
            get;
            set;
        }

        public Dispatcher UIDispatcher
        {
            get;
            private set;
        }

        public IStateManager VisualStateManager
        {
            get;
            set;
        }

        public void ClearActiveSection()
        {
            this.ActiveSection = null;

            // Simulate product code
            this.VisualStateManager.SyncCommandFromActiveSection();
        }

        public object GetService(Type serviceType)
        {
            if (typeof(ILocalService).IsAssignableFrom(serviceType))
            {
                Assert.IsTrue(VsSessionHost.SupportedLocalServices.Contains(serviceType), "The specified service type '{0}' will not be serviced in the real IHost.", serviceType.FullName);
            }

            return this.serviceProvider.GetService(serviceType);
        }

        public void SetActiveSection(ISectionController section)
        {
            Assert.IsNotNull(section);

            this.ActiveSection = section;

            // Simulate product code
            this.VisualStateManager.SyncCommandFromActiveSection();
        }

        public ISet<Language> SupportedPluginLanguages { get; } = new HashSet<Language>();

        #endregion

        #region Test helpers
        public void SimulateActiveSectionChanged()
        {
            this.ActiveSectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public ConfigurableStateManager TestStateManager
        {
            get { return (ConfigurableStateManager)this.VisualStateManager; }
        }

        #endregion
    }
}
