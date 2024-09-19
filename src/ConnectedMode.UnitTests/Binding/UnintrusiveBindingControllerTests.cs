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
using SonarLint.VisualStudio.Core;
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
    private static readonly BasicAuthCredentials ValidToken = new ("TOKEN", new SecureString());
    private static readonly BoundSonarQubeProject OldBoundProject = new (new Uri("http://any"), "any", "any");
    private static readonly BoundServerProject AnyBoundProject = new ("any", "any", new ServerConnection.SonarCloud("any", credentials: ValidToken));

    [TestMethod]
    public void MefCtor_CheckTypeIsNonShared()
        => MefTestHelpers.CheckIsNonSharedMefComponent<UnintrusiveBindingController>();

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<UnintrusiveBindingController, IUnintrusiveBindingController>(
            MefTestHelpers.CreateExport<IBindingProcessFactory>(),
            MefTestHelpers.CreateExport<IServerConnectionsRepository>(),
            MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
            MefTestHelpers.CreateExport<ISonarQubeService>(),
            MefTestHelpers.CreateExport<IActiveSolutionChangedHandler>());
    }

    [TestMethod]
    public async Task BindAsync_EstablishesConnection()
    {
        var sonarQubeService = Substitute.For<ISonarQubeService>();
        var projectToBind = new BoundServerProject(
            "local-key", 
            "server-key",
            new ServerConnection.SonarCloud("organization", credentials: ValidToken));
        var testSubject = CreateTestSubject(sonarQubeService: sonarQubeService);
        
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
        var activeSolutionChangedHandler = Substitute.For<IActiveSolutionChangedHandler>();
        var testSubject = CreateTestSubject(activeSolutionChangedHandler: activeSolutionChangedHandler);
        
        await testSubject.BindAsync(AnyBoundProject, ACancellationToken);

        activeSolutionChangedHandler
            .Received(1)
            .HandleBindingChange(false);
    }

    [TestMethod]
    public async Task BindAsync_CallsBindingProcessInOrder()
    {
        var cancellationToken = CancellationToken.None;
        var bindingProcess = Substitute.For<IBindingProcess>();
        var bindingProcessFactory = CreateBindingProcessFactory(bindingProcess);
        var testSubject = CreateTestSubject(bindingProcessFactory);
            
        await testSubject.BindAsync(AnyBoundProject, null, cancellationToken);

        Received.InOrder(() =>
        {
            bindingProcessFactory.Create(Arg.Is<BindCommandArgs>(b => b.ProjectToBind == AnyBoundProject));
            bindingProcess.DownloadQualityProfileAsync(null, cancellationToken);
            bindingProcess.SaveServerExclusionsAsync(cancellationToken);
        });
    }
        
    [TestMethod]
    public async Task BindWithMigrationAsync_OldProject_ConnectionExists_EstablishesBinding()
    {
        var cancellationToken = CancellationToken.None;
        var bindingProcess = Substitute.For<IBindingProcess>();
        var bindingProcessFactory = CreateBindingProcessFactory(bindingProcess);
        var convertedConnection = ServerConnection.FromBoundSonarQubeProject(OldBoundProject);
        var storedConnection = new ServerConnection.SonarQube(new Uri("http://any"));
        var serverConnectionsRepository = CreateServerConnectionsRepository(convertedConnection.Id, storedConnection);
        var solutionInfoProvider = CreateSolutionInfoProvider();
        var testSubject = CreateTestSubject(bindingProcessFactory, serverConnectionsRepository, solutionInfoProvider);
            
        await testSubject.BindWithMigrationAsync(OldBoundProject, null, cancellationToken);

        Received.InOrder(() =>
        {
            serverConnectionsRepository.TryGet(convertedConnection.Id, out Arg.Any<ServerConnection>());
            solutionInfoProvider.GetSolutionNameAsync();
            bindingProcessFactory.Create(Arg.Is<BindCommandArgs>(b => b.ProjectToBind.ServerProjectKey == OldBoundProject.ProjectKey && b.ProjectToBind.ServerConnection == storedConnection));
            bindingProcess.DownloadQualityProfileAsync(null, cancellationToken);
            bindingProcess.SaveServerExclusionsAsync(cancellationToken);
        });
    }

    [TestMethod]
    public async Task BindWithMigrationAsync_OldProject_ConnectionDoesNotExist_AddsConnectionAndEstablishesBinding()
    {
        var cancellationToken = CancellationToken.None;
        var bindingProcess = Substitute.For<IBindingProcess>();
        var bindingProcessFactory = CreateBindingProcessFactory(bindingProcess);
        var convertedConnection = ServerConnection.FromBoundSonarQubeProject(OldBoundProject);
        var serverConnectionsRepository = CreateServerConnectionsRepository();
        serverConnectionsRepository.TryAdd(Arg.Is<ServerConnection>(s => s.Id == convertedConnection.Id)).Returns(true);
        var solutionInfoProvider = CreateSolutionInfoProvider();
        var testSubject = CreateTestSubject(bindingProcessFactory, serverConnectionsRepository, solutionInfoProvider);
            
        await testSubject.BindWithMigrationAsync(OldBoundProject, null, cancellationToken);

        Received.InOrder(() =>
        {
            serverConnectionsRepository.TryGet(convertedConnection.Id, out Arg.Any<ServerConnection>());
            serverConnectionsRepository.TryAdd(Arg.Is<ServerConnection>(c => c.Id == convertedConnection.Id));
            solutionInfoProvider.GetSolutionNameAsync();
            bindingProcessFactory.Create(Arg.Is<BindCommandArgs>(b => b.ProjectToBind.ServerProjectKey == OldBoundProject.ProjectKey && b.ProjectToBind.ServerConnection.Id == convertedConnection.Id));
            bindingProcess.DownloadQualityProfileAsync(null, cancellationToken);
            bindingProcess.SaveServerExclusionsAsync(cancellationToken);
        });
    }
    
    [TestMethod]
    public void BindWithMigrationAsync_OldProject_ConnectionDoesNotExist_CannotAdd_Throws()
    {
        var convertedConnection = ServerConnection.FromBoundSonarQubeProject(OldBoundProject);
        var serverConnectionsRepository = CreateServerConnectionsRepository(convertedConnection.Id);
        var testSubject = CreateTestSubject(serverConnectionsRepository: serverConnectionsRepository);
            
        Func<Task> act = async () => await testSubject.BindWithMigrationAsync(OldBoundProject, null, CancellationToken.None);

        act.Should().Throw<InvalidOperationException>().WithMessage(BindingStrings.UnintrusiveController_CantMigrateConnection);
    }
    
    [TestMethod]
    public void BindWithMigrationAsync_OldProject_InvalidServerInformation_Throws()
    {
        var testSubject = CreateTestSubject();
            
        Func<Task> act = async () => await testSubject.BindWithMigrationAsync(new BoundSonarQubeProject(), null, CancellationToken.None);

        act.Should().Throw<InvalidOperationException>().WithMessage(BindingStrings.UnintrusiveController_InvalidConnection);
    }

    private UnintrusiveBindingController CreateTestSubject(IBindingProcessFactory bindingProcessFactory = null,
        IServerConnectionsRepository serverConnectionsRepository = null,
        ISolutionInfoProvider solutionInfoProvider = null,
        ISonarQubeService sonarQubeService = null,
        IActiveSolutionChangedHandler activeSolutionChangedHandler = null)
    {
        var testSubject = new UnintrusiveBindingController(bindingProcessFactory ?? CreateBindingProcessFactory(),
            serverConnectionsRepository ?? Substitute.For<IServerConnectionsRepository>(),
            solutionInfoProvider ?? Substitute.For<ISolutionInfoProvider>(),
            sonarQubeService ?? Substitute.For<ISonarQubeService>(),
            activeSolutionChangedHandler ?? Substitute.For<IActiveSolutionChangedHandler>());

        return testSubject;
    }

    private static IBindingProcessFactory CreateBindingProcessFactory(IBindingProcess bindingProcess = null)
    {
        bindingProcess ??= Substitute.For<IBindingProcess>();

        var bindingProcessFactory = Substitute.For<IBindingProcessFactory>();
        bindingProcessFactory.Create(Arg.Any<BindCommandArgs>()).Returns(bindingProcess);

        return bindingProcessFactory;
    }
    
    private static IServerConnectionsRepository CreateServerConnectionsRepository(string id = null, ServerConnection.SonarQube storedConnection = null)
    {
        var serverConnectionsRepository = Substitute.For<IServerConnectionsRepository>();
        serverConnectionsRepository.TryGet(id ?? Arg.Any<string>(), out Arg.Any<ServerConnection>())
            .Returns(info =>
            {
                info[1] = storedConnection;
                return storedConnection != null;
            });
        return serverConnectionsRepository;
    }
    
    private static ISolutionInfoProvider CreateSolutionInfoProvider()
    {
        var solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        solutionInfoProvider.GetSolutionNameAsync().Returns("solution");
        return solutionInfoProvider;
    }
}
