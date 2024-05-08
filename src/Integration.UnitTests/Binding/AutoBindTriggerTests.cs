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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding;

[TestClass]
public class AutoBindTriggerTests
{
    private static readonly ConnectionInformation ConnectionInformation = new ConnectionInformation(new Uri("http://localhost"));
    
    [TestMethod]
    public void MefCtor_CheckIsExported()
        => MefTestHelpers.CheckTypeCanBeImported<AutoBindTrigger, IAutoBindTrigger>(
            MefTestHelpers.CreateExport<IHost>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
        => MefTestHelpers.CheckIsSingletonMefComponent<AutoBindTrigger>();
    
    [TestMethod]
    public void TriggerAfterSuccessfulWorkflow_SchedulesAutobindOnFinish()
    {
        var testSubject = CreateTestSubject();
        var workflowProgressMock = new Mock<IProgressEvents>();

        testSubject.TriggerAfterSuccessfulWorkflow(workflowProgressMock.Object, null, null);
        
        workflowProgressMock.VerifyAdd(x => x.Finished += It.IsAny<EventHandler<ProgressControllerFinishedEventArgs>>(), Times.Once);
    }

    [TestMethod]
    public void AutobindIfPossible_FailedWorkflow_DoesNothing()
    {
        var (hostMock, bindCommandMock) = CreateHostMock();
        var testSubject = CreateTestSubject(hostMock.Object);
        
        testSubject.AutobindIfPossible(ProgressControllerResult.Failed, "project", ConnectionInformation);
        
        bindCommandMock.Verify(x => x.Execute(It.IsAny<BindCommandArgs>()), Times.Never);
    }
    
    [TestMethod]
    public void AutobindIfPossible_AutoBindNotRequested_DoesNothing()
    {
        var (hostMock, bindCommandMock) = CreateHostMock();
        var testSubject = CreateTestSubject(hostMock.Object);
        
        testSubject.AutobindIfPossible(ProgressControllerResult.Succeeded, null, ConnectionInformation);
        
        bindCommandMock.Verify(x => x.Execute(It.IsAny<BindCommandArgs>()), Times.Never);
    }
    
    [TestMethod]
    public void AutobindIfPossible_AutobindPossible_Binds()
    {
        var (hostMock, bindCommandMock) = CreateHostMock();
        var testSubject = CreateTestSubject(hostMock.Object);
        
        testSubject.AutobindIfPossible(ProgressControllerResult.Succeeded, "project", ConnectionInformation);
        
        bindCommandMock.Verify(
            x => x.Execute(It.Is<BindCommandArgs>(args =>
                args.ProjectKey == "project" && args.Connection == ConnectionInformation &&
                args.ProjectName == string.Empty)), Times.Once);
    }

    private static (Mock<IHost> hostMock, Mock<ICommand<BindCommandArgs>> bindCommandMock) CreateHostMock()
    {
        var hostMock = new Mock<IHost>();
        var activeSectionMock = new Mock<ISectionController>();
        var bindCommandMock = new Mock<ICommand<BindCommandArgs>>();
        hostMock.SetupGet(x => x.ActiveSection).Returns(activeSectionMock.Object);
        activeSectionMock.SetupGet(x => x.BindCommand).Returns(bindCommandMock.Object);
        return (hostMock, bindCommandMock);
    }

    private static AutoBindTrigger CreateTestSubject(IHost host = null)
    {
        host ??= Mock.Of<IHost>();
        return new AutoBindTrigger(host);
    }
}
