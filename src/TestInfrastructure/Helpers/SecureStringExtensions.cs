//-----------------------------------------------------------------------
// <copyright file="SecureStringExtensions.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal static class SecureStringForTestExtensions
    {
        // Copied from http://blogs.msdn.com/b/fpintos/archive/2009/06/12/how-to-properly-convert-securestring-to-string.aspx
        #region Conversion

        /// <summary>
        /// Create a read-only <see cref="SecureString"/> from this <see cref="string"/>.
        /// </summary>
        /// <returns>Read-only <see cref="SecureString"/></returns>
        public static SecureString ToSecureString(this string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            var secure = new SecureString();
            foreach (char c in str)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }

        /// <summary>
        /// WARNING: This will create plain text <see cref="string"/> version of the <see cref="SecureString"/> in
        /// memory which is not encrypted. This could lead to leaking of sensitive information and other security
        /// vulnerabilities - heavy caution is advised.
        /// </summary>
        [SecurityCritical]
        public static string ToUnsecureString(this SecureString secureString)
        {
            if (secureString == null)
            {
                throw new ArgumentNullException(nameof(secureString));
            }

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        #endregion
    }
}
