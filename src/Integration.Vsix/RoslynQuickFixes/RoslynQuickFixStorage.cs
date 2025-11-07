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
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.RoslynAnalyzerServer;

namespace SonarLint.VisualStudio.Integration.Vsix.RoslynQuickFixes;

[Export(typeof(IRoslynQuickFixStorageWriter))]
[Export(typeof(IRoslynQuickFixProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class RoslynQuickFixStorage : IRoslynQuickFixStorageWriter, IRoslynQuickFixProvider
{
    private readonly object locker = new();
    private readonly Dictionary<Guid, RoslynQuickFixApplicationImpl> cache = new();

    [method: ImportingConstructor]
    public RoslynQuickFixStorage(IActiveConfigScopeTracker configScopeTracker)
    {
        configScopeTracker.CurrentConfigurationScopeChanged += ConfigScopeTracker_OnCurrentConfigurationScopeChanged; // it's okay to miss initial events here, this is only used for cache cleanup
    }

    private void ConfigScopeTracker_OnCurrentConfigurationScopeChanged(object sender, ConfigurationScopeChangedEventArgs e)
    {
        if (!e.DefinitionChanged)
        {
            return;
        }

        ClearCache();
    }

    private void ClearCache()
    {
        lock (locker)
        {
            cache.Clear();
        }
    }

    public void Add(RoslynQuickFixApplicationImpl impl)
    {
        lock (locker)
        {
            cache[impl.Id] = impl;
        }
    }

    public void Clear(string filePath)
    {
        lock (locker)
        {
            var toRemove = cache
                .Where(x => x.Value.FilePath.Equals(filePath))
                .Select(x => x.Key)
                .ToList();
            foreach (var keyToRemove in toRemove)
            {
                cache.Remove(keyToRemove);
            }
        }
    }

    public bool TryGet(Guid id, out IQuickFixApplication roslynQuickFix)
    {
        lock (locker)
        {
            if (cache.TryGetValue(id, out var quickFixImplementation))
            {
                roslynQuickFix = new RoslynQuickFixApplication(quickFixImplementation);
                return true;
            }
        }

        roslynQuickFix = null;
        return false;
    }
}
