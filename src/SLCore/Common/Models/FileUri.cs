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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Protocol;

namespace SonarLint.VisualStudio.SLCore.Common.Models;

[JsonConverter(typeof(FileUriConverter))]
public sealed class FileUri
{
    private readonly Uri uri;

    public FileUri(string uriString)
    {
        uri = new Uri(uriString);
    }

    public string LocalPath => uri.LocalPath;

    public override string ToString()
    {
        return uri.ToString().Replace(" ", "%20");
    }
    
    protected bool Equals(FileUri other) => Equals(uri, other.uri);

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

    public override int GetHashCode() => uri != null ? uri.GetHashCode() : 0;

    public static bool operator ==(FileUri left, FileUri right) => Equals(left, right);

    public static bool operator !=(FileUri left, FileUri right) => !Equals(left, right);
}
