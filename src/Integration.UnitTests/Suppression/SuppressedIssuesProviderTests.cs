/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class SuppressedIssuesProviderTests
    {
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private ConfigurableActiveSolutionBoundTracker activeSolutionBoundTracker;
        private Mock<ILogger> loggerMock;
        private Mock<ITimerFactory> timerFactory;
        private SuppressedIssuesProvider testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            sonarQubeServiceMock = new Mock<ISonarQubeService>();
            activeSolutionBoundTracker = new ConfigurableActiveSolutionBoundTracker();
            loggerMock = new Mock<ILogger>();
            timerFactory = new Mock<ITimerFactory>();

            testSubject = new SuppressedIssuesProvider(sonarQubeServiceMock.Object,
                activeSolutionBoundTracker,
                loggerMock.Object,
                timerFactory.Object);
        }
    }
}
