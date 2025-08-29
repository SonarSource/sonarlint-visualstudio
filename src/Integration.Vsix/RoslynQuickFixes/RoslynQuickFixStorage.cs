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
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.RoslynAnalyzerServer;

namespace SonarLint.VisualStudio.Integration.Vsix.RoslynQuickFixes;

[Export(typeof(IRoslynQuickFixStorageWriter))]
[Export(typeof(IRoslynQuickFixProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class RoslynQuickFixStorage : IRoslynQuickFixStorageWriter, IRoslynQuickFixProvider
{
    private readonly Dictionary<Guid, RoslynQuickFixApplicationImpl> cache = new(); // todo clear the cache

    public void Add(
        Guid id,
        RoslynQuickFixApplicationImpl impl) =>
        cache[id] = impl;

    public bool TryGet(Guid id, out IQuickFixApplication roslynQuickFix)
    {
        if (cache.TryGetValue(id, out var quickFixImplementation))
        {
            roslynQuickFix = new RoslynQuickFixApplication(quickFixImplementation);
            return true;
        }

        roslynQuickFix = null;
        return false;
    }
}
