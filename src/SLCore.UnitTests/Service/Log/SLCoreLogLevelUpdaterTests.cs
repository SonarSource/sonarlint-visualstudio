/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using System;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Log;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Log;

[TestClass]
public class SLCoreLogLevelUpdaterTests
{
    private ISLCoreServiceProvider serviceProvider;
    private IThreadHandling threadHandling;
    private ILogger logger;
    private ILogSLCoreService logService;
    private SLCoreLogLevelUpdater testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        logger = Substitute.For<ILogger>();
        logService = Substitute.For<ILogSLCoreService>();
        serviceProvider.TryGetTransientService(out Arg.Any<ILogSLCoreService>()!)
            .Returns(info =>
            {
                info[0] = logService;
                return true;
            });
        testSubject = new SLCoreLogLevelUpdater(serviceProvider, threadHandling, logger);
    }

    [TestMethod]
    public void UpdateLogLevel_BackendNotReady_DoesNotThrow()
    {
        serviceProvider.TryGetTransientService(out Arg.Any<ILogSLCoreService>()!).Returns(false);

        Action act = () => testSubject.UpdateLogLevel(DaemonLogLevel.Verbose);

        act.Should().NotThrow();
        logService.DidNotReceiveWithAnyArgs().SetLogLevel(default);
    }

    [TestMethod]
    public void UpdateLogLevel_SendsDebugWhenVerbose()
    {
        testSubject.UpdateLogLevel(DaemonLogLevel.Verbose);

        logService.Received(1).SetLogLevel(Arg.Is<SetLogLevelParams>(p => p.newLevel == SlCoreBackendLogLevel.Debug));
    }

    [TestMethod]
    public void UpdateLogLevel_RpcThrows_DoesNotPropagate()
    {
        logService.When(x => x.SetLogLevel(Arg.Any<SetLogLevelParams>())).Throw(new InvalidOperationException("rpc lost"));

        Action act = () => testSubject.UpdateLogLevel(DaemonLogLevel.Info);

        act.Should().NotThrow();
        logger.Received(1).WriteLine(Arg.Is<string>(s => s.Contains("rpc lost")));
    }
}
