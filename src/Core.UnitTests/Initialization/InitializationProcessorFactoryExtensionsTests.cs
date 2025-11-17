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

using SonarLint.VisualStudio.Core.Initialization;

namespace SonarLint.VisualStudio.Core.UnitTests.Initialization;

[TestClass]
public class InitializationProcessorFactoryExtensionsTests
{
    private IRequireInitialization[] dependencies;
    private IInitializationProcessorFactory processorFactory;

    [TestInitialize]
    public void TestInitialize()
    {
        dependencies = [Substitute.For<IRequireInitialization>()];
        processorFactory = Substitute.For<IInitializationProcessorFactory>();
    }

    [TestMethod]
    public async Task CreateAndStart_Action_CallsFactoryAndStartsProcessor()
    {
        var initialization = Substitute.For<Action>();

        var initializationProcessor = InitializationProcessorFactoryExtensions.CreateAndStart<InitializationProcessorFactoryExtensionsTests>(processorFactory, dependencies, initialization);

        initializationProcessor.Should().NotBeNull();
        initializationProcessor.Received().InitializeAsync();
        processorFactory.Received().Create<InitializationProcessorFactoryExtensionsTests>(dependencies, Arg.Any<Func<IThreadHandling, Task>>());
        var func = (Func<IThreadHandling, Task>)processorFactory.ReceivedCalls().Single().GetArguments()[1]!;
        await func(Substitute.For<IThreadHandling>());
        initialization.Received(1).Invoke();
    }

    [TestMethod]
    public async Task CreateAndStart_ParameterlessFunc_CallsFactoryAndStartsProcessor()
    {
        var initialization = Substitute.For<Func<Task>>();

        var initializationProcessor = InitializationProcessorFactoryExtensions.CreateAndStart<InitializationProcessorFactoryExtensionsTests>(processorFactory, dependencies, initialization);

        initializationProcessor.Should().NotBeNull();
        initializationProcessor.Received().InitializeAsync();
        processorFactory.Received().Create<InitializationProcessorFactoryExtensionsTests>(dependencies, Arg.Any<Func<IThreadHandling, Task>>());
        var func = (Func<IThreadHandling, Task>)processorFactory.ReceivedCalls().Single().GetArguments()[1]!;
        await func(Substitute.For<IThreadHandling>());
        initialization.Received(1).Invoke();
    }


    [TestMethod]
    public async Task CreateAndStart_CallsFactoryAndStartsProcessor()
    {
        var initialization = Substitute.For<Func<IThreadHandling, Task>>();
        var threadHandling = Substitute.For<IThreadHandling>();

        var initializationProcessor = InitializationProcessorFactoryExtensions.CreateAndStart<InitializationProcessorFactoryExtensionsTests>(processorFactory, dependencies, initialization);

        initializationProcessor.Should().NotBeNull();
        initializationProcessor.Received().InitializeAsync();
        processorFactory.Received().Create<InitializationProcessorFactoryExtensionsTests>(dependencies, Arg.Any<Func<IThreadHandling, Task>>());
        var func = (Func<IThreadHandling, Task>)processorFactory.ReceivedCalls().Single().GetArguments()[1]!;
        await func(threadHandling);
        initialization.Received(1).Invoke(threadHandling);
    }
}
