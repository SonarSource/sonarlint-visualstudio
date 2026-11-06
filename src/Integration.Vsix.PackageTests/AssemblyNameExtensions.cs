/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using System.Reflection;

namespace SonarLint.VisualStudio.Integration.Vsix.PackageTests;

internal static class AssemblyNameExtensions
{
    public static string PublicKeyTokenText(this AssemblyName name) =>
        string.Concat((name.GetPublicKeyToken() ?? []).Select(b => b.ToString("x2")));

    public static bool IsStrongNamed(this AssemblyName name) => name.PublicKeyTokenText().Length > 0;

    public static bool HasSameIdentity(this AssemblyName expected, AssemblyName actual, int versionFieldCount) =>
        expected.Version.ToString(versionFieldCount) == actual.Version.ToString(versionFieldCount)
        && expected.PublicKeyTokenText() == actual.PublicKeyTokenText();

    public static AssemblyName TryGetAssemblyName(string path)
    {
        try
        {
            return AssemblyName.GetAssemblyName(path);
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }
}
