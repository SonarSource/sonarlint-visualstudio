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

using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.DependencyRisks;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class ChangeDependencyRiskStatusHandlerTests
{
    private ISLCoreServiceProvider serviceProvider;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IThreadHandling threadHandling;
    private TestLogger logger;
    private IDependencyRiskSlCoreService dependencyRiskService;
    private ChangeDependencyRiskStatusHandler testSubject;
    private readonly Guid dependencyRiskId = Guid.NewGuid();
    private const string ConfigScopeId = "test-scope-id";
    private const string Comment = "test comment";
    private readonly DependencyRiskTransition transition = DependencyRiskTransition.Accept;

    [TestInitialize]
    public void TestInitialize()
    {
        serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        dependencyRiskService = Substitute.For<IDependencyRiskSlCoreService>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        logger = Substitute.ForPartsOf<TestLogger>();

        SetupServiceProvider();

        testSubject = new ChangeDependencyRiskStatusHandler(
            serviceProvider,
            activeConfigScopeTracker,
            threadHandling,
            logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ChangeDependencyRiskStatusHandler, IChangeDependencyRiskStatusHandler>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ChangeDependencyRiskStatusHandler>();

    [TestMethod]
    public void Ctor_SetsLogContext() => logger.Received(1).ForContext(Resources.LogContext_DependencyRisks, Resources.LogContext_ChangeStatus);

    [TestMethod]
    public void ChangeStatusAsync_RunsOnBackgroundThread()
    {
        testSubject.ChangeStatusAsync(dependencyRiskId, transition, Comment);

        threadHandling.ReceivedWithAnyArgs(1).RunOnBackgroundThread(default(Func<Task<bool>>));
    }

    [TestMethod]
    public async Task ChangeStatusAsync_NoActiveConfigScope_LogsErrorAndReturnsFalse()
    {
        activeConfigScopeTracker.Current.ReturnsNull();

        var result = await testSubject.ChangeStatusAsync(dependencyRiskId, transition, Comment);

        result.Should().BeFalse();
        logger.AssertPartialOutputStringExists(SLCoreStrings.ConfigScopeNotInitialized);
        await dependencyRiskService.DidNotReceiveWithAnyArgs().ChangeStatusAsync(Arg.Any<ChangeDependencyRiskStatusParams>());
    }

    [TestMethod]
    public async Task ChangeStatusAsync_ServiceUnavailable_LogsErrorAndReturnsFalse()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId));
        serviceProvider.TryGetTransientService(out IDependencyRiskSlCoreService _).Returns(x =>
        {
            x[0] = null;
            return false;
        });

        var result = await testSubject.ChangeStatusAsync(dependencyRiskId, transition, Comment);

        result.Should().BeFalse();
        logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
        await dependencyRiskService.DidNotReceiveWithAnyArgs().ChangeStatusAsync(Arg.Any<ChangeDependencyRiskStatusParams>());
    }

    [TestMethod]
    public async Task ChangeStatusAsync_CallsService_AndReturnsTrue()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId));

        var result = await testSubject.ChangeStatusAsync(dependencyRiskId, transition, Comment);

        result.Should().BeTrue();
        await dependencyRiskService.Received(1).ChangeStatusAsync(
            Arg.Is<ChangeDependencyRiskStatusParams>(x =>
                x.configurationScopeId == ConfigScopeId
                && x.dependencyRiskKey == dependencyRiskId
                && x.transition == transition.ToSlCoreDependencyRiskTransition()
                && x.comment == Comment));
    }

    [TestMethod]
    public async Task ChangeStatusAsync_ServiceThrows_LogsErrorAndReturnsFalse()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId));
        const string testErrorMessage = "test error message";
        var exception = new Exception(testErrorMessage);
        dependencyRiskService.ChangeStatusAsync(Arg.Any<ChangeDependencyRiskStatusParams>())
            .ThrowsAsync(exception);

        var result = await testSubject.ChangeStatusAsync(dependencyRiskId, transition, Comment);

        result.Should().BeFalse();
        logger.AssertPartialOutputStringExists(string.Format(Resources.ChangeDependencyRisk_Error_ChangingStatus, testErrorMessage));
    }

    private void SetupServiceProvider() =>
        serviceProvider.TryGetTransientService(out IDependencyRiskSlCoreService _).Returns(x =>
        {
            x[0] = dependencyRiskService;
            return true;
        });
}
