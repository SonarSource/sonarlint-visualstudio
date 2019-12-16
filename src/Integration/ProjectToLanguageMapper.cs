/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration
{
    public static class ProjectToLanguageMapper
    {
        internal static readonly IDictionary<Guid, Language> KnownProjectTypes = new Dictionary<Guid, Language>()
        {
            { new Guid(ProjectSystemHelper.CSharpProjectKind), Language.CSharp },
            { new Guid(ProjectSystemHelper.VbProjectKind), Language.VBNET },
            { new Guid(ProjectSystemHelper.CSharpCoreProjectKind),  Language.CSharp },
            { new Guid(ProjectSystemHelper.VbCoreProjectKind), Language.VBNET },
            { new Guid(ProjectSystemHelper.CppProjectKind), Language.Cpp }
        };

        /// <summary>
        /// Returns the supported Sonar language for the specified project or Unknown
        /// if no languages are supported
        /// </summary>
        /// <returns>
        /// Previously the code assumed a one-to-one mapping between project types and languages.
        /// The worked when the only supported languages were C# and VB. It doesn't work now that
        /// connected mode is supported for C++ projects (which can have both C++ and C files).
        /// New code should call <see cref="GetAllBindingLanguagesForProject(EnvDTE.Project)"/> instead
        /// and handle the fact that there could be multiple supported languages.
        /// </returns>
        [Obsolete("Use GetAllBindingLanguagesForProject instead")]
        public static Language GetLanguageForProject(EnvDTE.Project dteProject)
        {
            if (dteProject == null)
            {
                throw new ArgumentNullException(nameof(dteProject));
            }

            Guid projectKind;
            if (!Guid.TryParse(dteProject.Kind, out projectKind))
            {
                return Language.Unknown;
            }

            Language language;
            if (KnownProjectTypes.TryGetValue(projectKind, out language))
            {
                return language;
            }
            return Language.Unknown;
        }

        /// <summary>
        /// Returns all of the supported Sonar languages for the sepcified project or Unknown
        /// if no languages are supported
        /// </summary>
        public static IEnumerable<Language> GetAllBindingLanguagesForProject(EnvDTE.Project dteProject)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var language = GetLanguageForProject(dteProject);
#pragma warning restore CS0618 // Type or member is obsolete

            if (Language.Cpp.Equals(language))
            {
                return new[] { Language.Cpp, Language.C };
            }

            return new[] { language };
        }
    }
}
