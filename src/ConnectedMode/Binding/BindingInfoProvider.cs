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
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    [Export(typeof(IBindingInfoProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class BindingInfoProvider : IBindingInfoProvider
    {
        private readonly IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider;
        private readonly ISolutionBindingFileLoader solutionBindingFileLoader;
        private readonly IFileSystem fileSystem;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public BindingInfoProvider(IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider, ISolutionBindingFileLoader solutionBindingFileLoader)
            : this(unintrusiveBindingPathProvider, solutionBindingFileLoader, new FileSystem(), ThreadHandling.Instance)
        {
        }

        internal BindingInfoProvider(IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider, ISolutionBindingFileLoader solutionBindingFileLoader, IFileSystem fileSystem, IThreadHandling threadHandling)
        {
            this.unintrusiveBindingPathProvider = unintrusiveBindingPathProvider;
            this.solutionBindingFileLoader = solutionBindingFileLoader;
            this.fileSystem = fileSystem;
            this.threadHandling = threadHandling;
        }

        public IEnumerable<BindingInfo> GetExistingBindings()
        {
            threadHandling.ThrowIfOnUIThread();

            var result = new List<BindingInfo>();

            if (fileSystem.Directory.Exists(unintrusiveBindingPathProvider.SLVSRootBindingFolder))
            {
                var bindings = fileSystem.Directory.GetDirectories(unintrusiveBindingPathProvider.SLVSRootBindingFolder);

                foreach (var binding in bindings)
                {
                    var configFilePath = Path.Combine(binding, "binding.config");

                    var boundSonarQubeProject = solutionBindingFileLoader.Load(configFilePath);

                    if (boundSonarQubeProject == null) { continue; }

                    result.Add(ConvertToBindingInfo(boundSonarQubeProject));
                }
            }
            return result.Distinct();
        }

        private BindingInfo ConvertToBindingInfo(BoundSonarQubeProject boundSonarQubeProject)
            => new BindingInfo
            {
                Organization = boundSonarQubeProject.Organization?.Key,
                ProjectKey = boundSonarQubeProject.ProjectKey,
                ServerUri = boundSonarQubeProject.ServerUri
            };
    }
}
