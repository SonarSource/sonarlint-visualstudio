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

using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    /// <summary>
    /// Data Class containing information about a binding
    /// </summary>
    public class BoundConnectionInfo
    {
        public Uri ServerUri { get; set; }

        public string Organization { get; set; }
    }

    internal class BoundConnectionInfoUriComparer : IEqualityComparer<BoundConnectionInfo>
    {
        public bool Equals(BoundConnectionInfo x, BoundConnectionInfo y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null && y == null) { return true; }

            if (x == null ^ y == null)
            {
                return false;
            }

            return x.ServerUri == y.ServerUri;
        }

        public int GetHashCode(BoundConnectionInfo obj)
        {
            return obj.ServerUri.GetHashCode();
        }
    }
}
