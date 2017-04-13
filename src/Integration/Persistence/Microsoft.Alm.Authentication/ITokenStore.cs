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
using System;

namespace Microsoft.Alm.Authentication
{
    public interface ITokenStore
    {
        /// <summary>
        /// Deletes a <see cref="Token"/> from the underlying storage.
        /// </summary>
        /// <param name="targetUri">The key identifying which token is being deleted.</param>
        void DeleteToken(Uri targetUri);
        /// <summary>
        /// Reads a <see cref="Token"/> from the underlying storage.
        /// </summary>
        /// <param name="targetUri">The key identifying which token to read.</param>
        /// <param name="token">A <see cref="Token"/> if successful; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if successful; otherwise <see langword="false"/>.</returns>
        bool ReadToken(Uri targetUri, out Token token);
        /// <summary>
        /// Writes a <see cref="Token"/> to the underlying storage.
        /// </summary>
        /// <param name="targetUri">
        /// Unique identifier for the token, used when reading back from storage.
        /// </param>
        /// <param name="token">The <see cref="Token"/> to be written.</param>
        void WriteToken(Uri targetUri, Token token);
    }
}

