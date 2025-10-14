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
using SonarQube.Client.Models;
using IConnectionCredentials = SonarLint.VisualStudio.Core.Binding.IConnectionCredentials;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

internal interface IBindingJsonModelConverter
{
    BoundServerProject ConvertFromModel(BindingJsonModel bindingJsonModel, ServerConnection connection, string localBindingKey);

    BindingJsonModel ConvertToModel(BoundServerProject binding);

    BoundSonarQubeProject ConvertFromModelToLegacy(BindingJsonModel bindingJsonModel, IConnectionCredentials credentials);
}

[Export(typeof(IBindingJsonModelConverter))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class BindingJsonModelConverter : IBindingJsonModelConverter
{
    public BoundServerProject ConvertFromModel(BindingJsonModel bindingJsonModel, ServerConnection connection, string localBindingKey) =>
        new(localBindingKey, bindingJsonModel.ProjectKey, connection);

    public BindingJsonModel ConvertToModel(BoundServerProject binding) =>
        new()
        {
            ProjectKey = binding.ServerProjectKey,
            ServerConnectionId = binding.ServerConnection.Id,
            // for compatibility reasons:
            ServerUri = binding.ServerConnection.ServerUri,
            Organization = binding.ServerConnection is ServerConnection.SonarCloud sonarCloudConnection
                ? new SonarQubeOrganization(sonarCloudConnection.OrganizationKey, null)
                : null
        };

    public BoundSonarQubeProject ConvertFromModelToLegacy(BindingJsonModel bindingJsonModel, IConnectionCredentials credentials) =>
        new(bindingJsonModel.ServerUri,
            bindingJsonModel.ProjectKey,
            bindingJsonModel.ProjectName,
            credentials,
            bindingJsonModel.Organization);
}
