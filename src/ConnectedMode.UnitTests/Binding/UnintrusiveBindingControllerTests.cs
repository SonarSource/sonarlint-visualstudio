/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Security;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding;

[TestClass]
public class UnintrusiveBindingControllerTests
{
    private static readonly CancellationToken ACancellationToken = CancellationToken.None;
    private static readonly BasicAuthCredentials ValidToken = new("TOKEN", new SecureString());
    private static readonly BoundServerProject AnyBoundProject = new("any", "any", new ServerConnection.SonarCloud("any", credentials: ValidToken));
    private IActiveSolutionChangedHandler activeSolutionChangedHandler;
    private IBindingProcess bindingProcess;
    private IBindingProcessFactory bindingProcessFactory;
    private ISonarQubeService sonarQubeService;
    private UnintrusiveBindingController testSubject;
    private ISolutionBindingRepository solutionBindingRepository;

    [TestInitialize]
    public void TestInitialize()
    {
        CreateBindingProcessFactory();
        sonarQubeService = Substitute.For<ISonarQubeService>();
        activeSolutionChangedHandler = Substitute.For<IActiveSolutionChangedHandler>();
        solutionBindingRepository = Substitute.For<ISolutionBindingRepository>();
        testSubject = new UnintrusiveBindingController(bindingProcessFactory, sonarQubeService, activeSolutionChangedHandler, solutionBindingRepository);
    }

    [TestMethod]
    public void MefCtor_CheckTypeIsNonShared() => MefTestHelpers.CheckIsNonSharedMefComponent<UnintrusiveBindingController>();

    [TestMethod]
    public void MefCtor_IUnintrusiveBindingController_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<UnintrusiveBindingController, IUnintrusiveBindingController>(
            MefTestHelpers.CreateExport<IBindingProcessFactory>(),
            MefTestHelpers.CreateExport<ISonarQubeService>(),
            MefTestHelpers.CreateExport<IActiveSolutionChangedHandler>(),
            MefTestHelpers.CreateExport<ISolutionBindingRepository>());

    [TestMethod]
    public void MefCtor_IBindingController_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<UnintrusiveBindingController, IBindingController>(
            MefTestHelpers.CreateExport<IBindingProcessFactory>(),
            MefTestHelpers.CreateExport<ISonarQubeService>(),
            MefTestHelpers.CreateExport<IActiveSolutionChangedHandler>(),
            MefTestHelpers.CreateExport<ISolutionBindingRepository>());

    [TestMethod]
    public async Task BindAsync_EstablishesConnection()
    {
        var projectToBind = new BoundServerProject(
            "local-key",
            "server-key",
            new ServerConnection.SonarCloud("organization", credentials: ValidToken));

        await testSubject.BindAsync(projectToBind, ACancellationToken);

        await sonarQubeService
            .Received()
            .ConnectAsync(
                Arg.Is<ConnectionInformation>(x => x.ServerUri.Equals("https://sonarcloud.io/")
                                                   && x.UserName.Equals(ValidToken.UserName)
                                                   && string.IsNullOrEmpty(x.Password.ToUnsecureString())),
                ACancellationToken);
    }

    [TestMethod]
    public async Task BindAsync_NotifiesBindingChanged()
    {
        await testSubject.BindAsync(AnyBoundProject, ACancellationToken);

        activeSolutionChangedHandler
            .Received(1)
            .HandleBindingChange(false);
    }

    [TestMethod]
    public async Task BindAsync_CallsBindingProcessInOrder()
    {
        var cancellationToken = CancellationToken.None;

        await testSubject.BindAsync(AnyBoundProject, null, cancellationToken);

        Received.InOrder(() =>
        {
            bindingProcessFactory.Create(Arg.Is<BindCommandArgs>(b => b.ProjectToBind == AnyBoundProject));
            bindingProcess.DownloadQualityProfileAsync(null, cancellationToken);
            bindingProcess.SaveServerExclusionsAsync(cancellationToken);
        });
    }

    [TestMethod]
    public void Unbind_BindingDeletionSucceeded_HandlesBindingChangesAndDisconnects()
    {
        solutionBindingRepository.DeleteBinding(AnyBoundProject.LocalBindingKey).Returns(true);

        testSubject.Unbind(AnyBoundProject.LocalBindingKey);

        Received.InOrder(() =>
        {
            solutionBindingRepository.DeleteBinding(AnyBoundProject.LocalBindingKey);
            sonarQubeService.Disconnect();
            activeSolutionChangedHandler.HandleBindingChange(true);
        });
    }

    [TestMethod]
    public void Unbind_BindingDeletionFailed_DoesNotCallHandleBindingChange()
    {
        solutionBindingRepository.DeleteBinding(AnyBoundProject.LocalBindingKey).Returns(false);

        testSubject.Unbind(AnyBoundProject.LocalBindingKey);

        solutionBindingRepository.Received(1).DeleteBinding(AnyBoundProject.LocalBindingKey);
        activeSolutionChangedHandler.DidNotReceive().HandleBindingChange(true);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Unbind_ReturnsResultOfDeletedBinding(bool expectedResult)
    {
        solutionBindingRepository.DeleteBinding(AnyBoundProject.LocalBindingKey).Returns(expectedResult);

        var result = testSubject.Unbind(AnyBoundProject.LocalBindingKey);

        result.Should().Be(expectedResult);
    }

    private void CreateBindingProcessFactory()
    {
        bindingProcess ??= Substitute.For<IBindingProcess>();

        bindingProcessFactory = Substitute.For<IBindingProcessFactory>();
        bindingProcessFactory.Create(Arg.Any<BindCommandArgs>()).Returns(bindingProcess);
    }
}
