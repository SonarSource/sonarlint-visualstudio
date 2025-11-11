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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence
{
    public interface ISolutionBindingCredentialsLoader : IDisposable
    {
        void DeleteCredentials(Uri boundServerUri);

        IConnectionCredentials Load(Uri boundServerUri);

        void Save(IConnectionCredentials credentials, Uri boundServerUri);

        void Clear();
    }

    public interface ISolutionBindingCredentialsLoaderImpl : ISolutionBindingCredentialsLoader
    {
        CredentialStoreType StoreType { get; }
    }

    public interface ICredentialStoreTypeProvider
    {
        CredentialStoreType CredentialStoreType { get; set; }

        event EventHandler Changed;
    }

    [Export(typeof(ICredentialStoreTypeProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CredentialStoreTypeProvider : ICredentialStoreTypeProvider
    {
        private readonly object locker = new object();
        private CredentialStoreType credentialStoreType;

        public CredentialStoreType CredentialStoreType
        {
            get
            {
                lock (locker)
                {
                    return credentialStoreType;
                }
            }
            set
            {
                lock (locker)
                {
                    if (credentialStoreType == value)
                    {
                        return;
                    }
                    credentialStoreType = value;
                }
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler Changed;
    }

}
