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
    IReadOnlyCollection<IDependencyRisk> GetAll();

    void Set(IEnumerable<IDependencyRisk> dependencyRisks);

    void Remove(IDependencyRisk dependencyRisk);

    void Reset();

    event EventHandler DependencyRisksChanged;
}

[Export(typeof(IDependencyRisksStore))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class DependencyRisksStore : IDependencyRisksStore
{
    private readonly List<IDependencyRisk> currentDependencyRisks = new();
    private readonly object lockObject = new();

    public IReadOnlyCollection<IDependencyRisk> GetAll()
    {
        lock (lockObject)
        {
            return currentDependencyRisks.ToList().AsReadOnly();
        }
    }

    public void Set(IEnumerable<IDependencyRisk> dependencyRisks)
    {
        lock (lockObject)
        {
            currentDependencyRisks.Clear();
            currentDependencyRisks.AddRange(dependencyRisks);
        }

        RaiseDependencyRisksChanged();
    }

    public void Remove(IDependencyRisk dependencyRisk)
    {
        lock (lockObject)
        {
            if (!currentDependencyRisks.Remove(dependencyRisk))
            {
                return;
            }
        }

        RaiseDependencyRisksChanged();
    }

    public void Reset()
    {
        lock (lockObject)
        {
            if (currentDependencyRisks.Count <= 0)
            {
                return;
            }

            currentDependencyRisks.Clear();
        }

        RaiseDependencyRisksChanged();
    }

    public event EventHandler DependencyRisksChanged;

    private void RaiseDependencyRisksChanged() => DependencyRisksChanged?.Invoke(this, EventArgs.Empty);
}
