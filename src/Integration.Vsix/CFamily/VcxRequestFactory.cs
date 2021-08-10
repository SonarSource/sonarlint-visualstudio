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

using System;
using System.ComponentModel.Composition;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    [Export(typeof(IRequestFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class VcxRequestFactory : IRequestFactory
    {
        private readonly ICFamilyRulesConfigProvider cFamilyRulesConfigProvider;
        private readonly ILogger logger;
        private readonly DTE dte;

        [ImportingConstructor]
        public VcxRequestFactory([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ICFamilyRulesConfigProvider cFamilyRulesConfigProvider,
            ILogger logger)
        {
            this.dte = serviceProvider.GetService<DTE>();
            this.cFamilyRulesConfigProvider = cFamilyRulesConfigProvider;
            this.logger = logger;
        }

        public IRequest TryGet(string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions)
        {
            var projectItem = dte?.Solution?.FindProjectItem(analyzedFilePath);

            if (projectItem == null)
            {
                return null;
            }

            // todo: extract the code from CFamilyHelper into this class
            return CFamilyHelper.CreateRequest(logger,
                projectItem,
                analyzedFilePath,
                cFamilyRulesConfigProvider,
                analyzerOptions);
        }
    }
}
