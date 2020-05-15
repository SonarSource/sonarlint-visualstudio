/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.IO;
using EnvDTE;
using FluentAssertions;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableSolutionRuleSetsInformationProvider : ISolutionRuleSetsInformationProvider
    {
        public const string DummyLegacyModeFolderName = "XXXLegacySolutionFolder";
        public const string DummyConnectedModeFolderName = "XXXConnectedSolutionFolder";

        private readonly Dictionary<Project, List<RuleSetDeclaration>> registeredProjectData = new Dictionary<Project, List<RuleSetDeclaration>>();

        #region ISolutionRuleSetsInformationProvider

        IEnumerable<RuleSetDeclaration> ISolutionRuleSetsInformationProvider.GetProjectRuleSetsDeclarations(Project project)
        {
            project.Should().NotBeNull();

            List<RuleSetDeclaration> result;
            if (!this.registeredProjectData.TryGetValue(project, out result))
            {
                result = new List<RuleSetDeclaration>();
                this.registeredProjectData[project] = result;
            }

            return result;
        }

        string ISolutionRuleSetsInformationProvider.GetSolutionSonarQubeRulesFolder(SonarLintMode bindingMode)
        {
            switch (bindingMode)
            {
                case SonarLintMode.LegacyConnected:
                    return Path.Combine(this.SolutionRootFolder, DummyLegacyModeFolderName);
                case SonarLintMode.Connected:
                    return Path.Combine(this.SolutionRootFolder, DummyConnectedModeFolderName);
                default:
                    throw new System.ArgumentOutOfRangeException($"Invalid bindingMode supplied: {bindingMode}");
            }
        }

        bool ISolutionRuleSetsInformationProvider.TryGetProjectRuleSetFilePath(Project project, RuleSetDeclaration declaration, out string fullFilePath)
        {
            fullFilePath = declaration.RuleSetPath;

            return true;
        }

        #endregion ISolutionRuleSetsInformationProvider

        #region Test helpers

        public void RegisterProjectInfo(Project project, params RuleSetDeclaration[] info)
        {
            List<RuleSetDeclaration> declarations;

            if (!this.registeredProjectData.TryGetValue(project, out declarations))
            {
                declarations = new List<RuleSetDeclaration>();
                this.registeredProjectData[project] = declarations;
            }

            declarations.AddRange(info);
        }

        public void ClearProjectInfo(Project project)
        {
            this.registeredProjectData.Remove(project);
        }

        public string SolutionRootFolder { get; set; }

        #endregion Test helpers
    }
}
