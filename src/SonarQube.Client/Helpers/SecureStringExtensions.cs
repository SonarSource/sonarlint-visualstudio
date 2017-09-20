/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using System.Security;

namespace SonarQube.Client.Helpers
{
    public static class SecureStringExtensions
    {
        /// <summary>
        /// Create a read-only copy of a <see cref="SecureString"/>.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <see cref="SecureString.Copy"/> followed by <see cref="SecureString.MakeReadOnly"/>.
        /// </remarks>
        /// <returns>Read-only copy of <see cref="SecureString"/></returns>
        public static SecureString CopyAsReadOnly(this SecureString secureString)
        {
            SecureString copy = secureString.Copy();
            copy.MakeReadOnly();
            return copy;
        }

        public static bool IsEmpty(this SecureString secureString)
        {
            return secureString.Length == 0;
        }

        public static bool IsNullOrEmpty(this SecureString secureString)
        {
            return secureString == null || secureString.IsEmpty();
        }
    }
}
