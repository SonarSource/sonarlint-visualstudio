/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using NSubstitute.Extensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.TestInfrastructure;

[ExcludeFromCodeCoverage]
public class MockableInitializationProcessor(
    IThreadHandling threadHandling,
    ILogger logger,
    string owner,
    IReadOnlyCollection<IRequireInitialization> dependencies,
    Func<IThreadHandling, Task> initialization) : IInitializationProcessor
{
    private readonly InitializationProcessor implementation = new(owner, dependencies, initialization, new AsyncLockFactory(), threadHandling, logger);

    public virtual bool IsFinalized => implementation.IsFinalized;

    /// <summary>
    /// Virtual wrapper for <see cref="InitializationProcessor.InitializeAsync"/> made for using TestSpies https://nsubstitute.github.io/help/partial-subs/
    /// </summary>
    public virtual Task InitializeAsync() =>
        implementation.InitializeAsync();

    public static IInitializationProcessorFactory CreateFactory<T>(IThreadHandling threadHandling, ILogger logger, Action<MockableInitializationProcessor> configure = null)
    {
        var initializationProcessorFactory = Substitute.For<IInitializationProcessorFactory>();
        initializationProcessorFactory
            .Create<T>(
                Arg.Any<IReadOnlyCollection<IRequireInitialization>>(),
                Arg.Any<Func<IThreadHandling, Task>>())
            .Returns(info =>
            {
                var processor = Substitute.ForPartsOf<MockableInitializationProcessor>(
                    threadHandling,
                    logger,
                    typeof(T).Name,
                    (IReadOnlyCollection<IRequireInitialization>)info[0],
                    (Func<IThreadHandling, Task>)info[1]);
                configure?.Invoke(processor);
                return processor;
            });
        return initializationProcessorFactory;
    }

    public static void ConfigureWithWait(MockableInitializationProcessor substitute, TaskCompletionSource<byte> barrier) =>
        substitute.Configure()
            .InitializeAsync()
            .ReturnsForAnyArgs(async _ =>
            {
                await barrier.Task;
                await substitute.implementation.InitializeAsync();
            });
}
