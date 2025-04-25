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

using System.Diagnostics.CodeAnalysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.TestInfrastructure;

[ExcludeFromCodeCoverage]
public class MockableInitializationProcessor(IThreadHandling threadHandling, ILogger logger) : IInitializationProcessor
{
    private readonly InitializationProcessor implementation = new(new AsyncLockFactory(), threadHandling,logger);

    public virtual bool IsFinalized => implementation.IsFinalized;

    /// <summary>
    /// Virtual wrapper for <see cref="InitializationProcessor.InitializeAsync"/> made for using TestSpies https://nsubstitute.github.io/help/partial-subs/
    /// </summary>
    public virtual Task InitializeAsync(
        string owner,
        IReadOnlyCollection<IRequireInitialization> dependencies,
        Func<IThreadHandling, Task> initialization) =>
        implementation.InitializeAsync(owner, dependencies, initialization);
}
