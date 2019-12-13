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

using EnvDTE;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// This class was created when refactoring binding to add support for C++ projects.
    /// It contains shared logic that doesn't currently have a proper home. It shouldn't
    /// exist (hence the name). It should be removed in the future if/when further
    /// refactoring is done and a more appropriate location for the responsibilities is
    /// created.
    /// </summary>
    internal static class BindingRefactoringDumpingGround
    {
        internal /* for testing */ static bool IsProjectLevelBindingRequired(Project project)
        {
            var language = ProjectToLanguageMapper.GetLanguageForProject(project);
            return language == Core.Language.VBNET || language == Core.Language.CSharp;
        }
    }
}
