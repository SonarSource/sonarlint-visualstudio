/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.SLCore.Service.Project.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

[Export(typeof(ISolutionBindingRepository))]
[Export(typeof(ILegacySolutionBindingRepository))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class SolutionBindingRepository : ISolutionBindingRepository, ILegacySolutionBindingRepository
{
    private readonly ISolutionBindingFileLoader solutionBindingFileLoader;
    private readonly ISolutionBindingCredentialsLoader credentialsLoader;
    private readonly IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider;
    private readonly IServerConnectionsRepository serverConnectionsRepository;
    private readonly IBindingDtoConverter bindingDtoConverter;
    private readonly ILogger logger;

    [ImportingConstructor]
    public SolutionBindingRepository(IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider,
        IBindingDtoConverter bindingDtoConverter,
        IServerConnectionsRepository serverConnectionsRepository,
        ICredentialStoreService credentialStoreService,
        ILogger logger)
        : this(unintrusiveBindingPathProvider,
            bindingDtoConverter,
            serverConnectionsRepository,
            new SolutionBindingFileLoader(logger),
            new SolutionBindingCredentialsLoader(credentialStoreService),
            logger)
    {
    }

    internal /* for testing */ SolutionBindingRepository(IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider,
        IBindingDtoConverter bindingDtoConverter,
        IServerConnectionsRepository serverConnectionsRepository,
        ISolutionBindingFileLoader solutionBindingFileLoader,
        ISolutionBindingCredentialsLoader credentialsLoader,
        ILogger logger)
    {
        this.unintrusiveBindingPathProvider = unintrusiveBindingPathProvider;
        this.serverConnectionsRepository = serverConnectionsRepository;
        this.bindingDtoConverter = bindingDtoConverter;
        this.solutionBindingFileLoader = solutionBindingFileLoader ?? throw new ArgumentNullException(nameof(solutionBindingFileLoader));
        this.credentialsLoader = credentialsLoader ?? throw new ArgumentNullException(nameof(credentialsLoader));
        this.logger = logger;
    }

    BoundSonarQubeProject ILegacySolutionBindingRepository.Read(string configFilePath)
    {
        var bindingDto = ReadBindingFile(configFilePath);
        return bindingDto switch
        {
            null => null,
            not null => bindingDtoConverter.ConvertFromDtoToLegacy(bindingDto, credentialsLoader.Load(bindingDto.ServerUri))
        };
    }

    public BoundServerProject Read(string configFilePath)
    {
        return Convert(ReadBindingFile(configFilePath), configFilePath);
    }

    public bool Write(string configFilePath, BoundServerProject binding)
    {
        _ = binding ?? throw new ArgumentNullException(nameof(binding));

        if (string.IsNullOrEmpty(configFilePath))
        {
            return false;
        }

        if (!solutionBindingFileLoader.Save(configFilePath, bindingDtoConverter.ConvertToDto(binding)))
        {
            return false;
        }

        BindingUpdated?.Invoke(this, EventArgs.Empty);
            
        return true;
    }

    public event EventHandler BindingUpdated;

    public IEnumerable<BoundServerProject> List()
    {
        if (!serverConnectionsRepository.TryGetAll(out var connections))
        {
            throw new NotImplementedException();
        }
        
        var serverConnections = connections.ToDictionary(k => k.Id, v => v);
        
        var bindingConfigPaths = unintrusiveBindingPathProvider.GetBindingPaths();

        foreach (var bindingConfigPath in bindingConfigPaths)
        {
            var bindingDto = ReadBindingFile(bindingConfigPath);

            if (bindingDto == null)
            {
                logger.LogVerbose($"Skipped {bindingConfigPath} because it could not be read");
                continue;
            }

            if (!serverConnections.TryGetValue(bindingDto.ServerConnectionId, out var serverConnection))
            {
                logger.LogVerbose($"Skipped {bindingConfigPath} because connection {bindingDto.ServerConnectionId} doesn't exist");
                continue;
            }

            var boundServerProject = Convert(bindingDto, serverConnection, bindingConfigPath);

            yield return boundServerProject;
        }
    }
    
    private BindingDto ReadBindingFile(string configFilePath)
    {
        var bound = solutionBindingFileLoader.Load(configFilePath);

        if (bound is null)
        {
            return null;
        }

        Debug.Assert(!bound.Profiles?.ContainsKey(Core.Language.Unknown) ?? true,
            "Not expecting the deserialized binding config to contain the profile for an unknown language");

        return bound;

    }

    private BoundServerProject Convert(BindingDto bindingDto, string configFilePath) =>
        bindingDto is not null && serverConnectionsRepository.TryGet(bindingDto.ServerConnectionId, out var connection)
            ? Convert(bindingDto, connection, configFilePath)
            : null;

    private BoundServerProject Convert(BindingDto bindingDto, ServerConnection connection, string configFilePath) => 
        bindingDtoConverter.ConvertFromDto(bindingDto, connection, unintrusiveBindingPathProvider.GetBindingKeyFromPath(configFilePath));
}
