/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Helpers;
using SonarLint.VisualStudio.ConnectedMode.Hotspots;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Hotspots;

[TestClass]
public class ServerHotspotStoreUpdaterTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ServerHotspotStoreUpdater, IServerHotspotStoreUpdater>(
            MefTestHelpers.CreateExport<ISonarQubeService>(),
            MefTestHelpers.CreateExport<IServerQueryInfoProvider>(),
            MefTestHelpers.CreateExport<IServerHotspotStore>(),
            MefTestHelpers.CreateExport<ICancellableActionRunner>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ServerHotspotStoreUpdater>();
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow(null, "branch")]
    [DataRow("projectKey", null)]
    public async Task UpdateAll_MissingServerQueryInfo_NoOp(string projectKey, string branchName)
    {
        // Server query info is not available -> give up
        var queryInfo = CreateQueryInfoProvider(projectKey, branchName);
        var server = new Mock<ISonarQubeService>();
        var store = new Mock<IServerHotspotStore>();

        var testSubject = CreateTestSubject(queryInfo.Object, server.Object, store.Object);

        await testSubject.UpdateAllServerHotspotsAsync();

        queryInfo.Verify(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()), Times.Once());
        server.Invocations.Should().HaveCount(0);
        store.Invocations.Should().HaveCount(0);
    }

    [TestMethod]
    public async Task UpdateAll_HasServerQueryInfo_ServerQueriedAndStoreUpdated()
    {
        // Happy path - fetch and update
        var queryInfo = CreateQueryInfoProvider("project", "branch");

        const string hotspotKey = "hotspot1";
        var hotspot = CreateHotspot(hotspotKey);
        var server = CreateSonarQubeService("project", "branch", hotspot);

        var store = new Mock<IServerHotspotStore>();

        var testSubject = CreateTestSubject(queryInfo.Object, server.Object, store.Object);

        await testSubject.UpdateAllServerHotspotsAsync();

        queryInfo.Verify(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()), Times.Once());
        server.Verify(x => x.SearchHotspotsAsync("project", "branch", It.IsAny<CancellationToken>()),
            Times.Once());
        store.Verify(x => x.Refresh(It.Is<IList<SonarQubeHotspot>>(list => list.Single().HotspotKey.Equals(hotspotKey))), Times.Once);

        server.Invocations.Should().HaveCount(1);
        store.Invocations.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task UpdateAll_RunOnBackgroundThreadInActionRunner()
    {
        var callSequence = new List<string>();

        var queryInfo = new Mock<IServerQueryInfoProvider>();
        var threadHandling = new Mock<IThreadHandling>();
        var actionRunner = new Mock<ICancellableActionRunner>();

        queryInfo.Setup(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(x => callSequence.Add("GetProjectKeyAndBranchAsync"));

        threadHandling.Setup(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task<bool>> action) =>
            {
                callSequence.Add("RunOnBackgroundThread");
                return action();
            });

        actionRunner.Setup(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()))
            .Returns((Func<CancellationToken, Task> action) =>
            {
                callSequence.Add("RunAction");
                return action(CancellationToken.None);
            });

        var testSubject = CreateTestSubject(queryInfo.Object,
            threadHandling: threadHandling.Object,
            actionRunner: actionRunner.Object);

        await testSubject.UpdateAllServerHotspotsAsync();

        threadHandling.Invocations.Should().HaveCount(1);
        actionRunner.Invocations.Should().HaveCount(1);
        queryInfo.Invocations.Should().HaveCount(1);

        callSequence.Should().ContainInOrder("RunOnBackgroundThread", "RunAction", "GetProjectKeyAndBranchAsync");
    }

    [TestMethod]
    public void UpdateAll_CriticalExpression_NotHandled()
    {
        var queryInfo = new Mock<IServerQueryInfoProvider>();
        queryInfo.Setup(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()))
            .Throws(new DivideByZeroException("thrown in a test"));

        var logger = new TestLogger(logToConsole: true);

        var testSubject = CreateTestSubject(queryInfo.Object, logger: logger);

        Func<Task> operation = testSubject.UpdateAllServerHotspotsAsync;
        operation.Should().Throw<DivideByZeroException>().And.Message.Should().Be("thrown in a test");

        logger.AssertPartialOutputStringDoesNotExist("thrown in a test");
    }

    [TestMethod]
    public void UpdateAll_NonCriticalExpression_IsSuppressed()
    {
        var queryInfo = new Mock<IServerQueryInfoProvider>();
        queryInfo.Setup(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("thrown in a test"));

        var logger = new TestLogger(logToConsole: true);

        var testSubject = CreateTestSubject(queryInfo.Object, logger: logger);

        Func<Task> operation = testSubject.UpdateAllServerHotspotsAsync;
        operation.Should().NotThrow();

        logger.AssertPartialOutputStringExists("thrown in a test");
    }

    [TestMethod]
    public void UpdateAll_OperationCancelledException_CancellationMessageLogged()
    {
        var queryInfo = new Mock<IServerQueryInfoProvider>();
        queryInfo.Setup(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException("thrown in a test"));

        var logger = new TestLogger(logToConsole: true);

        var testSubject = CreateTestSubject(queryInfo.Object, logger: logger);

        Func<Task> operation = testSubject.UpdateAllServerHotspotsAsync;
        operation.Should().NotThrow();

        logger.AssertPartialOutputStringDoesNotExist("thrown in a test");
        logger.AssertOutputStringExists(Resources.Hotspots_FetchOperationCancelled);
    }

    [TestMethod]
    public void Dispose_CallsActionRunnerDispose()
    {
        var actionRunnerMock = new Mock<ICancellableActionRunner>();

        var testSubject = CreateTestSubject(actionRunner: actionRunnerMock.Object);
        
        testSubject.Dispose();
        
        actionRunnerMock.Verify(runner => runner.Dispose(), Times.Once);
    }

    private static IServerHotspotStoreUpdater CreateTestSubject(IServerQueryInfoProvider queryInfo = null,
            ISonarQubeService server = null,
            IServerHotspotStore store = null,
            ILogger logger = null,
            ICancellableActionRunner actionRunner = null,
            IThreadHandling threadHandling = null)
        {
            store ??= Mock.Of<IServerHotspotStore>();
            server ??= Mock.Of<ISonarQubeService>();
            queryInfo ??= Mock.Of<IServerQueryInfoProvider>();
            logger ??= new TestLogger(logToConsole: true);
            threadHandling ??= new NoOpThreadHandler();
            actionRunner ??= new SynchronizedCancellableActionRunner(logger);

            return new ServerHotspotStoreUpdater(server, store, queryInfo, actionRunner, threadHandling, logger);
        }

        private static Mock<IServerQueryInfoProvider> CreateQueryInfoProvider(string projectKey, string branchName)
        {
            var mock = new Mock<IServerQueryInfoProvider>();
            mock.Setup(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>())).ReturnsAsync((projectKey, branchName));
            return mock;
        }

        private static Mock<ISonarQubeService> CreateSonarQubeService(string projectKey, string branchName, params SonarQubeHotspotSearch[] hotspotsToReturn)
        {
            var mock = new Mock<ISonarQubeService>();
            mock.Setup(x => x.SearchHotspotsAsync(projectKey, branchName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(hotspotsToReturn);

            return mock;
        }

        private static SonarQubeHotspotSearch CreateHotspot(string id)
        {
            return new SonarQubeHotspotSearch(id,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                default,
                default);
        }
}
