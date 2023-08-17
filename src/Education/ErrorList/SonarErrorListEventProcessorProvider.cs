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
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Education
{
    namespace SonarLint.VisualStudio.Education.ErrorList
    {
        // Notifies VS that we want to handle events from the Error List

        [Export(typeof(ITableControlEventProcessorProvider))]
        [Name("SonarLint ErrorList Event Processor")]

        // Need to hook into the list of processors before the standard VS handler so we can
        // change the behaviour of the "navigate to help" action
        [Order(After = "Default Priority", Before = "ErrorListPackage Table Control Event Processor")]
        [ManagerType("ErrorsTable")]

        // TODO - DataSourceType/DataSource can both be used multiple times. Can we just register for our source and Roslyn?
        // Ideally, we'd only handle our own data source types. However, we also need to handle the Roslyn data source
        [DataSourceType("*")]
        [DataSource("*")]
        internal class SonarErrorListEventProcessorProvider : ITableControlEventProcessorProvider
        {
            private readonly IEducation educationService;
            private readonly IErrorListHelper errorListHelper;
            private readonly ILogger logger;

            [ImportingConstructor]
            public SonarErrorListEventProcessorProvider(IEducation educationService, IErrorListHelper errorListHelper, ILogger logger)
            {
                this.educationService = educationService;
                this.errorListHelper = errorListHelper;
                this.logger = logger;
            }

            public ITableControlEventProcessor GetAssociatedEventProcessor(IWpfTableControl tableControl)
            {
                logger.LogVerbose(Resources.ErrorList_ProcessorCreated);
                return new SonarErrorListEventProcessor(educationService, errorListHelper, logger);
            }
        }
    }
}
