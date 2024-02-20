/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    [Export(typeof(IServerConnectionConfigurationProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ServerConnectionConfigurationProvider : IServerConnectionConfigurationProvider
    {
        private readonly ISolutionBindingRepository solutionBindingRepository;
        private readonly IThreadHandling threadHandling;
        private readonly IConnectionIdHelper connectionIdHelper;

        private List<ServerConnectionConfiguration> bindingList = null;

        [ImportingConstructor]
        [ExcludeFromCodeCoverage]
        public ServerConnectionConfigurationProvider(ISolutionBindingRepository solutionBindingRepository)
            : this(solutionBindingRepository, ThreadHandling.Instance, new ConnectionIdHelper())
        {
        }

        internal ServerConnectionConfigurationProvider(ISolutionBindingRepository solutionBindingRepository, IThreadHandling threadHandling, IConnectionIdHelper connectionIdHelper)
        {
            this.solutionBindingRepository = solutionBindingRepository;
            this.threadHandling = threadHandling;
            this.connectionIdHelper = connectionIdHelper;
        }

        public IEnumerable<T> GetServerConnectionConfiguration<T>() where T : ServerConnectionConfiguration
        {
            if (bindingList == null) { InitBindingList(); }

            return bindingList.OfType<T>();
        }

        private void InitBindingList()
        {
            threadHandling.ThrowIfOnUIThread();

            bindingList = new List<ServerConnectionConfiguration>();

            var bindings = solutionBindingRepository.List().Distinct(new BoundSonarQubeProjectUriComparer());

            foreach (var binding in bindings)
            {
                var connectionID = connectionIdHelper.GetConnectionIdFromUri(binding.ServerUri, binding.Organization?.Key);

                if (binding.ServerUri == ConnectionIdHelper.SonarCloudUri)
                {
                    bindingList.Add(new SonarCloudConnectionConfigurationDto(connectionID, true, binding.Organization?.Key));
                }
                else
                {
                    bindingList.Add(new SonarQubeConnectionConfigurationDto(connectionID, true, binding.ServerUri.ToString()));
                }
            }
        }

        private sealed class BoundSonarQubeProjectUriComparer : IEqualityComparer<BoundSonarQubeProject>
        {
            public bool Equals(BoundSonarQubeProject x, BoundSonarQubeProject y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null && y == null) { return true; }

                if (x == null ^ y == null)
                {
                    return false;
                }

                return x.ServerUri == y.ServerUri;
            }

            public int GetHashCode(BoundSonarQubeProject obj)
            {
                return obj.ServerUri.GetHashCode();
            }
        }
    }
}
