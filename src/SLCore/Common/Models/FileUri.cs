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

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using SonarLint.VisualStudio.SLCore.Protocol;

namespace SonarLint.VisualStudio.SLCore.Common.Models;

[TypeConverter(typeof(FileUriConverter))]
public sealed class FileUri
{
    private readonly Uri uri;
    private static readonly char[] Rfc3986ReservedCharsToEncoding = ['?', '#', '[', ']', '@'];

    public FileUri(string uriString)
    {
        var unescapedUri = Uri.UnescapeDataString(uriString);
        uri = new Uri(unescapedUri);
    }

    public string LocalPath => uri.LocalPath;

    public override string ToString()
    {
        var escapedUri = Uri.EscapeUriString(uri.ToString());

        return EscapeRfc3986ReservedCharacters(escapedUri);
    }

    /// <summary>
    /// The backend (SlCore) uses java, in which the Uri follows the RFC 3986 protocol.
    /// The <see cref="Uri.EscapeUriString"/> does not escape the reserved characters, that's why they are escaped here.
    /// See https://learn.microsoft.com/en-us/dotnet/api/system.uri.escapeuristring?view=netframework-4.7.2
    /// </summary>
    /// <param name="stringToEscape"></param>
    /// <returns></returns>
    private static string EscapeRfc3986ReservedCharacters(string stringToEscape)
    {
        var charsToEscape = Rfc3986ReservedCharsToEncoding.Where(stringToEscape.Contains).ToList();

        return !charsToEscape.Any()
            ? stringToEscape
            : charsToEscape.Aggregate(stringToEscape, (current, charToEscape) => current.Replace(charToEscape.ToString(), Uri.HexEscape(charToEscape)));
    }

    [ExcludeFromCodeCoverage]
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((FileUri)obj);
    }

    private bool Equals(FileUri other) => Equals(uri, other.uri);

    public override int GetHashCode() => uri != null ? uri.GetHashCode() : 0;

    public static bool operator ==(FileUri left, FileUri right) => Equals(left, right);

    public static bool operator !=(FileUri left, FileUri right) => !Equals(left, right);
}
