﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Runtime.InteropServices;
using System.Security;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Http;

internal static class SecureStringExtensions
{
    internal static SecureString ToSecureString(this string str)
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

    // Copied from http://blogs.msdn.com/b/fpintos/archive/2009/06/12/how-to-properly-convert-securestring-to-string.aspx
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
}
