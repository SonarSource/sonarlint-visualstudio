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
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI;

[TestClass]
public class BindingControllerAdapterTests
{
    private IBindingController bindingController;
    private ISolutionInfoProvider solutionInfoProvider;
    private TestLogger testLogger;
    private BindingControllerAdapter testSubject;
    private IConnectionForBindingProvider connectionForBindingProvider;
    private CancellationTokenSource cancellationTokenSource;

    [TestInitialize]
    public void TestInitialize()
    {
        bindingController = Substitute.For<IBindingController>();
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        testLogger = new TestLogger();
        connectionForBindingProvider = Substitute.For<IConnectionForBindingProvider>();
        cancellationTokenSource = new CancellationTokenSource();
        testSubject = new BindingControllerAdapter(bindingController, solutionInfoProvider, testLogger);
    }

    [TestMethod]
    public void Ctor_SetsCorrectLogContext()
    {
        var logger = Substitute.For<ILogger>();

        _ = new BindingControllerAdapter(bindingController, solutionInfoProvider, logger);

        logger.Received().ForContext(Resources.ConnectedModeLogContext, Resources.ConnectedModeBindingLogContext);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<BindingControllerAdapter, IBindingControllerAdapter>(
            MefTestHelpers.CreateExport<IBindingController>(),
            MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<BindingControllerAdapter>();

    [DynamicData(nameof(RequestsWithNullProjectKey))]
    [DataTestMethod]
    public async Task ValidateAndBindAsync_NoProjectKey_ReturnsNoProjectKeyResult(BindingRequest request)
    {
        var result = await testSubject.ValidateAndBindAsync(request, connectionForBindingProvider, cancellationTokenSource.Token);

        result.Should().Be(BindingResult.ProjectKeyNotFound);
        testLogger.AssertPartialOutputStringExists(Resources.Binding_ProjectKeyNotFound, request.TypeName);
        bindingController.DidNotReceiveWithAnyArgs().BindAsync(default, default);
    }

    [DynamicData(nameof(Requests))]
    [DataTestMethod]
    public async Task ValidateAndBindAsync_NoConnection_ReturnsNoConnectionResult(BindingRequest request)
    {
        connectionForBindingProvider.GetServerConnectionAsync(request).Returns(null as ServerConnection);

        var result = await testSubject.ValidateAndBindAsync(request, connectionForBindingProvider, cancellationTokenSource.Token);

        result.Should().Be(BindingResult.ConnectionNotFound);
        testLogger.AssertPartialOutputStringExists(Resources.Binding_ConnectionNotFound, request.TypeName);
        bindingController.DidNotReceiveWithAnyArgs().BindAsync(default, default);
    }

    [DynamicData(nameof(Requests))]
    [DataTestMethod]
    public async Task ValidateAndBindAsync_NoCredentials_ReturnsNoCredentialsResult(BindingRequest request)
    {
        var fakeConnection = new ServerConnection.SonarCloud("any org", credentials: null);
        connectionForBindingProvider.GetServerConnectionAsync(request).Returns(fakeConnection);

        var result = await testSubject.ValidateAndBindAsync(request, connectionForBindingProvider, cancellationTokenSource.Token);

        result.Should().Be(BindingResult.CredentialsNotFound);
        testLogger.AssertPartialOutputStringExists(string.Format(Resources.Binding_CredentiasNotFound, fakeConnection.Id), request.TypeName);
        bindingController.DidNotReceiveWithAnyArgs().BindAsync(default, default);
    }

    [DynamicData(nameof(Requests))]
    [DataTestMethod]
    public async Task ValidateAndBindAsync_BindingThrows_ReturnsFailedResult(BindingRequest request)
    {
        var fakeConnection = new ServerConnection.SonarCloud("any org", credentials: Substitute.For<IConnectionCredentials>());
        connectionForBindingProvider.GetServerConnectionAsync(request).Returns(fakeConnection);
        var fakeSolutionName = "solution name";
        solutionInfoProvider.GetSolutionNameAsync().Returns(fakeSolutionName);
        var exception = new Exception("some exception");
        bindingController.BindAsync(Arg.Any<BoundServerProject>(), cancellationTokenSource.Token).ThrowsAsync(exception);

        var result = await testSubject.ValidateAndBindAsync(request, connectionForBindingProvider, cancellationTokenSource.Token);

        result.Should().Be(BindingResult.Failed);
        testLogger.AssertPartialOutputStringExists(string.Format(Resources.Binding_Fails, exception.Message), request.TypeName);
    }


    [DynamicData(nameof(Requests))]
    [DataTestMethod]
    public async Task ValidateAndBindAsync_Success_ReturnsSuccessResult(BindingRequest request)
    {
        var fakeConnection = new ServerConnection.SonarCloud("any org", credentials: Substitute.For<IConnectionCredentials>());
        connectionForBindingProvider.GetServerConnectionAsync(request).Returns(fakeConnection);
        var fakeSolutionName = "solution name";
        solutionInfoProvider.GetSolutionNameAsync().Returns(fakeSolutionName);

        var result = await testSubject.ValidateAndBindAsync(request, connectionForBindingProvider, cancellationTokenSource.Token);

        result.Should().Be(BindingResult.Success);
        testLogger.AssertNoOutputMessages();
        bindingController.Received().BindAsync(Arg.Is<BoundServerProject>(x => x.LocalBindingKey == fakeSolutionName && x.ServerConnection == fakeConnection && x.ServerProjectKey == request.ProjectKey), cancellationTokenSource.Token);
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void Unbind_NoKey_UsesSolutionName(bool expectedResult)
    {
        var solutionName = "solution name";
        solutionInfoProvider.GetSolutionName().Returns(solutionName);
        bindingController.Unbind(solutionName).Returns(expectedResult);

        testSubject.Unbind().Should().Be(expectedResult);
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void Unbind_KeyProvided_UsesProvidedKey(bool expectedResult)
    {
        var solutionName = "solution name";
        bindingController.Unbind(solutionName).Returns(expectedResult);

        testSubject.Unbind(solutionName).Should().Be(expectedResult);
        solutionInfoProvider.DidNotReceive().GetSolutionName();
    }

    public static object[][] RequestsWithNullProjectKey =>
    [
        [new BindingRequest.Manual(null, "any")],
        [new BindingRequest.Shared(new SharedBindingConfigModel { ProjectKey = null, Uri = new("http://anyhost") })],
        [new BindingRequest.Assisted("any", null, false)]
    ];

    public static object[][] Requests =>
    [
        [new BindingRequest.Manual("project key", "connection id")],
        [new BindingRequest.Shared(new SharedBindingConfigModel { ProjectKey = "project key", Uri = new("http://anyhost") })],
        [new BindingRequest.Shared(new SharedBindingConfigModel { ProjectKey = "project key", Organization = "organization id", Region = "US" })],
        [new BindingRequest.Assisted("connection id", "project key", true)],
        [new BindingRequest.Assisted("connection id", "project key", false)],
    ];
}
