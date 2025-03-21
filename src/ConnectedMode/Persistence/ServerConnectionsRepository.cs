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
using System.IO.Abstractions;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Persistence;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

[Export(typeof(IServerConnectionsRepository))]
[Export(typeof(IServerConnectionWithInvalidTokenRepository))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class ServerConnectionsRepository : IServerConnectionsRepository, IServerConnectionWithInvalidTokenRepository
{
    internal const string ConnectionsFileName = "connections.json";

    private readonly ISolutionBindingCredentialsLoader credentialsLoader;
    private readonly IFileSystem fileSystem;
    private readonly ILogger logger;
    private readonly IJsonFileHandler jsonFileHandle;
    private readonly IServerConnectionModelMapper serverConnectionModelMapper;
    private readonly string connectionsStorageFilePath;
    private readonly HashSet<string> connectionIdsWithInvalidToken = new();
    private static readonly object LockObject = new();
    private static readonly object TokenLockObject = new();

    public event EventHandler ConnectionChanged;
    public event EventHandler<ServerConnectionUpdatedEventArgs> CredentialsChanged;

    [ImportingConstructor]
    public ServerConnectionsRepository(
        IJsonFileHandler jsonFileHandle,
        IServerConnectionModelMapper serverConnectionModelMapper,
        ICredentialStoreService credentialStoreService,
        ILogger logger) : this(jsonFileHandle,
        serverConnectionModelMapper,
        new SolutionBindingCredentialsLoader(credentialStoreService),
        EnvironmentVariableProvider.Instance,
        new FileSystem(),
        logger)
    {
    }

    internal /* for testing */ ServerConnectionsRepository(
        IJsonFileHandler jsonFileHandle,
        IServerConnectionModelMapper serverConnectionModelMapper,
        ISolutionBindingCredentialsLoader credentialsLoader,
        IEnvironmentVariableProvider environmentVariables,
        IFileSystem fileSystem,
        ILogger logger)
    {
        this.jsonFileHandle = jsonFileHandle;
        this.serverConnectionModelMapper = serverConnectionModelMapper;
        this.credentialsLoader = credentialsLoader;
        this.fileSystem = fileSystem;
        this.logger = logger;
        connectionsStorageFilePath = GetStorageFilePath(environmentVariables);
    }

    public bool TryGet(string connectionId, out ServerConnection serverConnection)
    {
        serverConnection = ReadServerConnectionsFromFile()?.Find(c => c.Id == connectionId);
        if (serverConnection is null)
        {
            return false;
        }

        serverConnection.Credentials = credentialsLoader.Load(serverConnection.CredentialsUri);
        return true;
    }

    public bool TryGetAll(out IReadOnlyList<ServerConnection> serverConnections)
    {
        serverConnections = ReadServerConnectionsFromFile();

        return serverConnections != null;
    }

    public bool TryAdd(ServerConnection connectionToAdd) => SafeUpdateConnectionsFile(connections => TryAddConnection(connections, connectionToAdd));

    public bool TryDelete(string connectionId)
    {
        ServerConnection removedConnection = null;
        var wasDeleted = SafeUpdateConnectionsFile(connections =>
        {
            removedConnection = connections?.Find(c => c.Id == connectionId);
            connections?.Remove(removedConnection);

            return removedConnection != null;
        });
        if (wasDeleted)
        {
            TryDeleteCredentials(removedConnection);
            RemoveConnectionIdWithInvalidToken(connectionId);
        }

        return wasDeleted;
    }

    public bool TryUpdateSettingsById(string connectionId, ServerConnectionSettings connectionSettings)
    {
        return SafeUpdateConnectionsFile(connections => TryUpdateConnectionSettings(connections, connectionId, connectionSettings));
    }

    public bool TryUpdateCredentialsById(string connectionId, IConnectionCredentials credentials)
    {
        try
        {
            var wasFound = TryGet(connectionId, out ServerConnection serverConnection);
            if (wasFound)
            {
                credentialsLoader.Save(credentials, serverConnection.CredentialsUri);
                RemoveConnectionIdWithInvalidToken(connectionId);
                OnCredentialsChanged(serverConnection);
                return true;
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine($"Failed updating credentials: {ex.Message}");
        }
        return false;
    }

    public bool ConnectionsFileExists() => fileSystem.File.Exists(connectionsStorageFilePath);

    public void AddConnectionIdWithInvalidToken(string connectionId)
    {
        lock (TokenLockObject)
        {
            connectionIdsWithInvalidToken.Add(connectionId);
        }
    }

    public bool HasInvalidToken(string connectionId)
    {
        lock (TokenLockObject)
        {
            return connectionIdsWithInvalidToken.Contains(connectionId);
        }
    }

    internal void RemoveConnectionIdWithInvalidToken(string connectionId)
    {
        lock (TokenLockObject)
        {
            connectionIdsWithInvalidToken.Remove(connectionId);
        }
    }

    private bool TryAddConnection(List<ServerConnection> connections, ServerConnection connection)
    {
        if (connection.Credentials is null)
        {
            logger.LogVerbose($"Connection was not added.{nameof(ServerConnection.Credentials)} is not filled");
            return false;
        }

        if (connections.Find(x => x.Id == connection.Id) is not null)
        {
            logger.LogVerbose($"Connection was not added.{nameof(ServerConnection.Id)} already exist");
            return false;
        }

        try
        {
            connections.Add(connection);
            credentialsLoader.Save(connection.Credentials, connection.CredentialsUri);
            return true;
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine($"Failed adding server connection: {ex.Message}");
        }

        return false;
    }

    private void TryDeleteCredentials(ServerConnection removedConnection)
    {
        try
        {
            credentialsLoader.DeleteCredentials(removedConnection.CredentialsUri);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine($"Failed deleting credentials: {ex.Message}");
        }
    }

    private static bool TryUpdateConnectionSettings(List<ServerConnection> connections, string connectionId, ServerConnectionSettings connectionSettings)
    {
        var serverConnectionToUpdate = connections?.Find(c => c.Id == connectionId);
        if (serverConnectionToUpdate == null)
        {
            return false;
        }

        serverConnectionToUpdate.Settings = connectionSettings;
        return true;
    }

    private static string GetStorageFilePath(IEnvironmentVariableProvider environmentVariables)
    {
        var appDataFolder = environmentVariables.GetSLVSAppDataRootPath();
        return Path.Combine(appDataFolder, ConnectionsFileName);
    }

    private List<ServerConnection> ReadServerConnectionsFromFile()
    {
        try
        {
            var model = jsonFileHandle.ReadFile<ServerConnectionsListJsonModel>(connectionsStorageFilePath);
            return model.ServerConnections.Select(serverConnectionModelMapper.GetServerConnection).ToList();
        }
        catch (FileNotFoundException)
        {
            // file not existing should not be treated as an error, as it will be created at the first write
            return [];
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine($"Failed reading the {ConnectionsFileName}: {ex.Message}");
        }

        return null;
    }

    private bool SafeUpdateConnectionsFile(Func<List<ServerConnection>, bool> tryUpdateConnectionModels)
    {
        lock (LockObject)
        {
            try
            {
                var serverConnections = ReadServerConnectionsFromFile();

                if (tryUpdateConnectionModels(serverConnections))
                {
                    var model = serverConnectionModelMapper.GetServerConnectionsListJsonModel(serverConnections);
                    var wasSaved = jsonFileHandle.TryWriteToFile(connectionsStorageFilePath, model);
                    if (wasSaved)
                    {
                        OnConnectionChanged();
                    }
                    return wasSaved;
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine($"Failed updating the {ConnectionsFileName}: {ex.Message}");
            }

            return false;
        }
    }

    private void OnConnectionChanged() => ConnectionChanged?.Invoke(this, EventArgs.Empty);

    private void OnCredentialsChanged(ServerConnection serverConnection) => CredentialsChanged?.Invoke(this, new ServerConnectionUpdatedEventArgs(serverConnection));
}
