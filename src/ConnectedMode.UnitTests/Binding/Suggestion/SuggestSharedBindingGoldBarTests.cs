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

using System.Linq;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding.Suggestion;

[TestClass]
public class SuggestSharedBindingGoldBarTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<SuggestSharedBindingGoldBar, ISuggestSharedBindingGoldBar>(
            MefTestHelpers.CreateExport<INotificationService>(),
            MefTestHelpers.CreateExport<IDoNotShowAgainNotificationAction>(),
            MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
            MefTestHelpers.CreateExport<IBrowserService>());
    }
    
    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SuggestSharedBindingGoldBar>();
    }
    
    [TestMethod]
    public void Show_GeneratesCorrectNotificationStructure()
    {
        var notificationServiceMock = new Mock<INotificationService>();
        var doNotShowAgainMock = new Mock<IDoNotShowAgainNotificationAction>();
        var solutionInfoProviderMock = new Mock<ISolutionInfoProvider>();

        var testSubject = CreateTestSubject(notificationServiceMock.Object, doNotShowAgainMock.Object, solutionInfoProviderMock.Object);
        
        testSubject.Show(ServerType.SonarQube, () => { });
        
        solutionInfoProviderMock.Verify(x => x.GetSolutionName(), Times.Once);
        notificationServiceMock.Verify(x => x.ShowNotification(It.IsAny<INotification>()), Times.Once);

        var notification = (INotification)notificationServiceMock.Invocations.Single().Arguments.First();

        notification.Id.Should().NotBeNull();
        notification.Message.Should().Be(string.Format(BindingStrings.SharedBindingSuggestionMainText, ServerType.SonarQube));
        var notificationActions = notification.Actions.ToArray();
        notificationActions.Should().HaveCount(3);
        notificationActions[0].CommandText.Should().Be(BindingStrings.SharedBindingSuggestionConnectOptionText);
        notificationActions[0].ShouldDismissAfterAction.Should().BeTrue();
        notificationActions[1].CommandText.Should().Be(BindingStrings.SharedBindingSuggestionInfoOptionText);
        notificationActions[1].ShouldDismissAfterAction.Should().BeFalse();
        notificationActions[2].Should().BeSameAs(doNotShowAgainMock.Object);
    }

    
    [DataTestMethod]
    [DataRow(ServerType.SonarQube)]
    [DataRow(ServerType.SonarCloud)]
    public void Show_GeneratesMessageBasedOnServerType(ServerType serverType)
    {
        var notificationServiceMock = new Mock<INotificationService>();

        var testSubject = CreateTestSubject(notificationServiceMock.Object);
        
        testSubject.Show(serverType, null);
        
        var notification = (INotification)notificationServiceMock.Invocations.Single().Arguments.First();
        notification.Message.Should().Be(string.Format(BindingStrings.SharedBindingSuggestionMainText, serverType));
    }
    
    [DataTestMethod]
    [DataRow("Solution1")]
    [DataRow("MyPetProject")]
    public void Show_GeneratesIdBasedOnSolutionName(string solutionName)
    {
        var notificationServiceMock = new Mock<INotificationService>();
        var solutionInfoProviderMock = new Mock<ISolutionInfoProvider>();
        solutionInfoProviderMock.Setup(x => x.GetSolutionName()).Returns(solutionName);

        var testSubject = CreateTestSubject(notificationServiceMock.Object, solutionInfoProvider: solutionInfoProviderMock.Object);
        
        testSubject.Show(default, null);
        
        var notification = (INotification)notificationServiceMock.Invocations.Single().Arguments.First();
        notification.Id.Should().Be(string.Format(SuggestSharedBindingGoldBar.IdTemplate, solutionName));
    }

    [TestMethod]
    public void Show_InfoOptionOpensBrowser()
    {
        var notificationServiceMock = new Mock<INotificationService>();
        var browserServiceMock = new Mock<IBrowserService>();
        
        var testSubject = CreateTestSubject(notificationServiceMock.Object, browserService: browserServiceMock.Object);
        
        testSubject.Show(default, null);
        
        var notification = (INotification)notificationServiceMock.Invocations.Single().Arguments.First();
        var infoAction = notification.Actions.ToArray()[1];

        infoAction.Action(null);
        
        browserServiceMock.Verify(x => x.Navigate(DocumentationLinks.UseSharedBinding), Times.Once);
    }
    
    [TestMethod]
    public void Show_ConnectOptionCallsConnectAction()
    {
        var connectExecuted = false;
        
        var notificationServiceMock = new Mock<INotificationService>();
        
        var testSubject = CreateTestSubject(notificationServiceMock.Object);
        
        testSubject.Show(default, () => { connectExecuted = true;});
        
        var notification = (INotification)notificationServiceMock.Invocations.Single().Arguments.First();
        var connectAction = notification.Actions.ToArray()[0];

        connectAction.Action(null);

        connectExecuted.Should().BeTrue();
    }

    [TestMethod]
    public void Close_RemovesGoldBar()
    {
        var notificationServiceMock = new Mock<INotificationService>();
        
        var testSubject = CreateTestSubject(notificationServiceMock.Object);
        
        testSubject.Close();
        
        notificationServiceMock.Verify(x => x.RemoveNotification(), Times.Once);
    }

    private SuggestSharedBindingGoldBar CreateTestSubject(INotificationService notificationServiceMock,
        IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction = null,
        ISolutionInfoProvider solutionInfoProvider = null,
        IBrowserService browserService = null)
    {
        return new SuggestSharedBindingGoldBar(notificationServiceMock,
            doNotShowAgainNotificationAction ?? Mock.Of<IDoNotShowAgainNotificationAction>(),
            solutionInfoProvider ?? Mock.Of<ISolutionInfoProvider>(),
            browserService ?? Mock.Of<IBrowserService>());
    } 
}
