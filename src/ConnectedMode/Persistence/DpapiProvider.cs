/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

public interface IDpapiProvider
{
    SecureString UnprotectBase64String(string encryptedString);

    string GetProtectedBase64String(SecureString unprotectedString);
}

[Export(typeof(IDpapiProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
[ExcludeFromCodeCoverage] // not really testable, calls static methods from library
public class DpapiProvider(ILogger log) : IDpapiProvider
{
    public SecureString UnprotectBase64String(string encryptedString)
    {
        byte[] tokenUnprotectedBytes = null;
        string unprotectedString;
        try
        {
            tokenUnprotectedBytes =
                ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedString),
                    null,
                    DataProtectionScope.CurrentUser);
            unprotectedString = Encoding.UTF8.GetString(tokenUnprotectedBytes);
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            log.WriteLine(e.ToString());
            return null;
        }
        finally
        {
            Clear(tokenUnprotectedBytes);
        }

        return unprotectedString.ToSecureString();
    }

    public string GetProtectedBase64String(SecureString unprotectedString)
    {
        byte[] tokenUnprotected = null;
        byte[] tokenProtected;
        try
        {
            tokenUnprotected = Encoding.UTF8.GetBytes(unprotectedString.ToUnsecureString());
            tokenProtected = ProtectedData.Protect(
                tokenUnprotected,
                null,
                DataProtectionScope.CurrentUser);
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            log.WriteLine(e.ToString());
            return null;
        }
        finally
        {
            Clear(tokenUnprotected);
        }

        return Convert.ToBase64String(tokenProtected);
    }

    private static void Clear(byte[] array)
    {
        if (array is null)
        {
            return;
        }
        Array.Clear(array, 0, array.Length);
    }
}
