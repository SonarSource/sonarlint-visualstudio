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

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

public interface IDependencyRisksStore
{
    string CurrentConfigurationScope { get; }

    IReadOnlyCollection<IDependencyRisk> GetAll();

    void Set(IEnumerable<IDependencyRisk> dependencyRisks, string configurationScopeId);

    void Reset();

    void Update(DependencyRisksUpdate dependencyRisksUpdate);

    event EventHandler DependencyRisksChanged;
}

public class DependencyRisksUpdate(
    string configurationScope,
    IEnumerable<IDependencyRisk> added,
    IEnumerable<IDependencyRisk> updated,
    IEnumerable<Guid> closed)
{
    public string ConfigurationScope { get; } = !string.IsNullOrEmpty(configurationScope) ? configurationScope : throw new ArgumentNullException(nameof(configurationScope));
    public IEnumerable<IDependencyRisk> Added { get; } = added ?? throw new ArgumentNullException(nameof(added));
    public IEnumerable<IDependencyRisk> Updated { get; } = updated ?? throw new ArgumentNullException(nameof(updated));
    public IEnumerable<Guid> Closed { get; } = closed ?? throw new ArgumentNullException(nameof(closed));
}

[Export(typeof(IDependencyRisksStore))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class DependencyRisksStore : IDependencyRisksStore
{
    private Dictionary<Guid, IDependencyRisk> currentDependencyRisks = new();
    private readonly object lockObject = new();
    private string currentConfigurationScope;

    public string CurrentConfigurationScope
    {
        get
        {
            lock (lockObject)
            {
                return currentConfigurationScope;
            }
        }
    }

    public IReadOnlyCollection<IDependencyRisk> GetAll()
    {
        lock (lockObject)
        {
            return currentDependencyRisks.Values.ToList();
        }
    }

    public void Set(IEnumerable<IDependencyRisk> dependencyRisks, string configurationScopeId)
    {
        lock (lockObject)
        {
            currentDependencyRisks = dependencyRisks.ToDictionary(x => x.Id);
            currentConfigurationScope = configurationScopeId;
        }

        RaiseDependencyRisksChanged();
    }

    public void Reset()
    {
        lock (lockObject)
        {
            if (currentConfigurationScope == null && currentDependencyRisks.Count <= 0)
            {
                return;
            }

            currentConfigurationScope = null;
            currentDependencyRisks.Clear();
        }

        RaiseDependencyRisksChanged();
    }

    public void Update(DependencyRisksUpdate dependencyRisksUpdate)
    {
        bool hasChanges;

        lock (lockObject)
        {
            if (dependencyRisksUpdate.ConfigurationScope != currentConfigurationScope)
            {
                Debug.Fail("Unexpected configuration scope");
                return;
            }

            UpdateStore(dependencyRisksUpdate, out hasChanges);
        }

        if (hasChanges)
        {
            RaiseDependencyRisksChanged();
        }
    }

    private void UpdateStore(DependencyRisksUpdate dependencyRisksUpdate, out bool hasChanges)
    {
        hasChanges = false;
        foreach (var closedId in dependencyRisksUpdate.Closed)
        {
            if (!currentDependencyRisks.Remove(closedId))
            {
                Debug.Fail("Dependency Risk update: attempt to remove a non-existent risk");
                continue;
            }
            hasChanges = true;
        }

        foreach (var updatedRisk in dependencyRisksUpdate.Updated)
        {
            if (!currentDependencyRisks.ContainsKey(updatedRisk.Id))
            {
                Debug.Fail("Dependency Risk  update: attempt to update a non-existent risk");
                continue;
            }
            currentDependencyRisks[updatedRisk.Id] = updatedRisk;
            hasChanges = true;
        }

        foreach (var addedRisk in dependencyRisksUpdate.Added)
        {
            if (currentDependencyRisks.ContainsKey(addedRisk.Id))
            {
                Debug.Fail("Dependency Risk update: attempt to add an already existing risk");
                continue;
            }
            currentDependencyRisks[addedRisk.Id] = addedRisk;
            hasChanges = true;
        }
    }

    public event EventHandler DependencyRisksChanged;

    private void RaiseDependencyRisksChanged() => DependencyRisksChanged?.Invoke(this, EventArgs.Empty);
}
