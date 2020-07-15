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

namespace SonarLint.VisualStudio.Integration.Suppression
{
    /// <summary>
    /// Calculates the checksum for an issue using the same method as the SonarQube server
    /// </summary>
    /// <remarks>
    /// For the corresponding code in SL IntelliJ see src\main\java\org\sonarlint\intellij\issue\LiveIssue.java::checksum
    /// </remarks>
    public static class ChecksumCalculator
    {
        public static string Calculate(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            string content = Regex.Replace(text, "\\s", ""); // strip whitespace
            using (var md5Digest = new Md5Managed())
            {
                return EncodeHex(md5Digest.ComputeHash(Encoding.UTF8.GetBytes(content)));
            }
        }

        /// <summary>
        /// Converts an array of bytes into a string containing the hexadecimal representation of each byte
        /// </summary>
        private static string EncodeHex(byte[] data)
        {
            const string HexAlphabet = "0123456789abcdef";

            const byte HighOrderBitsOn = 240; //  128 + 64 + 32 + 16
            const byte LowOrderBitsOn = 15;   //  8 + 4 + 2 + 1

            int length = data.Length;
            char[] encodedData = new char[length * 2];
            int targetIndex = 0;

            for (int sourceIndex = 0; sourceIndex < length; sourceIndex++)
            {
                // Converting an 8-bit binary to the corresponding 2-character hex representation:
                // The binary representation is AAAABBBB where
                //  AAAA is a number between 0 and 15 that represents the first hex char.
                //  BBBB is a number between 0 and 15 that represents the second hex char.
                // To convert, extract AAAA and BBBB in turn and look up the corresponding hex representation (0-9a-f)
                encodedData[targetIndex++] = HexAlphabet[((HighOrderBitsOn & data[sourceIndex]) >> 4)]; // extract AAAA----
                encodedData[targetIndex++] = HexAlphabet[LowOrderBitsOn & data[sourceIndex]]; // extract ----BBBB
            }

            return new string(encodedData);
        }
    }
}
