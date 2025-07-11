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
using System.Runtime.CompilerServices;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

[Export(typeof(ISolutionBindingCredentialsLoader))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class AggregatingSolutionBindingCredentialsLoader : ISolutionBindingCredentialsLoader
{
    private readonly ICredentialStoreTypeProvider credentialStoreTypeProvider;
    private readonly ILogger logger;
    private readonly Dictionary<CredentialStoreType, ISolutionBindingCredentialsLoaderImpl> solutionBindingCredentialsLoaderImpls;

    [ImportingConstructor]
    public AggregatingSolutionBindingCredentialsLoader(
        [ImportMany] IEnumerable<ISolutionBindingCredentialsLoaderImpl> impls,
        ICredentialStoreTypeProvider credentialStoreTypeProvider,
        ILogger logger)
    {
        this.credentialStoreTypeProvider = credentialStoreTypeProvider;
        this.logger = logger.ForVerboseContext(nameof(AggregatingSolutionBindingCredentialsLoader));
        solutionBindingCredentialsLoaderImpls = impls.ToDictionary(x => x.StoreType, y => y);
    }

    public void DeleteCredentials(Uri boundServerUri) =>
        SafeExecute(() =>
        {
            solutionBindingCredentialsLoaderImpls[credentialStoreTypeProvider.CredentialStoreType].DeleteCredentials(boundServerUri);
        });

    public IConnectionCredentials Load(Uri boundServerUri)
    {
        IConnectionCredentials credentials = null;
        SafeExecute(() =>
        {
            credentials = solutionBindingCredentialsLoaderImpls[credentialStoreTypeProvider.CredentialStoreType].Load(boundServerUri);
        });

        return credentials;
    }

    public void Save(IConnectionCredentials credentials, Uri boundServerUri) =>
        SafeExecute(() =>
        {
            solutionBindingCredentialsLoaderImpls[credentialStoreTypeProvider.CredentialStoreType].Save(credentials, boundServerUri);
        });

    private void SafeExecute(Action act, [CallerMemberName] string caller = "")
    {
        logger.LogVerbose(GetContext(caller), "Executing Credential Operation");

        try
        {
            act();
        }
        catch (Exception e)
        {
            logger.LogVerbose(GetContext(caller), e.ToString());
        }
    }

    private MessageLevelContext GetContext(string caller) => new() { VerboseContext = [credentialStoreTypeProvider.CredentialStoreType.ToString(), caller] };
}
