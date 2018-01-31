/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.RuleSets;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarQubeRuleSetProviderTests
    {
        [TestMethod]
        public void Ctor_WhenSonarQubeServiceIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarQubeRuleSetProvider(null, new Mock<ILogger>().Object);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeService");
        }

        [TestMethod]
        public void Ctor_WhenLoggerIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarQubeRuleSetProvider(new Mock<ISonarQubeService>().Object, null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void GetRuleSet_WhenProjectIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            var testSubject = new SonarQubeRuleSetProvider(new Mock<ISonarQubeService>().Object, new Mock<ILogger>().Object);

            // Act
            Action act = () => testSubject.GetRuleSet(null, Language.CSharp);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [TestMethod]
        public void GetRuleSet_WhenLanguageIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            var testSubject = new SonarQubeRuleSetProvider(new Mock<ISonarQubeService>().Object, new Mock<ILogger>().Object);

            // Act
            Action act = () => testSubject.GetRuleSet(new Persistence.BoundSonarQubeProject(), null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("language");
        }

        [TestMethod]
        public void GetRuleSet_WhenLanguageIsUnknown_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var testSubject = new SonarQubeRuleSetProvider(new Mock<ISonarQubeService>().Object, new Mock<ILogger>().Object);

            // Act
            Action act = () => testSubject.GetRuleSet(new Persistence.BoundSonarQubeProject(), Language.Unknown);

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("language");
        }

        [TestMethod]
        public void GetRuleSet_WhenGetQualityProfileAsyncReturnsNull_ReturnsNull()
        {
            // Arrange
            var sonarqubeService = new Mock<ISonarQubeService>();
            sonarqubeService.Setup(x => x.GetQualityProfileAsync(It.IsAny<string>(), It.IsAny<string>(), SonarQubeLanguage.CSharp, CancellationToken.None))
                .ReturnsAsync(default(SonarQubeQualityProfile));
            var testSubject = new SonarQubeRuleSetProvider(sonarqubeService.Object, new Mock<ILogger>().Object);

            // Act
            var result = testSubject.GetRuleSet(new Persistence.BoundSonarQubeProject(), Language.CSharp);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void GetRuleSet_WhenGetRoslynExportProfileAsyncReturnsNull_ReturnsNull()
        {
            // Arrange
            var sonarqubeService = new Mock<ISonarQubeService>();
            sonarqubeService.Setup(x => x.GetQualityProfileAsync(It.IsAny<string>(), It.IsAny<string>(), SonarQubeLanguage.CSharp, CancellationToken.None))
                .ReturnsAsync(new SonarQubeQualityProfile("", "", "", true, DateTime.Now));
            sonarqubeService.Setup(x => x.GetRoslynExportProfileAsync(It.IsAny<string>(), It.IsAny<string>(), SonarQubeLanguage.CSharp, CancellationToken.None))
                .ReturnsAsync(default(RoslynExportProfileResponse));
            var testSubject = new SonarQubeRuleSetProvider(sonarqubeService.Object, new Mock<ILogger>().Object);

            // Act
            var result = testSubject.GetRuleSet(new Persistence.BoundSonarQubeProject(), Language.CSharp);

            // Assert
            result.Should().BeNull();
        }
    }
}
