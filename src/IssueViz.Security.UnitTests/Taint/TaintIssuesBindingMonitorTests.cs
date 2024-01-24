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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint
{
    [TestClass]
    public class TaintIssuesBindingMonitorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TaintIssuesBindingMonitor, ITaintIssuesBindingMonitor>(
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<IFolderWorkspaceMonitor>(),
                MefTestHelpers.CreateExport<ITaintIssuesSynchronizer>());
        }

        [TestMethod]
        public void Ctor_SubscribeToSolutionBindingUpdated()
        {
            var synchronizer = new Mock<ITaintIssuesSynchronizer>();
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            var folderWorkspaceInitialized = new Mock<IFolderWorkspaceMonitor>();

            new TaintIssuesBindingMonitor(activeSolutionBoundTracker.Object, folderWorkspaceInitialized.Object, synchronizer.Object);
            synchronizer.Invocations.Clear();

            activeSolutionBoundTracker.Raise(x=> x.SolutionBindingUpdated += null, EventArgs.Empty);

            synchronizer.Verify(x=> x.SynchronizeWithServer(), Times.Once);
        }

        [TestMethod]
        public void Ctor_SubscribeToSolutionBindingChanged()
        {
            var synchronizer = new Mock<ITaintIssuesSynchronizer>();
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            var folderWorkspaceInitialized = new Mock<IFolderWorkspaceMonitor>();

            new TaintIssuesBindingMonitor(activeSolutionBoundTracker.Object, folderWorkspaceInitialized.Object, synchronizer.Object);
            synchronizer.Invocations.Clear();
            
            activeSolutionBoundTracker.Raise(x => x.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(BindingConfiguration.Standalone));

            synchronizer.Verify(x => x.SynchronizeWithServer(), Times.Once);
        }

        [TestMethod]
        public void Ctor_SubscribeToFolderInitialized()
        {
            var synchronizer = new Mock<ITaintIssuesSynchronizer>();
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            var folderWorkspaceInitialized = new Mock<IFolderWorkspaceMonitor>();

            new TaintIssuesBindingMonitor(activeSolutionBoundTracker.Object, folderWorkspaceInitialized.Object, synchronizer.Object);
            synchronizer.Invocations.Clear();

            folderWorkspaceInitialized.Raise(x => x.FolderWorkspaceInitialized += null, EventArgs.Empty);

            synchronizer.Verify(x => x.SynchronizeWithServer(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnsubscribeFromEvents()
        {
            var synchronizer = new Mock<ITaintIssuesSynchronizer>();
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            var folderWorkspaceInitialized = new Mock<IFolderWorkspaceMonitor>();

            var testSubject = new TaintIssuesBindingMonitor(activeSolutionBoundTracker.Object, folderWorkspaceInitialized.Object, synchronizer.Object);
            testSubject.Dispose();
            synchronizer.Invocations.Clear();

            folderWorkspaceInitialized.Raise(x => x.FolderWorkspaceInitialized += null, EventArgs.Empty);
            activeSolutionBoundTracker.Raise(x => x.SolutionBindingUpdated += null, EventArgs.Empty);
            activeSolutionBoundTracker.Raise(x => x.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(BindingConfiguration.Standalone));

            synchronizer.Invocations.Count.Should().Be(0);
        }
    }
}
