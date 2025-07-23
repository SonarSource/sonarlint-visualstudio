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

using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.DependencyRisks;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class ShowDependencyRiskInBrowserHandlerTests
{
    private ISLCoreServiceProvider slCoreServiceProvider;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IDependencyRiskSlCoreService dependencyRiskService;
    private IThreadHandling threadHandling;
    private TestLogger logger;
    private ShowDependencyRiskInBrowserHandler testSubject;
    private readonly Guid dependencyRiskId = Guid.NewGuid();
    private const string ConfigScopeId = "test-scope-id";

    [TestInitialize]
    public void TestInitialize()
    {
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        dependencyRiskService = Substitute.For<IDependencyRiskSlCoreService>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        logger = Substitute.ForPartsOf<TestLogger>();

        SetupServiceProvider();

        testSubject = new ShowDependencyRiskInBrowserHandler(
            slCoreServiceProvider,
            activeConfigScopeTracker,
            threadHandling,
            logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ShowDependencyRiskInBrowserHandler, IShowDependencyRiskInBrowserHandler>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ShowDependencyRiskInBrowserHandler>();

    [TestMethod]
    public void Ctor_SetsLogContext() =>
        logger.Received(1).ForContext(Resources.LogContext_DependencyRisks, Resources.LogContext_ShowInBrowser);

    [TestMethod]
    public void ShowInBrowser_RunsOnBackgroundThread()
    {
        testSubject.ShowInBrowser(dependencyRiskId);

        threadHandling.ReceivedWithAnyArgs(1).RunOnBackgroundThread(default(Func<Task<int>>));
    }

    [TestMethod]
    public void ShowInBrowser_NoActiveConfigScope_LogsError()
    {
        activeConfigScopeTracker.Current.ReturnsNull();

        testSubject.ShowInBrowser(dependencyRiskId);

        logger.AssertPartialOutputStringExists(SLCoreStrings.ConfigScopeNotInitialized);
        dependencyRiskService.DidNotReceiveWithAnyArgs().OpenDependencyRiskInBrowserAsync(default);
    }

    [TestMethod]
    public void ShowInBrowser_ServiceUnavailable_LogsError()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId));
        slCoreServiceProvider.TryGetTransientService(out IDependencyRiskSlCoreService _).Returns(x =>
        {
            x[0] = null;
            return false;
        });

        testSubject.ShowInBrowser(dependencyRiskId);

        logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
        dependencyRiskService.DidNotReceiveWithAnyArgs().OpenDependencyRiskInBrowserAsync(Arg.Any<OpenDependencyRiskInBrowserParams>());
    }

    [TestMethod]
    public void ShowInBrowser_CallsService()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId));

        testSubject.ShowInBrowser(dependencyRiskId);

        dependencyRiskService.Received(1).OpenDependencyRiskInBrowserAsync(
            Arg.Is<OpenDependencyRiskInBrowserParams>(p =>
                p.configScopeId == ConfigScopeId &&
                p.dependencyRiskKey == dependencyRiskId));
    }

    [TestMethod]
    public void ShowInBrowser_ServiceThrows_LogsError()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId));
        const string testErrorMessage = "test error message";
        var exception = new Exception(testErrorMessage);
        dependencyRiskService.OpenDependencyRiskInBrowserAsync(Arg.Any<OpenDependencyRiskInBrowserParams>())
            .ThrowsAsync(exception);

        testSubject.ShowInBrowser(dependencyRiskId);

        logger.AssertPartialOutputStringExists(string.Format(Resources.ShowDependencyRisk_Error_ShowingInBrowser, testErrorMessage));
    }

    private void SetupServiceProvider() =>
        slCoreServiceProvider.TryGetTransientService(out IDependencyRiskSlCoreService _).Returns(x =>
        {
            x[0] = dependencyRiskService;
            return true;
        });
}
