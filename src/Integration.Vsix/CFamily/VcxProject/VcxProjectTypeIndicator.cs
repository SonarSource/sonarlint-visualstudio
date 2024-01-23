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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.VCProjectEngine;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject
{
    [Export(typeof(IVcxProjectTypeIndicator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class VcxProjectTypeIndicator : IVcxProjectTypeIndicator
    {
        private readonly IProjectSystemHelper projectSystemHelper;

        [ImportingConstructor]
        public VcxProjectTypeIndicator(IProjectSystemHelper projectSystemHelper)
        {
            this.projectSystemHelper = projectSystemHelper;
        }

        public VcxProjectTypesResult GetProjectTypes()
        {
            bool hasAnalyzableVcxProjects = false;
            bool hasNonAnalyzableVcxProjects = false;

            foreach (var project in projectSystemHelper.GetSolutionProjects())
            {
                if (project.Object is VCProject vcProject)
                {
                    if (IsAnalyzableVcx(vcProject))
                    {
                        hasAnalyzableVcxProjects = true;
                    }
                    else
                    {
                        hasNonAnalyzableVcxProjects = true;
                    }
                }
            }

            return new VcxProjectTypesResult
            {
                HasAnalyzableVcxProjects = hasAnalyzableVcxProjects,
                HasNonAnalyzableVcxProjects = hasNonAnalyzableVcxProjects
            };
        }

        private bool IsAnalyzableVcx(VCProject vcProject)
        {
            var vcConfig = vcProject.ActiveConfiguration;
            var tools = vcConfig.Tools as IVCCollection;
            var toolItem = tools.Item("VCCLCompilerTool");

            // We don't support custom build tools. VCCLCompilerTool is needed for all the necessary compilation options to be present
            return toolItem is VCCLCompilerTool;
        }
    }
}
