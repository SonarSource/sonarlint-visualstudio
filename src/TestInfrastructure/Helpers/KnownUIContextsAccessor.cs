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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public static class KnownUIContextsAccessor
    {
        // KnownUIContexts is not very friendly for testing, and requires this static properties
        static KnownUIContextsAccessor()
        {
            ServiceProvider = VsServiceProviderHelper.GlobalServiceProvider;
            MonitorSelectionService = new ConfigurableVsMonitorSelection();
            ServiceProvider.RegisterService(typeof(IVsMonitorSelection), MonitorSelectionService, true);
            Reset();
        }

        public static ConfigurableVsMonitorSelection MonitorSelectionService
        {
            get;
            private set;
        }

        public static ConfigurableServiceProvider ServiceProvider
        {
            get;
        }

        public static void Reset()
        {
            MonitorSelectionService.UIContexts
                .ToList()
                .ForEach(contextId => MonitorSelectionService.SetContext(contextId, false));

            Assert.IsTrue(KnownUIContextsProperties.All(pi => pi.GetValue(null) != null), "UIContext failed to register");
        }

        private static IEnumerable<PropertyInfo> KnownUIContextsProperties
        {
            get
            {
                return typeof(KnownUIContexts)
                    .GetProperties(BindingFlags.Static | BindingFlags.Public)
                    .Where(p => p.PropertyType.IsEquivalentTo(typeof(UIContext)));
            }
        }
    }
}
