/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Integration.Notifications.UnitTests
{
    [TestClass]
    public class SonarQubeNotifications_Stop
    {
        private ISonarQubeServiceWrapper sqService;
        private IStateManager stateManager;
        private Mock<ITimer> timerMock;

        [TestInitialize]
        public void TestInitialize()
        {
            sqService = new ConfigurableSonarQubeServiceWrapper();
            stateManager = new ConfigurableStateManager();
            timerMock = new Mock<ITimer>();
        }

        [TestMethod]
        public void Stop_Sets_IsVisible_Timer_Stop()
        {
            // Arrange
            timerMock.Setup(mock => mock.Stop());

            var notifications = new SonarQubeNotifications(sqService, stateManager, timerMock.Object);
            notifications.IsVisible = true;

            // Act
            notifications.Stop();

            // Assert
            timerMock.VerifyAll();
            notifications.IsVisible.Should().BeFalse();
        }
    }
}
