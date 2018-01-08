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

namespace SonarLint.VisualStudio.Integration
{
    public static class ProjectToLanguageMapper
    {
        internal static readonly IDictionary<Guid, Language> KnownProjectTypes = new Dictionary<Guid, Language>()
        {
            { new Guid(ProjectSystemHelper.CSharpProjectKind), Language.CSharp },
            { new Guid(ProjectSystemHelper.VbProjectKind), Language.VBNET },
            { new Guid(ProjectSystemHelper.CSharpCoreProjectKind),  Language.CSharp },
            { new Guid(ProjectSystemHelper.VbCoreProjectKind), Language.VBNET }
        };

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

    }
}
