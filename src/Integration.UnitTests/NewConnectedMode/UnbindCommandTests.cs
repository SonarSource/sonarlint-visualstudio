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
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.TeamFoundation.MVVM;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class UnbindCommandTests
    {
        private readonly BoundSonarQubeProject ValidProject = new BoundSonarQubeProject(new Uri("http://any"), "projectKey");

        private ConfigurableHost host;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableConfigurationProvider configProvider;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableSectionController section;

        private UnbindCommand testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            serviceProvider = new ConfigurableServiceProvider();

            configProvider = new ConfigurableConfigurationProvider();
            configProvider.ProjectToReturn = ValidProject;
            serviceProvider.RegisterService(typeof(IConfigurationProvider), configProvider);

            var outputWindow = new ConfigurableVsOutputWindow();
            outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            host = new ConfigurableHost(serviceProvider, Dispatcher.CurrentDispatcher);

            section = new ConfigurableSectionController();
            host.SetActiveSection(section);

            testSubject = new UnbindCommand(host);
        }

        [TestMethod]
        public void Ctor_NullHost_Throws()
        {
            // Arrange
            Action act = () => new UnbindCommand(null);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("host");
        }

        [TestMethod]
        public void CanExecute_HostIsBusy_ReturnsFalse()
        {
            // Arrange
            host.TestStateManager.IsBusy = true;

            // Act & Assert
            testSubject.CanExecute().Should().BeFalse();
        }

        [TestMethod]
        public void CanExecute_InStandaloneMode_ReturnsFalse()
        {
            // Arrange
            host.TestStateManager.IsBusy = false;
            configProvider.ModeToReturn = SonarLintMode.Standalone;

            // Act & Assert
            testSubject.CanExecute().Should().BeFalse();
        }

        [TestMethod]
        public void CanExecute_InLegacyConnectedMode_ReturnsFalse()
        {
            // Arrange
            host.TestStateManager.IsBusy = false;
            configProvider.ModeToReturn = SonarLintMode.LegacyConnected;

            // Act & Assert
            testSubject.CanExecute().Should().BeFalse();
        }

        [TestMethod]
        public void CanExecute_InConnectedMode_ReturnsTrue()
        {
            // Arrange
            host.TestStateManager.IsBusy = false;
            configProvider.ModeToReturn = SonarLintMode.Connected;

            // Act & Assert
            testSubject.CanExecute().Should().BeTrue();
        }

        [TestMethod]
        public void Execute_NoErrors_Succeed()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();

            host.TestStateManager.IsBusy = false;
            configProvider.ModeToReturn = SonarLintMode.Connected;

            bool disconnectCalled = false;
            section.DisconnectCommand = new RelayCommand(exec =>
                {
                    host.TestStateManager.IsBusy.Should().BeTrue(); // check busy flag is set
                    disconnectCalled = true;
                });

            // Act
            testSubject.Execute();

            // Assert
            configProvider.DeleteCallCount.Should().Be(1);
            disconnectCalled.Should().BeTrue();

            outputWindowPane.AssertOutputStrings(
                Strings.Unbind_State_Started,
                Strings.Unbind_DeletingBinding,
                Strings.Unbind_DisconnectingFromSonarQube,
                Strings.Unbind_State_Succeeded);
            host.VisualStateManager.IsBusy.Should().BeFalse();  
        }

        [TestMethod]
        public void Execute_ErrorThrown_FailsButNoExceptionThrow()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();

            host.TestStateManager.IsBusy = false;
            configProvider.ModeToReturn = SonarLintMode.Connected;

            bool eventRaised = false;
            host.TestStateManager.BindingStateChanged += (s, e) => eventRaised = true;

            section.DisconnectCommand = new RelayCommand(exec =>
                {
                    host.TestStateManager.IsBusy.Should().BeTrue(); // check busy flag is set
                    throw new InvalidCastException("my error message");
                });

            // Act
            testSubject.Execute();

            // Assert
            eventRaised.Should().BeFalse();
            outputWindowPane.AssertOutputStrings(
                Strings.Unbind_State_Started,
                Strings.Unbind_DeletingBinding,
                Strings.Unbind_DisconnectingFromSonarQube,
                "my error message",
                Strings.Unbind_State_Failed);
            host.VisualStateManager.IsBusy.Should().BeFalse();
        }
    }
}