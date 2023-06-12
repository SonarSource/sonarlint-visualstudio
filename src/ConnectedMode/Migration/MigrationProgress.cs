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

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    /// <summary>
    /// Data class containing information about migration progress
    /// </summary>
    internal class MigrationProgress
    {
        public MigrationProgress(int currentProject, int totalProjects, string message, bool isWarning)
        {
            CurrentProject = currentProject;
            TotalProjects = totalProjects;
            Message = message;
            IsWarning = isWarning;
        }

        public int CurrentProject { get; }

        public int TotalProjects { get; }

        public string Message { get; }

        /// <summary>
        /// Indicates whether the step is a warning or not
        /// i.e. true if the migration process didn't manage to carry out part of the cleanup
        /// sucessfully
        /// </summary>
        public bool IsWarning { get; }
    }
}
