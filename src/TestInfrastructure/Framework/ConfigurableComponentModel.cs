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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableComponentModel : IComponentModel, IDisposable
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

        #endregion Test helper methods

        #region IComponentModel

        ComposablePartCatalog IComponentModel.DefaultCatalog
        {
            get { throw new NotImplementedException(); }
        }

        ICompositionService IComponentModel.DefaultCompositionService
        {
            get { return this.DefaultCompositionService; }
        }

        ExportProvider IComponentModel.DefaultExportProvider
        {
            get { throw new NotImplementedException(); }
        }

        ComposablePartCatalog IComponentModel.GetCatalog(string catalogName)
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

        #endregion IComponentModel

        #region IDisposable

        public void Dispose()
        {
            this.container.Dispose();
        }

        #endregion IDisposable

        public CompositionContainer CompositionContainer
        {
            get
            {
                return this.container;
            }
        }
    }
}