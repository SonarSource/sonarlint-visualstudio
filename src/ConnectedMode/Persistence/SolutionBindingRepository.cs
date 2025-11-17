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
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

[Export(typeof(ISolutionBindingRepository))]
[Export(typeof(ILegacySolutionBindingRepository))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class SolutionBindingRepository : ISolutionBindingRepository, ILegacySolutionBindingRepository
{
    private readonly IBindingJsonModelConverter bindingJsonModelConverter;
    private readonly ISolutionBindingCredentialsLoader credentialsLoader;
    private readonly ILogger logger;
    private readonly IServerConnectionsRepository serverConnectionsRepository;
    private readonly ISolutionBindingFileLoader solutionBindingFileLoader;
    private readonly IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider;

    [ImportingConstructor]
    public SolutionBindingRepository(
        IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider,
        IBindingJsonModelConverter bindingJsonModelConverter,
        IServerConnectionsRepository serverConnectionsRepository,
        ICredentialStoreService credentialStoreService,
        ILogger logger)
        : this(unintrusiveBindingPathProvider,
            bindingJsonModelConverter,
            serverConnectionsRepository,
            new SolutionBindingFileLoader(logger),
            new SolutionBindingCredentialsLoader(credentialStoreService),
            logger)
    {
    }

    internal /* for testing */ SolutionBindingRepository(
        IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider,
        IBindingJsonModelConverter bindingJsonModelConverter,
        IServerConnectionsRepository serverConnectionsRepository,
        ISolutionBindingFileLoader solutionBindingFileLoader,
        ISolutionBindingCredentialsLoader credentialsLoader,
        ILogger logger)
    {
        this.unintrusiveBindingPathProvider = unintrusiveBindingPathProvider;
        this.serverConnectionsRepository = serverConnectionsRepository;
        this.bindingJsonModelConverter = bindingJsonModelConverter;
        this.solutionBindingFileLoader = solutionBindingFileLoader ?? throw new ArgumentNullException(nameof(solutionBindingFileLoader));
        this.credentialsLoader = credentialsLoader ?? throw new ArgumentNullException(nameof(credentialsLoader));
        this.logger = logger;
    }

    BoundSonarQubeProject ILegacySolutionBindingRepository.Read(string configFilePath)
    {
        var bindingJsonModel = ReadBindingFile(configFilePath);
        return bindingJsonModel switch
        {
            null => null,
            not null => bindingJsonModelConverter.ConvertFromModelToLegacy(bindingJsonModel, credentialsLoader.Load(bindingJsonModel.ServerUri))
        };
    }

    public BoundServerProject Read(string configFilePath) => Convert(ReadBindingFile(configFilePath), configFilePath);

    public bool Write(string configFilePath, BoundServerProject binding)
    {
        _ = binding ?? throw new ArgumentNullException(nameof(binding));

        if (string.IsNullOrEmpty(configFilePath))
        {
            return false;
        }

        if (!solutionBindingFileLoader.Save(configFilePath, bindingJsonModelConverter.ConvertToModel(binding)))
        {
            return false;
        }

        BindingUpdated?.Invoke(this, EventArgs.Empty);

        return true;
    }

    public bool DeleteBinding(string localBindingKey)
    {
        var bindingPath = unintrusiveBindingPathProvider.GetBindingPath(localBindingKey);
        if (!solutionBindingFileLoader.DeleteBindingDirectory(bindingPath))
        {
            return false;
        }
        BindingDeleted?.Invoke(this, new LocalBindingKeyEventArgs(localBindingKey));
        return true;
    }

    public event EventHandler BindingUpdated;
    public event EventHandler<LocalBindingKeyEventArgs> BindingDeleted;

    public IEnumerable<BoundServerProject> List()
    {
        if (!serverConnectionsRepository.TryGetAll(out var connections))
        {
            throw new InvalidOperationException("Could not retrieve all connections.");
        }

        var bindingConfigPaths = unintrusiveBindingPathProvider.GetBindingPaths();

        foreach (var bindingConfigPath in bindingConfigPaths)
        {
            var bindingJsonModel = ReadBindingFile(bindingConfigPath);

            if (bindingJsonModel == null)
            {
                logger.LogVerbose($"Skipped {bindingConfigPath} because it could not be read");
                continue;
            }

            if (connections.FirstOrDefault(c => c.Id == bindingJsonModel.ServerConnectionId) is not { } serverConnection)
            {
                logger.LogVerbose($"Skipped {bindingConfigPath} because connection {bindingJsonModel.ServerConnectionId} doesn't exist");
                continue;
            }

            var boundServerProject = Convert(bindingJsonModel, serverConnection, bindingConfigPath);

            yield return boundServerProject;
        }
    }

    private BindingJsonModel ReadBindingFile(string configFilePath) =>
        solutionBindingFileLoader.Load(configFilePath);

    private BoundServerProject Convert(BindingJsonModel bindingJsonModel, string configFilePath) =>
        bindingJsonModel is not null && serverConnectionsRepository.TryGet(bindingJsonModel.ServerConnectionId, out var connection)
            ? Convert(bindingJsonModel, connection, configFilePath)
            : null;

    private BoundServerProject Convert(BindingJsonModel bindingJsonModel, ServerConnection connection, string configFilePath) =>
        bindingJsonModelConverter.ConvertFromModel(bindingJsonModel, connection, unintrusiveBindingPathProvider.GetBindingKeyFromPath(configFilePath));
}
