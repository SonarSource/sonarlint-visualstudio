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

namespace SonarLint.VisualStudio.Core
{
    public interface ISourceControlWorkspace
    {
        /// <summary>
        /// Returns true/false if the given file is ignored under source control, i.e. via .gitignore
        /// </summary>
        /// <remarks>
        /// Will return true if the file is not under source control at all, or if the file is under source control but ignored.
        /// Will return false if the file is under source control and not ignored.
        /// </remarks>
        bool IsFileIgnoredInSourceControl(string fullFilePath);
    }
}
