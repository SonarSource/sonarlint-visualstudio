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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.Secrets;
using SonarLint.VisualStudio.Integration.Binding;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration
{
    public interface IProjectToLanguageMapper
    {
        /// <summary>
        /// Returns all of the supported Sonar languages for the specified project or Unknown
        /// if no languages are supported
        /// </summary>
        IEnumerable<Language> GetAllBindingLanguagesForProject(Project dteProject);

        /// <summary>
        /// Returns true/false if the project has at least one supported Sonar language
        /// </summary>
        bool HasSupportedLanguage(Project project);

        IEnumerable<Language> GetAllBindingLanguagesInSolution();
    }

    [Export(typeof(IProjectToLanguageMapper))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ProjectToLanguageMapper : IProjectToLanguageMapper
    {
        private readonly ICMakeProjectTypeIndicator cmakeProjectTypeIndicator;
        private readonly IProjectLanguageIndicator projectLanguageIndicator;
        private readonly IConnectedModeSecrets connectedModeSecrets;

        internal static readonly IDictionary<Guid, Language> KnownProjectTypes = new Dictionary<Guid, Language>()
        {
            {new Guid(ProjectSystemHelper.CSharpProjectKind), Language.CSharp},
            {new Guid(ProjectSystemHelper.VbProjectKind), Language.VBNET},
            {new Guid(ProjectSystemHelper.CSharpCoreProjectKind), Language.CSharp},
            {new Guid(ProjectSystemHelper.VbCoreProjectKind), Language.VBNET}
        };

        [ImportingConstructor]
        public ProjectToLanguageMapper(ICMakeProjectTypeIndicator cmakeProjectTypeIndicator,
            IProjectLanguageIndicator projectLanguageIndicator,
            IConnectedModeSecrets connectedModeSecrets)
        {
            this.cmakeProjectTypeIndicator = cmakeProjectTypeIndicator;
            this.projectLanguageIndicator = projectLanguageIndicator;
            this.connectedModeSecrets = connectedModeSecrets;
        }

        public IEnumerable<Language> GetAllBindingLanguagesForProject(Project dteProject)
        {
            if (dteProject == null)
            {
                throw new ArgumentNullException(nameof(dteProject));
            }

            if (!Guid.TryParse(dteProject.Kind, out var projectKind))
            {
                return new[] { Language.Unknown };
            }

            return GetLanguagesInProject(dteProject);
        }

        private IEnumerable<Language> GetLanguagesInProject(Project dteProject)
        {
            var languages = new List<Language>();

            if (!Guid.TryParse(dteProject?.Kind, out var projectKind))
            {
                projectKind = Guid.Empty;
            }


            if (KnownProjectTypes.TryGetValue(projectKind, out var language))
            {
                languages.Add(language);
            }

            if (IsCFamilyProject(projectKind))
            {
                languages.Add(Language.Cpp);
                languages.Add(Language.C);
            }

            if (projectLanguageIndicator.HasTargetLanguage(dteProject, JsTsTargetLanguagePredicate.Instance))
            {
                languages.Add(Language.Js);
                languages.Add(Language.Ts);
            }

            if (projectLanguageIndicator.HasTargetLanguage(dteProject, CssTargetLanguagePredicate.Instance))
            {
                languages.Add(Language.Css);
            }

            if (connectedModeSecrets.AreSecretsAvailable())
            {
                languages.Add(Language.Secrets);
            }

            if (languages.Any())
            {
                return languages;
            }

            return new[] {Language.Unknown};
        }

        public bool HasSupportedLanguage(Project project)
        {
            var languages = GetAllBindingLanguagesForProject(project);

            return languages.Any(x => x.IsSupported);
        }

        public IEnumerable<Language> GetAllBindingLanguagesInSolution()
        {
            return GetLanguagesInProject(null);
        }

        private bool IsCFamilyProject(Guid projectKind)
        {
            return projectKind == new Guid(ProjectSystemHelper.CppProjectKind) || cmakeProjectTypeIndicator.IsCMake();
        }
    }
}
