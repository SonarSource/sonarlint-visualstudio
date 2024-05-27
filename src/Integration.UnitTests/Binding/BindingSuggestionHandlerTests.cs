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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NuGet;
using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding;

[TestClass]
public class BindingSuggestionHandlerTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<BindingSuggestionHandler, IBindingSuggestionHandler>(
            MefTestHelpers.CreateExport<INotificationService>(),
            MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
            MefTestHelpers.CreateExport<IIDEWindowService>(),
            MefTestHelpers.CreateExport<ITeamExplorerController>());
    }

    [TestMethod]
    [DataRow(SonarLintMode.Standalone)]
    [DataRow(SonarLintMode.Connected)]
    public void Notify_BringsWindowToFront(SonarLintMode sonarLintMode)
    {
        var ideWindowService = Substitute.For<IIDEWindowService>();

        var testSubject = CreateTestSubject(sonarLintMode: sonarLintMode, ideWindowService: ideWindowService);
        testSubject.Notify();

        ideWindowService.Received().BringToFront();
    }

    [TestMethod]
    [DataRow(SonarLintMode.Standalone, true)]
    [DataRow(SonarLintMode.Connected, false)]
    public void Notify_ShowsMessage(SonarLintMode sonarLintMode, bool promptToConnect)
    {
        var notificationService = Substitute.For<INotificationService>();

        var testSubject = CreateTestSubject(sonarLintMode: sonarLintMode, notificationService: notificationService);
        testSubject.Notify();

        if (promptToConnect)
        {
            notificationService.Received().ShowNotification(Arg.Is<INotification>(
                n => n.Message.Equals(BindingStrings.BindingSuggestionProjectNotBound)
                     && n.Actions.ToArray()[0].CommandText.Equals(BindingStrings.BindingSuggestionConnect)));
        }
        else
        {
            notificationService.Received().ShowNotification(Arg.Is<INotification>(
                n => n.Message.Equals(BindingStrings.BindingSuggetsionBindingConflict)
                     && n.Actions.IsEmpty()));
        }
    }

    private BindingSuggestionHandler CreateTestSubject(SonarLintMode sonarLintMode, INotificationService notificationService = null, IIDEWindowService ideWindowService = null)
    {
        notificationService ??= Substitute.For<INotificationService>();
        var activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        ideWindowService ??= Substitute.For<IIDEWindowService>();
        var teamExplorerController = Substitute.For<ITeamExplorerController>();

        activeSolutionBoundTracker.CurrentConfiguration.Returns(new BindingConfiguration(new BoundSonarQubeProject(), sonarLintMode, "a-directory"));

        return new BindingSuggestionHandler(notificationService, activeSolutionBoundTracker, ideWindowService, teamExplorerController);
    }
}
