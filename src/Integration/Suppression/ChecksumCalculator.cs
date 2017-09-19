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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SonarLint.VisualStudio.Integration.Suppression
{
    /// <summary>
    /// Calculates the checksum for an issue using the same method as the SonarQube server
    /// </summary>
    /// <remarks>
    /// For the corresponding code in SL IntelliJ see src\main\java\org\sonarlint\intellij\issue\LiveIssue.java::checksum
    /// </remarks>
    internal static class ChecksumCalculator
    {
        public static string Calculate(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            string content = Regex.Replace(text, "\\s", ""); // strip whitespace
            using (MD5 md5Digest = MD5.Create())
            {
                return EncodeHex(md5Digest.ComputeHash(Encoding.UTF8.GetBytes(content)));
            }
        }

        /// <summary>
        /// Converts an array of bytes into a string containing the hexadecimal representation of each byte
        /// </summary>
        public static string EncodeHex(byte[] data)
        {
            const string HexAlphabet = "0123456789abcdef";

            int length = data.Length;
            char[] encodedData = new char[length * 2];
            int targetIndex = 0;

            for (int sourceIndex = 0; sourceIndex < length; sourceIndex++)
            {
                encodedData[targetIndex++] = HexAlphabet[((240 & data[sourceIndex]) >> 4)];
                encodedData[targetIndex++] = HexAlphabet[15 & data[sourceIndex]];
            }

            return new string(encodedData);
        }
    }
}
