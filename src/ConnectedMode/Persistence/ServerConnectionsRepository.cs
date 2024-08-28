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

using System.Collections.Concurrent;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using System.ComponentModel.Composition;
using System.IO;
using SonarLint.VisualStudio.Core.Persistence;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;


[Export(typeof(IServerConnectionsRepository))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class ServerConnectionsRepository : IServerConnectionsRepository
{
    private readonly ISolutionBindingCredentialsLoader credentialsLoader;
    private readonly ILogger logger;
    private readonly IJsonFileHandler jsonFileHandle;
    private readonly IServerConnectionModelMapper serverConnectionModelMapper;
    private readonly string storageFilePath;
    private const string ConnectionsFileName = "connections.json";

    private ConcurrentDictionary<string, ServerConnection> ServerConnectionsCache { get; set; }

    [ImportingConstructor]
    public ServerConnectionsRepository(
        IJsonFileHandler jsonFileHandle,
        IServerConnectionModelMapper serverConnectionModelMapper,
        ISolutionBindingCredentialsLoader credentialsLoader,
        ILogger logger) : this(jsonFileHandle,
        serverConnectionModelMapper,
        credentialsLoader,
        EnvironmentVariableProvider.Instance,
        logger) { }

    internal /* for testing */ ServerConnectionsRepository(
        IJsonFileHandler jsonFileHandle,
        IServerConnectionModelMapper serverConnectionModelMapper,
        ISolutionBindingCredentialsLoader credentialsLoader,
        IEnvironmentVariableProvider environmentVariables,
        ILogger logger)
    {
        this.jsonFileHandle = jsonFileHandle;
        this.serverConnectionModelMapper = serverConnectionModelMapper;
        this.credentialsLoader = credentialsLoader;
        this.logger = logger;
        storageFilePath = GetStorageFilePath(environmentVariables);
    }

    public bool TryGet(string connectionId, out ServerConnection serverConnection)
    {
        serverConnection = null;
        if (ServerConnectionsCache == null)
        {
            ReadServerConnectionsFile();
        }

        if (ServerConnectionsCache != null && ServerConnectionsCache.TryGetValue(connectionId, out ServerConnection foundConnection))
        {
            serverConnection = foundConnection;
            serverConnection.Credentials = credentialsLoader.Load(serverConnection.CredentialsUri);
            return true;
        }

        return false;

    }

    public IReadOnlyList<ServerConnection> GetAll()
    {
        if (ServerConnectionsCache == null)
        {
            ReadServerConnectionsFile();
        }

        return ServerConnectionsCache?.Values.ToList() ?? [];
    }

    public bool TryAdd(ServerConnection connection)
    {
        return SafeUpdateConnectionsFile(() =>
        {
            var wasAdded = ServerConnectionsCache.TryAdd(connection.Id, connection);
            if (wasAdded && connection.Credentials != null)
            { 
                credentialsLoader.Save(connection.Credentials, connection.CredentialsUri);
            }
            return wasAdded;
        });
    }

    public bool TryDelete(string connectionId)
    {
        ServerConnection removedConnection = null;
        var wasDeleted = SafeUpdateConnectionsFile(() => ServerConnectionsCache.TryRemove(connectionId, out removedConnection));
        if (wasDeleted && removedConnection != null)
        {
            credentialsLoader.DeleteCredentials(removedConnection.CredentialsUri);
        }
        return wasDeleted;
    }

    public bool TryUpdateSettingsById(string connectionId, ServerConnectionSettings connectionSettings)
    {
        return SafeUpdateConnectionsFile(() => TryUpdateConnectionSettings(connectionId, connectionSettings));
    }

    public bool TryUpdateCredentialsById(string connectionId, ICredentials credentials)
    {
        try
        {
            var wasFound = TryGet(connectionId, out ServerConnection serverConnection);
            if (!wasFound)
            {
                return false;
            }
            credentialsLoader.Save(credentials, serverConnection.CredentialsUri);
            return true;
        }
        catch (Exception ex)
        {
            logger.WriteLine(ex.Message);
            return false;
        }
    }

    private bool TryUpdateConnectionSettings(string connectionId, ServerConnectionSettings connectionSettings)
    {
        var wasFound = ServerConnectionsCache.TryGetValue(connectionId, out ServerConnection existingServerConnection);
        if (!wasFound)
        {
            return false;
        }

        existingServerConnection.Settings = connectionSettings;
        return true;
    }

    private bool SafeUpdateConnectionsFile(Func<bool> updateConnectionsCache)
    {
        try
        {
            ReadServerConnectionsFile();
            ServerConnectionsCache ??= new ConcurrentDictionary<string, ServerConnection>();

            var wasUpdated = updateConnectionsCache();
            if (!wasUpdated)
            {
                return false;
            }

            var model = serverConnectionModelMapper.GetServerConnectionsListJsonModel(ServerConnectionsCache.Values);

            return jsonFileHandle.TryWriteToFile(storageFilePath, model);
        }
        catch (Exception ex)
        {
            logger.WriteLine(ex.Message);
            return false;
        }
    }

    private static string GetStorageFilePath(IEnvironmentVariableProvider environmentVariables)
    {
        var appDataFolder = environmentVariables.GetSLVSAppDataRootPath();
        return Path.Combine(appDataFolder, ConnectionsFileName);
    }

    private void ReadServerConnectionsFile()
    {
        try
        {
            if (!jsonFileHandle.TryReadFile(storageFilePath, out ServerConnectionsListJsonModel model))
            {
                return;
            }

            ServerConnectionsCache = new ConcurrentDictionary<string, ServerConnection>();
            model.ServerConnections.ForEach(c => ServerConnectionsCache.TryAdd(c.Id, serverConnectionModelMapper.GetServerConnection(c)));
        }
        catch (Exception ex)
        {
            logger.WriteLine(ex.Message);
        }
    }
}


