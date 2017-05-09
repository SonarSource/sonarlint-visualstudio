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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.Tests
{
    [TestClass]
    public class TelemetryManagerTests
    {
        [TestMethod]
        public void Ctor_WhenGivenANullActiveSolutionBoundTracker_ThrowsArgumentNullException()
        {
            // Arrange
            var telemetryRepository = new Mock<ITelemetryDataRepository>();
            var telemetryClient = new Mock<ITelemetryClient>();

            // Act
            Action action = () => new TelemetryManager(null, telemetryRepository.Object, telemetryClient.Object);


            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingTracker");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullTelmetryRepository_ThrowsArgumentNullException()
        {
            // Arrange
            var solutionBindingTracker = new Mock<IActiveSolutionBoundTracker>();
            var telemetryClient = new Mock<ITelemetryClient>();

            // Act
            Action action = () => new TelemetryManager(solutionBindingTracker.Object, null, telemetryClient.Object);


            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("telemetryRepository");
        }

        [TestMethod]
        public void Ctor_WhenGivenANullTelemetryClient_ThrowsArgumentNullException()
        {
            // Arrange
            var solutionBindingTracker = new Mock<IActiveSolutionBoundTracker>();
            var telemetryRepository = new Mock<ITelemetryDataRepository>();

            // Act
            Action action = () => new TelemetryManager(solutionBindingTracker.Object, telemetryRepository.Object, null);


            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("telemetryClient");
        }

        [TestMethod]
        public void IsAnonymousDataShared_ReturnsValueFromRepository()
        {
            // Arrange
            var solutionBindingTracker = new Mock<IActiveSolutionBoundTracker>();
            var expectedValue = false;
            var telemetryRepository = new Mock<ITelemetryDataRepository>();
            telemetryRepository.Setup(x => x.Data.IsAnonymousDataShared).Returns(expectedValue);
            var telemetryClient = new Mock<ITelemetryClient>();

            var manager = new TelemetryManager(solutionBindingTracker.Object, telemetryRepository.Object,
                telemetryClient.Object);

            // Act
            var result = manager.IsAnonymousDataShared;

            // Assert
            result.Should().Be(expectedValue);
        }
    }
}
