/*
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

using System.ComponentModel.Composition;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.CredentialStore2;

internal class CredentialDto
{
    public Uri Uri { get; init; }
    public string EncryptedToken { get; init; }
}

[Export(typeof(ISolutionBindingCredentialsLoader))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class CredentialStore2 : ISolutionBindingCredentialsLoader, IDisposable
{
    private readonly IFileSystemService fileSystem;
    private readonly IAsyncLock asyncLock;
    private readonly string storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLVS_Credentials", "credentials.json");
    private readonly SecureString masterPassword;
    private bool disposed = false;

    [ImportingConstructor]
    public CredentialStore2(IFileSystemService fileSystem, IAsyncLockFactory asyncLockFactory)
    {
        this.fileSystem = fileSystem;
        asyncLock = asyncLockFactory.Create();

        masterPassword = new SecureString();
        foreach (char c in "testpassword")
        {
            masterPassword.AppendChar(c);
        }
        masterPassword.MakeReadOnly();
    }

    public void DeleteCredentials(Uri targetUri)
    {
        ThrowIfDisposed();

        using (asyncLock.Acquire())
        {
            if (targetUri == null || !fileSystem.File.Exists(storagePath))
            {
                return;
            }

            var allText = fileSystem.File.ReadAllText(storagePath);
            var dictionary = JsonConvert.DeserializeObject<Dictionary<Uri, CredentialDto>>(allText);

            if (dictionary != null && dictionary.Remove(targetUri))
            {
                var serializedDictionary = JsonConvert.SerializeObject(dictionary);
                fileSystem.File.WriteAllText(storagePath, serializedDictionary);
            }
        }
    }

    public IConnectionCredentials Load(Uri boundServerUri)
    {
        ThrowIfDisposed();

        using (asyncLock.Acquire())
        {
            string encryptedToken = ReadToken(boundServerUri);

            if (encryptedToken == null)
            {
                return null;
            }

            var secureToken = GetSecureString(encryptedToken);
            return new TokenAuthCredentials(secureToken);
        }
    }

    public void Save(IConnectionCredentials credentials, Uri boundServerUri)
    {
        ThrowIfDisposed();

        if (credentials is not ITokenCredentials tokenCredentials)
        {
            throw new ArgumentException("Only token credentials are supported", nameof(credentials));
        }

        using (asyncLock.Acquire())
        {
            var tokenProtectedBytes = UseMasterPasswordSafe(masterPasswordBytes =>
            {
                byte[] tokenUnprotected = null;
                byte[] tokenProtected = null;
                try
                {
                    tokenUnprotected = Encoding.UTF8.GetBytes(tokenCredentials.Token.ToUnsecureString());
                    tokenProtected = ProtectedData.Protect(
                        tokenUnprotected,
                        masterPasswordBytes,
                        DataProtectionScope.LocalMachine);
                }
                finally
                {
                    Clear(tokenUnprotected);
                }

                return tokenProtected;
            });

            WriteToken(boundServerUri, Convert.ToBase64String(tokenProtectedBytes));
        }
    }

    private SecureString GetSecureString(string encryptedToken)
    {
        SecureString secureToken = new SecureString();
        byte[] tokenUnprotectedBytes = null;
        string unprotectedString;
        try
        {
            tokenUnprotectedBytes = UseMasterPasswordSafe(masterPasswordBytes =>
                ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedToken),
                    masterPasswordBytes,
                    DataProtectionScope.LocalMachine));
            unprotectedString = Encoding.UTF8.GetString(tokenUnprotectedBytes);
        }
        finally
        {
            Clear(tokenUnprotectedBytes);
        }

        foreach (var character in unprotectedString)
        {
            secureToken.AppendChar(character);
        }
        secureToken.MakeReadOnly();

        return secureToken;
    }

    private byte[] UseMasterPasswordSafe(Func<byte[], byte[]> operation)
    {
        byte[] masterPasswordUnprotectedBytes = null;
        byte[] result = null;
        try
        {
            masterPasswordUnprotectedBytes = Encoding.UTF8.GetBytes(masterPassword.ToUnsecureString());
            result = operation(masterPasswordUnprotectedBytes);
        }
        finally
        {
            Clear(masterPasswordUnprotectedBytes);
        }
        return result;
    }

    private string ReadToken(Uri targetUri)
    {
        if (fileSystem.File.Exists(storagePath))
        {
            var allText = fileSystem.File.ReadAllText(storagePath);
            var dictionary = JsonConvert.DeserializeObject<Dictionary<Uri, CredentialDto>>(allText);
            if (dictionary != null && dictionary.TryGetValue(targetUri, out var dto))
            {
                return dto.EncryptedToken;
            }
        }

        return null;
    }

    private void WriteToken(Uri targetUri, string token)
    {
        Dictionary<Uri, CredentialDto> dictionary;

        if (fileSystem.File.Exists(storagePath))
        {
            var allText = fileSystem.File.ReadAllText(storagePath);
            dictionary = JsonConvert.DeserializeObject<Dictionary<Uri, CredentialDto>>(allText) ?? new Dictionary<Uri, CredentialDto>();
        }
        else
        {
            dictionary = new Dictionary<Uri, CredentialDto>();

            var directory = Path.GetDirectoryName(storagePath);
            if (!fileSystem.Directory.Exists(directory))
            {
                fileSystem.Directory.CreateDirectory(directory);
            }
        }

        dictionary[targetUri] = new CredentialDto { Uri = targetUri, EncryptedToken = token };
        var serializedDictionary = JsonConvert.SerializeObject(dictionary);

        fileSystem.File.WriteAllText(storagePath, serializedDictionary);
    }

    private void Clear(byte[] array)
    {
        if (array is null)
        {
            return;
        }
        Array.Clear(array, 0, array.Length);
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CredentialStore2));
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        asyncLock.Dispose();
        masterPassword?.Dispose();
        disposed = true;
    }
}
