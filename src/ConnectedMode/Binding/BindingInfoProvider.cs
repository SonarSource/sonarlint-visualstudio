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
using Newtonsoft.Json.Linq;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    [Export(typeof(IBindingInfoProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class BindingInfoProvider : IBindingInfoProvider
    {
        private readonly IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider;
        private readonly IFileSystem fileSystem;

        [ImportingConstructor]
        public BindingInfoProvider(IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider) : this(unintrusiveBindingPathProvider, new FileSystem())
        {
        }

        internal BindingInfoProvider(IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider, IFileSystem fileSystem)
        {
            this.unintrusiveBindingPathProvider = unintrusiveBindingPathProvider;
            this.fileSystem = fileSystem;
        }

        public IList<BindingInfo> GetExistingBindings()
        {
            var result = new List<BindingInfo>();

            if (fileSystem.Directory.Exists(unintrusiveBindingPathProvider.SLVSRootBindingFolder))
            {
                var bindings = fileSystem.Directory.GetDirectories(unintrusiveBindingPathProvider.SLVSRootBindingFolder);

                foreach (var binding in bindings)
                {
                    var configFilePath = Path.Combine(binding, "binding.config");

                    if (!fileSystem.File.Exists(configFilePath)) { continue; }

                    var fileContent = fileSystem.File.ReadAllText(configFilePath);

                    BindingInfo bindingInfo = CreateBindingInfo(fileContent);

                    result.Add(bindingInfo);
                }
            }
            return result;
        }

        private static BindingInfo CreateBindingInfo(string fileContent)
        {
            var bindingObject = JObject.Parse(fileContent);

            var organisation = bindingObject["Organization"]["Key"];

            var serverUri = bindingObject["ServerUri"].ToString();
            var projectKey = bindingObject["ProjectKey"].ToString();

            var bindingInfo = new BindingInfo { ServerUri = serverUri, ProjectKey = projectKey };
            if (organisation.HasValues)
            {
                bindingInfo.Organization = organisation.ToString();
            }

            return bindingInfo;
        }
    }
}
