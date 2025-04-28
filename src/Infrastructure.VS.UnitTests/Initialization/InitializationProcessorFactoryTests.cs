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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Initialization;

[TestClass]
public class InitializationProcessorFactoryTests
{
    private IAsyncLockFactory asyncLockFactory;
    private IThreadHandling threadHandling;
    private ILogger logger;
    private InitializationProcessorFactory testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        asyncLockFactory = Substitute.For<IAsyncLockFactory>();
        threadHandling = Substitute.For<IThreadHandling>();
        logger = Substitute.For<ILogger>();
        testSubject = new InitializationProcessorFactory(asyncLockFactory, threadHandling, logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<InitializationProcessorFactory, IInitializationProcessorFactory>(
            MefTestHelpers.CreateExport<IAsyncLockFactory>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<InitializationProcessorFactory>();

    [TestMethod]
    public void Create_ReturnsNonNull() =>
        // for more tests see InitializationProcessorTests
        testSubject.Create<InitializationProcessorFactoryTests>([], _ => Task.CompletedTask).Should().NotBeNull();
}
