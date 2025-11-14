/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

namespace SonarLint.VisualStudio.Core;

public record RepoInfo
{
    public RepoInfo(string repoKey, string folderName = null)
    {
        Key = repoKey ?? throw new ArgumentNullException(nameof(repoKey));
        FolderName = folderName ?? repoKey;
    }

    /// <summary>
    /// The repository key (a.k.a repoKey) of the rules for this language.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The rules for each language are in a separate folder in the rules website.
    /// For some languages, the folder name happens to match the repo key. The property provides the name to use in cases where they are not the same.
    /// </summary>
    public string FolderName { get; }
}
