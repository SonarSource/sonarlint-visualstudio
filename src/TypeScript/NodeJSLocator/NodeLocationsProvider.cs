/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Linq;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator.LocationProviders;

namespace SonarLint.VisualStudio.TypeScript.NodeJSLocator
{
    internal interface INodeLocationsProvider
    {
        /// <summary>
        /// Returns `node.exe` candidate file locations, in precedence order. The files might not exist on disk.
        /// </summary>
        IReadOnlyCollection<string> Get();
    }

    [Export(typeof(INodeLocationsProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class NodeLocationsProvider : INodeLocationsProvider
    {
        internal readonly IList<INodeLocationsProvider> LocationProviders;

        [ImportingConstructor]
        public NodeLocationsProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, ILogger logger)
            : this(new INodeLocationsProvider[]
            {
                new EnvironmentVariableNodeLocationsProvider(logger),
                new GlobalPathNodeLocationsProvider(), 
                new BundledNodeLocationsProvider(serviceProvider, logger), 
            })
        {
        }

        internal NodeLocationsProvider(IList<INodeLocationsProvider> locationProviders)
        {
            LocationProviders = locationProviders;
        }

        public IReadOnlyCollection<string> Get()
        {
            return LocationProviders
                .SelectMany(x => x.Get() ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
        }
    }
}
