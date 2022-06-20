/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class UnboundSolutionCheckerTests
    {
        [TestMethod]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void IsBindingUpdateRequired_ReturnsIfSettingsExist(bool settingsExist, bool expectedResult)
        {
            var exclusionsSettingStorage = new Mock<IExclusionSettingsStorage>();
            exclusionsSettingStorage.Setup(x => x.SettingsExist()).Returns(settingsExist);

            var testSubject = CreateTestSubject(exclusionsSettingStorage.Object);

            var result = testSubject.IsBindingUpdateRequired();

            result.Should().Be(expectedResult);
            exclusionsSettingStorage.VerifyAll();
            exclusionsSettingStorage.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IsBindingUpdateRequired_FailedToFetchServerSettings_False()
        {
            var exclusionsSettingStorage = new Mock<IExclusionSettingsStorage>();
            exclusionsSettingStorage
                .Setup(x => x.SettingsExist())
                .Throws(new NotImplementedException("this is a test"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(exclusionsSettingStorage.Object, logger);

            var result = testSubject.IsBindingUpdateRequired();

            result.Should().BeFalse();
            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void IsBindingUpdateRequired_FailedToFetchServerSettings_CriticalException_ExceptionNotCaught()
        {
            var exclusionsSettingStorage = new Mock<IExclusionSettingsStorage>();
            exclusionsSettingStorage
                .Setup(x => x.SettingsExist())
                .Throws(new StackOverflowException());

            var testSubject = CreateTestSubject(exclusionsSettingStorage.Object);

            Action act = () => testSubject.IsBindingUpdateRequired();

            act.Should().Throw<StackOverflowException>();
        }

        private UnboundSolutionChecker CreateTestSubject(IExclusionSettingsStorage exclusionsSettingStorage, ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();

            return new UnboundSolutionChecker(exclusionsSettingStorage, logger);
        }
    }
}
