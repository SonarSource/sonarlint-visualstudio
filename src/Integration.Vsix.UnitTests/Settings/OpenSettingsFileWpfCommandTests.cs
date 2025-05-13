/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Windows.Forms;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings;

[TestClass]
public class OpenSettingsFileWpfCommandTests
{
    [TestMethod]
    public void QueryStatus_AlwaysEnabled()
    {
        // Arrange
        var globalSettingsStorage = CreateDummyGlobalSettingsStorage("d:\\a\\file.txt");
        var testSubject = new TestableOpenSettingsFileWpfCommand(new ConfigurableServiceProvider(), globalSettingsStorage, null, new TestLogger());

        // Act & Assert
        testSubject.CanExecute(null).Should().BeTrue();
    }

    [TestMethod]
    public void Execute_EnsureFileExists()
    {
        // Arrange
        var globalSettingsStorage = CreateDummyGlobalSettingsStorage("d:\\a\\file.txt");
        var testSubject = new TestableOpenSettingsFileWpfCommand(new ConfigurableServiceProvider(), globalSettingsStorage, null, new TestLogger());

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
        var globalSettingsStorage = CreateDummyGlobalSettingsStorage("d:\\a\\file.txt");
        var testLogger = new TestLogger();
        var testSubject = new TestableOpenSettingsFileWpfCommand(new ConfigurableServiceProvider(), globalSettingsStorage, null, testLogger)
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
        var globalSettingsStorage = CreateDummyGlobalSettingsStorage("any");
        var testLogger = new TestLogger();
        var testSubject = new TestableOpenSettingsFileWpfCommand(new ConfigurableServiceProvider(), globalSettingsStorage, null, testLogger)
        {
            OpenDocOp = () => throw new StackOverflowException("dummy execute exception")
        };

        // Act
        var act = () => testSubject.Execute(null);

        // Assert
        act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("dummy execute exception");
        testLogger.AssertPartialOutputStringDoesNotExist("dummy execute exception");
    }

    private static IGlobalSettingsStorage CreateDummyGlobalSettingsStorage(string filePath)
    {
        var globalSettingsStorageMock = new Mock<IGlobalSettingsStorage>();
        globalSettingsStorageMock.Setup(x => x.SettingsFilePath).Returns(filePath);
        return globalSettingsStorageMock.Object;
    }

    private class TestableOpenSettingsFileWpfCommand : OpenSettingsFileWpfCommand
    {
        public int CallCount { get; private set; }
        public string LastSuppliedFilePath { get; set; }

        public Action OpenDocOp { get; set; }

        public TestableOpenSettingsFileWpfCommand(
            IServiceProvider serviceProvider,
            IGlobalSettingsStorage globalSettingsStorage,
            IWin32Window win32Window,
            ILogger logger)
            : base(serviceProvider, globalSettingsStorage, win32Window, logger)
        {
        }

        protected override void OpenDocumentInVs(string filePath)
        {
            CallCount++;
            LastSuppliedFilePath = filePath;
            OpenDocOp.Invoke();
        }
    }
}
