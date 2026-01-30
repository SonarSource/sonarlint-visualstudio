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

using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.SLCore.Listener.Promote;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests;

[TestClass]
public class ShowMessageNotificationTests
{
    private INotificationService notificationService;
    private ShowMessageNotification testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        notificationService = Substitute.For<INotificationService>();
        testSubject = new ShowMessageNotification(notificationService);
    }

    [TestMethod]
    public void MefCtor_CheckExports() =>
        MefTestHelpers.CheckTypeCanBeImported<ShowMessageNotification, IShowMessageNotification>(
            MefTestHelpers.CreateExport<INotificationService>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<ShowMessageNotification>();

    [TestMethod]
    public void ShowAsync_WithMessageAndActions_ShowsNotificationWithCorrectMessage()
    {
        const string message = "Test message";
        var actions = new List<MessageActionItem>
        {
            new("key1", "Display 1", true)
        };

        var task = testSubject.ShowAsync(message, actions);

        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(n => n.Message == message));
        task.IsCompleted.Should().BeFalse();
    }

    [TestMethod]
    public void ShowAsync_WithMultipleActions_CreatesNotificationActionsForEach()
    {
        var actions = new List<MessageActionItem>
        {
            new("key1", "Display 1", true),
            new("key2", "Display 2", false),
            new("key3", "Display 3", false)
        };

        var task = testSubject.ShowAsync("message", actions);

        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(n =>
            n.Actions.Count() == 3 &&
            n.Actions.ElementAt(0).CommandText == "Display 1" &&
            n.Actions.ElementAt(1).CommandText == "Display 2" &&
            n.Actions.ElementAt(2).CommandText == "Display 3"));
        task.IsCompleted.Should().BeFalse();
    }

    [TestMethod]
    public void ShowAsync_WithEmptyActions_CreatesNotificationWithEmptyActions()
    {
        var actions = new List<MessageActionItem>();

        var task = testSubject.ShowAsync("message", actions);

        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(n => !n.Actions.Any()));
        task.IsCompleted.Should().BeFalse();
    }

    [TestMethod]
    public void ShowAsync_ConfiguresNotificationCorrectly()
    {
        var actions = new List<MessageActionItem> { new("key1", "Display 1", true) };

        var task = testSubject.ShowAsync("message", actions);

        notificationService.Received(1).ShowNotification(Arg.Is<INotification>(n =>
            n.Id == "ShowMessageNotification" &&
            n.ShowOncePerSession == false &&
            n.CloseOnSolutionClose == false &&
            n.Actions.All(a => a.ShouldDismissAfterAction == true)));
        task.IsCompleted.Should().BeFalse();
    }

    [TestMethod]
    public async Task ShowAsync_WhenActionInvoked_ReturnsActionKey()
    {
        var actions = new List<MessageActionItem>
        {
            new("key1", "Display 1", true),
            new("key2", "Display 2", false)
        };

        var task = testSubject.ShowAsync("message", actions);
        var notification = GetNotification();
        var action = notification.Actions.ElementAt(1);

        action.Action(notification);

        var result = await task;
        result.Should().Be("key2");
    }

    [TestMethod]
    public async Task ShowAsync_WhenClose_ReturnsNull()
    {
        var actions = new List<MessageActionItem> { new("key1", "Display 1", true) };

        var task = testSubject.ShowAsync("message", actions);
        var notification = GetNotification();

        notification.OnClose();

        var result = await task;
        result.Should().BeNull();
    }


    private INotification GetNotification()
    {
        return notificationService.ReceivedCalls()
            .Select(call => call.GetArguments()[0] as INotification)
            .First();
    }
}
