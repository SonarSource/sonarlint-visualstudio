/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core.VsInfo;
using SonarLint.VisualStudio.Integration.Service;

namespace SonarLint.VisualStudio.Integration;

public interface IUserAgentProvider
{
    string UserAgent { get; }
}

[Export(typeof(IUserAgentProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class UserAgentProvider(IVsInfoProvider vsInfoProvider) : IUserAgentProvider
{
    public string UserAgent { get; } = GetUserAgent(vsInfoProvider);

    private static string GetUserAgent(IVsInfoProvider vsInfoProvider)
    {
        var vsVersionInfo = vsInfoProvider.Version;
        var vsProductName = vsVersionInfo?.DisplayName ?? "VisualStudio";
        var vsProductVersion = vsVersionInfo?.DisplayVersion ?? "version unknown";

        return ($"SonarQube for IDE (SonarLint) - Visual Studio {VersionHelper.SonarLintVersion} - {vsProductName} {vsProductVersion}").Replace("[", "").Replace("]", "");
    }
}
