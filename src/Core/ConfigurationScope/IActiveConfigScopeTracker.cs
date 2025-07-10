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

namespace SonarLint.VisualStudio.Core.ConfigurationScope;

public interface IActiveConfigScopeTracker : IDisposable
{
    ConfigurationScope Current { get; }

    void SetCurrentConfigScope(string id, string connectionId = null, string sonarProjectKey = null);

    void Reset();

    void RemoveCurrentConfigScope();

    bool TryUpdateRootOnCurrentConfigScope(string id, string root, string commandsBaseDir);

    bool TryUpdateAnalysisReadinessOnCurrentConfigScope(string id, bool isReady);

    event EventHandler<ConfigurationScopeChangedEventArgs> CurrentConfigurationScopeChanged;
}

public class ConfigurationScopeChangedEventArgs(bool definitionChanged) : EventArgs
{
    /// <summary>
    /// True if a new configuration scope was defined, false if the current scope was updated
    /// </summary>
    public bool DefinitionChanged { get; } = definitionChanged;
}

public record ConfigurationScope(
    string Id,
    string ConnectionId = null,
    string SonarProjectId = null,
    string RootPath = null,
    string CommandsBaseDir = null,
    bool IsReadyForAnalysis = false)
{
    public string Id { get; } = Id ?? throw new ArgumentNullException(nameof(Id));
}



