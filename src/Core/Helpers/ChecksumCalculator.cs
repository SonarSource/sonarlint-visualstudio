/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Text;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Utilities.Encoders;

namespace SonarLint.VisualStudio.Core.Helpers
{
    /// <summary>
    /// Calculates the checksum for an issue using the same method as the SonarQube server
    /// </summary>
    public static class ChecksumCalculator
    {
        public static string Calculate(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var content = Regex.Replace(text, "\\s", ""); // strip whitespace
            var data = Encoding.UTF8.GetBytes(content);

            var hash = new MD5Digest();
            hash.BlockUpdate(data, 0, data.Length);
            var result = new byte[hash.GetDigestSize()];
            hash.DoFinal(result, 0);
            return Hex.ToHexString(result);
        }
    }
}
