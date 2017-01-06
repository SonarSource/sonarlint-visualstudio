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

using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableComponentModel : IComponentModel, IDisposable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly CompositionContainer container;

        public ConfigurableComponentModel(ComposablePartCatalog defaultCatalog = null)
            : this(new ConfigurableServiceProvider(), defaultCatalog)
        {

        }
        public ConfigurableComponentModel(IServiceProvider serviceProvider, ComposablePartCatalog defaultCatalog = null)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            this.serviceProvider = serviceProvider;
            this.container = defaultCatalog == null ? new CompositionContainer() : new CompositionContainer(defaultCatalog);
            this.DefaultCompositionService = this.container;
        }

        #region Test helper methods
        /// <summary>
        /// Creates and returns a new IComponentModel instance that supports composition.
        /// The model will have the exports pre configured
        public static IComponentModel CreateWithExports(params Export[] exports)
        {
            ConfigurableComponentModel model = new ConfigurableComponentModel();
            ConfigurableCompositionService svc = new ConfigurableCompositionService(model.container);
            svc.ExportsToCompose.AddRange(exports);
            svc.SatisfyImportsOnce((ComposablePart)null);
            model.DefaultCompositionService = svc;
            return model;
        }


        /// <summary>
        /// Creates and returns a new IComponentModel instance that supports calls
        /// to DefaultCompositionService.SatisyImportsOnce.
        /// The supplied composable parts will be used in the composition.
        /// </summary>
        public static IComponentModel Create(params object[] composableParts)
        {
            ConfigurableComponentModel model = new ConfigurableComponentModel();
            ConfigurableCompositionService svc = new ConfigurableCompositionService(model.container);
            if (composableParts != null)
            {
                svc.PartsToCompose.AddRange(composableParts);
            }
            model.DefaultCompositionService = svc;
            return model;
        }

        /// <summary>
        /// Creates and returns a new IComponentModel instance that supports calls
        /// to DefaultCompositionService.SatisyImportsOnce.
        /// The supplied composable parts will be used in the composition.
        /// </summary>
        /// <param name="doNothingOnSatisfyImportsOnce">When set to true, SatisfyImportsOnce will do nothing.</param>
        public static IComponentModel Create(bool doNothingOnSatisfyImportsOnce)
        {
            ConfigurableComponentModel model = new ConfigurableComponentModel();
            model.DefaultCompositionService = new ConfigurableCompositionService(model.container) { DoNothingOnSatisfyImportsOnce = doNothingOnSatisfyImportsOnce };
            return model;
        }

        /// <summary>
        /// Creates and returns a new IComponentModel instance that supports calls
        /// to DefaultCompositionService.SatisyImportsOnce.
        /// </summary>
        /// <param name="catalog">An existing catalog to use</param>
        public static IComponentModel Create(ComposablePartCatalog catalog)
        {
            ConfigurableComponentModel model = new ConfigurableComponentModel(catalog);
            model.DefaultCompositionService = new ConfigurableCompositionService(model.container) { };
            return model;
        }

        public ICompositionService DefaultCompositionService
        {
            get;
            private set;
        }
        #endregion

        #region IComponentModel
        System.ComponentModel.Composition.Primitives.ComposablePartCatalog IComponentModel.DefaultCatalog
        {
            get { throw new NotImplementedException(); }
        }

        System.ComponentModel.Composition.ICompositionService IComponentModel.DefaultCompositionService
        {
            get { return this.DefaultCompositionService; }
        }

        System.ComponentModel.Composition.Hosting.ExportProvider IComponentModel.DefaultExportProvider
        {
            get { throw new NotImplementedException(); }
        }

        System.ComponentModel.Composition.Primitives.ComposablePartCatalog IComponentModel.GetCatalog(string catalogName)
        {
            throw new NotImplementedException();
        }

        IEnumerable<T> IComponentModel.GetExtensions<T>()
        {
            return this.container.GetExports<T>().Select(l => l.Value);
        }

        T IComponentModel.GetService<T>()
        {
            T result = this.container.GetExports<T>().Select(l => l.Value).SingleOrDefault();
            if (result == null)
            {
                // Emulate the real VS IComponentModel behavior
                throw new Exception("Microsoft.VisualStudio.Composition.CompositionFailedException");
            }

            return result;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            this.container.Dispose();
        }
        #endregion

        public CompositionContainer CompositionContainer
        {
            get
            {
                return this.container;
            }
        }
    }
}
