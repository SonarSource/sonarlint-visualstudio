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

namespace SonarLint.VisualStudio.Core.Binding;

public sealed class CloudServerRegion
{
    public const string EuRegionName = "EU";
    public const string UsRegionName = "US";

    public static readonly CloudServerRegion Eu = new(EuRegionName, new("https://sonarcloud.io/"));
    public static readonly CloudServerRegion Us = new(UsRegionName, new("https://us.sonarcloud.io/"));

    private CloudServerRegion(string name, Uri url)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Url = url ?? throw new ArgumentNullException(nameof(url));
    }

    public string Name { get; set; }

    public Uri Url { get; set; }

    /// <summary>
    /// Returns the region by the given name, ignoring the case and leading/trailing whitespaces.
    /// If null or empty is provided, defaults to <see cref="Eu"/>
    /// </summary>
    /// <param name="name">The name of the region</param>
    /// <returns>An instance of <see cref="CloudServerRegion"/></returns>
    /// <exception cref="ArgumentException">If an invalid region is provided</exception>
    public static CloudServerRegion GetRegionByName(string name) =>
        name?.ToUpper().Trim() switch
        {
            EuRegionName => Eu,
            UsRegionName => Us,
            null or "" => Eu,
            _ => throw new ArgumentOutOfRangeException(name)
        };

    public static CloudServerRegion GetRegion(bool isUsRegion) => isUsRegion ? Us : Eu;
}
