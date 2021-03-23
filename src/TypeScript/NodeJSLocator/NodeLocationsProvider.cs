/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

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
        private readonly IEnumerable<INodeLocationsProvider> locationsProviders;

        [ImportingConstructor]
        public NodeLocationsProvider()
            : this(Enumerable.Empty<INodeLocationsProvider>())
        {
        }

        internal NodeLocationsProvider(IEnumerable<INodeLocationsProvider> locationsProviders)
        {
            this.locationsProviders = locationsProviders;
        }

        public IReadOnlyCollection<string> Get()
        {
            return locationsProviders
                .SelectMany(x => x.Get() ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToArray();
        }
    }
}
