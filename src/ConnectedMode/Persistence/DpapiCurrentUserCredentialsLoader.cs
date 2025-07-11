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
using System.IO;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

[Export(typeof(ISolutionBindingCredentialsLoaderImpl))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class DpapiCurrentUserCredentialsLoader(
    IFileSystemService fileSystem,
    IDpapiProvider dpapiProvider,
    ILogger log) : ISolutionBindingCredentialsLoaderImpl
{
    private const CredentialStoreType DpapiStoreType = CredentialStoreType.DPAPI;
    internal class DpapiCredentialsStorageJsonModel : Dictionary<Uri, DpapiCredentialJsonModel>;
    internal record DpapiCredentialJsonModel([property: JsonRequired]string EncryptedToken);

    private readonly string storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLVS_Credentials", "credentials.json");
    private readonly ILogger log = log
        .ForContext(PersistenceStrings.CredentialsLoader_LogContext, DpapiStoreType.ToString())
        .ForVerboseContext(nameof(DpapiCurrentUserCredentialsLoader));

    public CredentialStoreType StoreType => DpapiStoreType;

    public void DeleteCredentials(Uri boundServerUri)
    {
        if (ReadModel() is { } model && model.Remove(boundServerUri))
        {
            WriteModel(model);
        }
    }

    public IConnectionCredentials Load(Uri boundServerUri)
    {
        if (ReadModel() is not {} model || !model.TryGetValue(boundServerUri, out var dto) )
        {
            log.WriteLine(PersistenceStrings.DpapiCurrentUserCredentialsLoader_NoCredentials, boundServerUri);
            return null;
        }

        if (dpapiProvider.UnprotectBase64String(dto.EncryptedToken) is not { } secureToken)
        {
            return null;
        }

        return new TokenAuthCredentials(secureToken);
    }

    public void Save(IConnectionCredentials credentials, Uri boundServerUri)
    {
        if (credentials is not ITokenCredentials tokenCredentials)
        {
            throw new ArgumentException(PersistenceStrings.DpapiCurrentUserCredentialsLoader_Save_NotATokenError, nameof(credentials));
        }

        var model = ReadModel() ?? new();
        model[boundServerUri] = new DpapiCredentialJsonModel(dpapiProvider.GetProtectedBase64String(tokenCredentials.Token));

        WriteModel(model);
    }

    private DpapiCredentialsStorageJsonModel ReadModel()
    {
        if (!fileSystem.File.Exists(storagePath)
            || fileSystem.File.ReadAllText(storagePath) is not { } allText)
        {
            log.LogVerbose("Credentials storage file does not exist or is empty");
            return null;
        }

        return JsonConvert.DeserializeObject<DpapiCredentialsStorageJsonModel>(allText);
    }

    private void WriteModel(DpapiCredentialsStorageJsonModel model)
    {
        var serializedDictionary = JsonConvert.SerializeObject(model);
        fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(storagePath));
        fileSystem.File.WriteAllText(storagePath, serializedDictionary);
    }
}
