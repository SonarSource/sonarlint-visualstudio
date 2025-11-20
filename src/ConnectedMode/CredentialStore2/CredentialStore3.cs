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
using System.Windows;
using Newtonsoft.Json;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.CredentialStore2;

[Export(typeof(ISolutionBindingCredentialsLoaderImpl))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class DpapiCurrentUserCredentialsLoader(
    IFileSystemService fileSystem,
    ILogger log) : ISolutionBindingCredentialsLoaderImpl, IDisposable
{
    private readonly string storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLVS_Credentials", "credentials_dpapicurrentuser.json");
    private bool disposed = false;
    private readonly ILogger log = log.ForVerboseContext(nameof(DpapiMasterPasswordCredentialsLoader));

    public CredentialStoreType StoreType => CredentialStoreType.DPAPI;

    public void DeleteCredentials(Uri targetUri)
    {
        ThrowIfDisposed();

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

    public IConnectionCredentials Load(Uri boundServerUri)
    {
        ThrowIfDisposed();

        string encryptedToken = ReadToken(boundServerUri);

        if (encryptedToken == null)
        {
            return null;
        }

        var secureToken = GetSecureString(encryptedToken);

        if (secureToken == null)
        {
            return null;
        }

        return new TokenAuthCredentials(secureToken);
    }

    public void Save(IConnectionCredentials credentials, Uri boundServerUri)
    {
        ThrowIfDisposed();

        if (credentials is not ITokenCredentials tokenCredentials)
        {
            throw new ArgumentException("Only token credentials are supported", nameof(credentials));
        }

        byte[] tokenUnprotected = null;
        byte[] tokenProtected = null;
        try
        {
            tokenUnprotected = Encoding.UTF8.GetBytes(tokenCredentials.Token.ToUnsecureString());
            tokenProtected = ProtectedData.Protect(
                tokenUnprotected,
                null,
                DataProtectionScope.CurrentUser);
        }
        finally
        {
            Clear(tokenUnprotected);
        }

        WriteToken(boundServerUri, Convert.ToBase64String(tokenProtected));
    }

    public void Clear() => fileSystem.File.Delete(storagePath);

    private SecureString GetSecureString(string encryptedToken)
    {
        SecureString secureToken = new SecureString();
        byte[] tokenUnprotectedBytes = null;
        string unprotectedString;
        try
        {
            tokenUnprotectedBytes =
                ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedToken),
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

        foreach (var character in unprotectedString)
        {
            secureToken.AppendChar(character);
        }
        secureToken.MakeReadOnly();

        return secureToken;
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
            throw new ObjectDisposedException(nameof(DpapiMasterPasswordCredentialsLoader));
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
    }
}
