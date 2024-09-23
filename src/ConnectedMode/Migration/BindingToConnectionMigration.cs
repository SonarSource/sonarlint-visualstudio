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
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.ConnectedMode.Migration;

public interface IBindingToConnectionMigration
{
    Task MigrateAllBindingsToServerConnectionsIfNeededAsync();
}

/// <summary>
/// Migrates the information about the server connection that, in the past, was stored into the binding.config file to the new connections.json file.
/// The binding.config file is also updated to contain a ServerConnectionId that references the new connection.
/// </summary>
[Export(typeof(IBindingToConnectionMigration))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class BindingToConnectionMigration : IBindingToConnectionMigration
{
    private readonly IFileSystem fileSystem;
    private readonly IServerConnectionsRepository serverConnectionsRepository;
    private readonly ILegacySolutionBindingRepository legacyBindingRepository;
    private readonly ISolutionBindingRepository solutionBindingRepository;
    private readonly IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;

    [ImportingConstructor]
    public BindingToConnectionMigration(
        IServerConnectionsRepository serverConnectionsRepository,
        ILegacySolutionBindingRepository legacyBindingRepository,
        ISolutionBindingRepository solutionBindingRepository,
        IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider,
        ILogger logger) : 
        this(
            new FileSystem(),
            serverConnectionsRepository,
            legacyBindingRepository,
            solutionBindingRepository,
            unintrusiveBindingPathProvider,
            ThreadHandling.Instance,
            logger)
    { }

    internal /* for testing */ BindingToConnectionMigration(
        IFileSystem fileSystem,
        IServerConnectionsRepository serverConnectionsRepository,
        ILegacySolutionBindingRepository legacyBindingRepository,
        ISolutionBindingRepository solutionBindingRepository,
        IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider,
        IThreadHandling threadHandling, 
        ILogger logger) 
    {
        this.fileSystem = fileSystem;
        this.serverConnectionsRepository = serverConnectionsRepository;
        this.legacyBindingRepository = legacyBindingRepository;
        this.solutionBindingRepository = solutionBindingRepository;
        this.unintrusiveBindingPathProvider = unintrusiveBindingPathProvider;
        this.threadHandling = threadHandling;
        this.logger = logger;
    }

    public Task MigrateAllBindingsToServerConnectionsIfNeededAsync()
    {
        return threadHandling.RunOnBackgroundThread(MigrateBindingToServerConnectionIfNeeded);
    }

    private void MigrateBindingToServerConnectionIfNeeded()
    {
        if (fileSystem.File.Exists(serverConnectionsRepository.ConnectionsStorageFilePath))
        {
            return;
        }

        logger.WriteLine(MigrationStrings.ConnectionMigration_StartMigration);
        foreach (var bindingPath in unintrusiveBindingPathProvider.GetBindingPaths())
        {
            MigrateBindingToServerConnection(bindingPath);
        }
    }

    private void MigrateBindingToServerConnection(string bindingFilePath)
    {
        try
        {
            if (legacyBindingRepository.Read(bindingFilePath) is not {} legacyBoundProject)
            {
                logger.WriteLine(string.Format(MigrationStrings.ConnectionMigration_BindingNotMigrated, bindingFilePath, $"{nameof(legacyBoundProject)} was not found"));
                return;
            }

            if (MigrateServerConnection(legacyBoundProject) is {} serverConnection)
            {
                MigrateBinding(bindingFilePath, legacyBoundProject, serverConnection);
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(string.Format(MigrationStrings.ConnectionMigration_BindingNotMigrated, bindingFilePath, ex.Message));
        }
    }

    private ServerConnection MigrateServerConnection(BoundSonarQubeProject legacyBoundProject)
    {
        var serverConnection = ServerConnection.FromBoundSonarQubeProject(legacyBoundProject);
        serverConnection.Credentials = legacyBoundProject.Credentials;

        if (serverConnectionsRepository.TryGet(serverConnection.Id, out _))
        {
            logger.WriteLine(string.Format(MigrationStrings.ConnectionMigration_ExistingServerConnectionNotMigrated, serverConnection.Id));
            return serverConnection;
        }

        if (serverConnectionsRepository.TryAdd(serverConnection))
        {
            return serverConnection;
        }

        logger.WriteLine(string.Format(MigrationStrings.ConnectionMigration_ServerConnectionNotMigrated, serverConnection.Id));
        return null;
    }

    private void MigrateBinding(string bindingPath, BoundSonarQubeProject legacyBoundProject, ServerConnection serverConnection)
    {
        var boundServerProject = BoundServerProject.FromBoundSonarQubeProject(legacyBoundProject, bindingPath, serverConnection);
        solutionBindingRepository.Write(bindingPath, boundServerProject);
    }
}
