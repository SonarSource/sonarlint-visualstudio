/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Suppressions
{
    [TestClass]
    public class TimedUpdateHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TimedUpdateHandler, TimedUpdateHandler>(
                MefTestHelpers.CreateExport<ISuppressionIssueStoreUpdater>());
        }

        [TestMethod]
        public void Ctor_TimerIsSetupAsExpected()
        {
            var refreshTimer = new Mock<ITimer>();
            refreshTimer.SetupProperty(x => x.AutoReset);
            refreshTimer.SetupProperty(x => x.Interval);

            var timerFactory = CreateTimerFactory(refreshTimer.Object);

            _ = new TimedUpdateHandler(Mock.Of<ISuppressionIssueStoreUpdater>(), timerFactory);

            refreshTimer.Object.AutoReset.Should().BeTrue();
            refreshTimer.Object.Interval.Should().Be(1000 * 60 * 10);
            refreshTimer.VerifyAdd(x => x.Elapsed += It.IsAny<EventHandler<TimerEventArgs>>(), Times.Once);
            refreshTimer.Verify(x => x.Start(), Times.Once);
        }

        [TestMethod]
        public void InvokeEvent_TimerElapsed_SuppressionIssueStoreUpdaterIsCalled()
        {
            var refreshTimer = new Mock<ITimer>();
            var timerFactory = CreateTimerFactory(refreshTimer.Object);
            var suppressionIssueStoreUpdater = new Mock<ISuppressionIssueStoreUpdater>();

            _ = new TimedUpdateHandler(suppressionIssueStoreUpdater.Object, timerFactory);

            refreshTimer.Raise(x => x.Elapsed += null, new TimerEventArgs(DateTime.UtcNow));

            suppressionIssueStoreUpdater.Verify(x => x.UpdateAllServerSuppressionsAsync(), Times.Once);
        }

        [TestMethod]
        public void Dispose_RefreshTimerDisposed_RaisingEventDoesNothing()
        {
            var refreshTimer = new Mock<ITimer>();
            var timerFactory = CreateTimerFactory(refreshTimer.Object);
            var suppressionIssueStoreUpdater = new Mock<ISuppressionIssueStoreUpdater>();

            var testSubject = new TimedUpdateHandler(suppressionIssueStoreUpdater.Object, timerFactory);

            testSubject.Dispose();

            refreshTimer.Verify(x => x.Dispose(), Times.Once);
            refreshTimer.Raise(x => x.Elapsed += null, new TimerEventArgs(DateTime.UtcNow));

            suppressionIssueStoreUpdater.Verify(x => x.UpdateAllServerSuppressionsAsync(), Times.Never);
        }

        private static ITimerFactory CreateTimerFactory(ITimer timer)
        {
            var timerFactory = new Mock<ITimerFactory>();

            timerFactory.Setup(x => x.Create()).Returns(timer);

            return timerFactory.Object;
        }
    }
}
