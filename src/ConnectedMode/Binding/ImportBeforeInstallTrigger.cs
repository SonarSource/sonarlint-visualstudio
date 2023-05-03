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

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    [Export(typeof(ImportBeforeInstallTrigger))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ImportBeforeInstallTrigger
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly IImportBeforeFileGenerator importBeforeFileGenerator;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public ImportBeforeInstallTrigger(IConfigurationProvider configurationProvider, IImportBeforeFileGenerator importBeforeFileGenerator, IThreadHandling threadHandling)
        {
            this.configurationProvider = configurationProvider;
            this.importBeforeFileGenerator = importBeforeFileGenerator;
            this.threadHandling = threadHandling;
        }

        public async Task TriggerUpdate()
        {
            var config = configurationProvider.GetConfiguration();

            if (config.Mode != SonarLintMode.Standalone)
            {
                await threadHandling.SwitchToBackgroundThread();

                importBeforeFileGenerator.WriteTargetsFileToDiskIfNotExists();
            }
        }
    }
}
