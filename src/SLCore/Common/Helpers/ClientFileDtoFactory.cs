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
using System.Text;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;

namespace SonarLint.VisualStudio.SLCore.Common.Helpers;

[Export(typeof(IClientFileDtoFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class ClientFileDtoFactory : IClientFileDtoFactory
{
    public ClientFileDto Create(string fullPath, string configScopeId, string rootPath)
    {
        var ideRelativePath = GetRelativePath(rootPath, fullPath);
        var uri = new FileUri(fullPath);
        return new ClientFileDto(uri, ideRelativePath, configScopeId, null, Encoding.UTF8.WebName, fullPath);
    }

    private static string GetRelativePath(string root, string fullPath)
    {
        return fullPath.Substring(root.Length);
    }
}
