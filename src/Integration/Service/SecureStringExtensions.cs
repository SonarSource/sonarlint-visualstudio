//-----------------------------------------------------------------------
// <copyright file="SecureStringExtensions.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Security;

namespace SonarLint.VisualStudio.Integration.Service
{
    internal static class SecureStringExtensions
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
