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

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;

using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    [Export(typeof(IMefFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MefFactory : IMefFactory
    {
        private readonly IServiceProvider serviceProvider;

        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public MefFactory([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, IThreadHandling threadHandling)
        {
            this.serviceProvider = serviceProvider;
            this.threadHandling = threadHandling;
        }

        public async Task<T> CreateAsync<T>() where T : class
        {
            T instance = null;

            await threadHandling.RunOnUIThreadAsync(() =>
                {
                    var componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
                    instance = componentModel.GetService<T>();
                });

            return instance;
        }
    }
}
