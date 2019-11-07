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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings
{
    [TestClass]
    public class OpenSettingsFileWpfCommandTests
    {
        [TestMethod]
        public void QueryStatus_AlwaysEnabled()
        {
            // Arrange
            var userSettingsProvider = CreateDummyUserSettingsProvider("d:\\a\\file.txt");
            var testSubject = new TestableOpenSettingsFileWpfCommand(new ConfigurableServiceProvider(), userSettingsProvider, new TestLogger());

            // Act & Assert
            testSubject.CanExecute(null).Should().BeTrue();
        }

        [TestMethod]
        public void Execute_EnsureFileExists()
        {
            // Arrange
            var userSettingsProvider = CreateDummyUserSettingsProvider("d:\\a\\file.txt");
            var testSubject = new TestableOpenSettingsFileWpfCommand(new ConfigurableServiceProvider(), userSettingsProvider, new TestLogger());

            // Act
            testSubject.Execute(null);

            // Assert
            testSubject.CallCount.Should().Be(1);
            testSubject.LastSuppliedFilePath.Should().Be("d:\\a\\file.txt");
        }

        [TestMethod]
        public void Execute_NonCriticalError_IsSuppressed()
        {
            // Arrange
            var userSettingsProvider = CreateDummyUserSettingsProvider("d:\\a\\file.txt");
            var testLogger = new TestLogger();
            var testSubject = new TestableOpenSettingsFileWpfCommand(new ConfigurableServiceProvider(), userSettingsProvider, testLogger)
            {
                OpenDocOp = () => throw new InvalidOperationException("dummy execute exception")
            };

            // Act - should not throw
            testSubject.Execute(null);

            // Assert
            testLogger.AssertPartialOutputStringExists("dummy execute exception");
        }

        [TestMethod]
        public void Execute_CriticalError_IsNotSuppressed()
        {
            // Arrange
            var userSettingsProvider = CreateDummyUserSettingsProvider("any");
            var testLogger = new TestLogger();
            var testSubject = new TestableOpenSettingsFileWpfCommand(new ConfigurableServiceProvider(), userSettingsProvider, testLogger)
            {
                OpenDocOp = () => throw new StackOverflowException("dummy execute exception")
            };

            // Act
            Action act = () => testSubject.Execute(null);

            // Assert
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("dummy execute exception");
            testLogger.AssertPartialOutputStringDoesNotExist("dummy execute exception");
        }

        private static IUserSettingsProvider CreateDummyUserSettingsProvider(string filePath)
        {
            var userSettingsProviderMock = new Mock<IUserSettingsProvider>();
            userSettingsProviderMock.Setup(x => x.SettingsFilePath).Returns(filePath);
            return userSettingsProviderMock.Object;
        }

        private class TestableOpenSettingsFileWpfCommand : OpenSettingsFileWpfCommand
        {
            public TestableOpenSettingsFileWpfCommand(IServiceProvider serviceProvider, IUserSettingsProvider userSettingsProvider, ILogger logger)
                : base(serviceProvider, userSettingsProvider, logger){}

            public int CallCount { get; private set; }
            public string LastSuppliedFilePath { get; set; }

            public Action OpenDocOp { get; set; }

            protected override void OpenDocumentInVs(string filePath)
            {
                CallCount++;
                LastSuppliedFilePath = filePath;
                OpenDocOp.Invoke();
            }
        }
    }
}
