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
using System.Security.Cryptography;
using System.Text;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    internal static class CMakeHashCalculator
    {
        /// <summary>
        /// Generates a GUID from the supplied input
        /// </summary>
        /// <remarks>The algorithm is equivalent to the one used by VS2019/2022 when calculating the
        /// CMake macros "workspaceHash" and "projectHash".
        /// <para>
        /// The VS2019 implementation is in:
        /// * assembly: \Common7\IDE\CommonExtensions\Microsoft\EBF\Microsoft.VisualStudio.Workspace.ExternalBuildFramework.Impl.dll
        /// * method: HashHelper.GetGuid
        /// Unfortunately, it isn't currently available in a NuGet package.
        /// </para>
        /// </remarks>
        public static Guid CalculateVS2019Guid(string input)
        {
            // UTF8-encode the input
            var data = Encoding.UTF8.GetBytes(input);
            byte[] guidBytes = new byte[16];

            // Get the SHA256 hash of the encoded input and take the first 16 bytes
            using (var algorithm = SHA256.Create())
            {
                var hash = algorithm.ComputeHash(data);
                Array.Copy(hash, guidBytes, 16);
            }

            // Modify the eight byte to 0100----
            SetBits(ref guidBytes[7], 0b0100_0000);
            ClearBits(ref guidBytes[7], 0b1011_0000);

            // Modify the ninth byte to 10------
            SetBits(ref guidBytes[8], 0b1000_0000);
            ClearBits(ref guidBytes[8], 0b0100_0000);

            // Turn the byte array into a Guid and return it
            return new Guid(guidBytes);
        }

        // 1-s in the mask indicate the bits to set/clear
        // 0-s in the mask indicate the bits that should not be modified
        private static void SetBits(ref byte b, byte mask) => b |= mask;
        private static void ClearBits(ref byte b, byte mask) => b &= (byte)~mask;
    }
}
