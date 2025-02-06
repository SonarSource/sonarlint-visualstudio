﻿/*
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

using System;
using System.Windows;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Vsix.Commands.ConnectedModeMenu;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands.ConnectedModeMenu
{
    [TestClass]
    public class SaveSharedConnectionCommandTests
    {
        [DataRow(SonarLintMode.Standalone, false)]
        [DataRow(SonarLintMode.Connected, true)]
        [DataRow(SonarLintMode.LegacyConnected, false)]
        [TestMethod]
        public void QueryStatus_EnableCommandCorrectly(SonarLintMode mode, bool expectedResult)
        {
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var bindingConfiguration = new BindingConfiguration(null, mode, null);

            var configurationProvider = CreateConfigurationProvider(bindingConfiguration);
            SaveSharedConnectionCommand testSubject = CreateTestSubject(configurationProvider);

            testSubject.QueryStatus(command, null);

            command.Enabled.Should().Be(expectedResult);
        }

        [TestMethod]
        public void Invoke_SonarQube_Success_InvokesCorrectly()
        {
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var serverConnection = new ServerConnection.SonarQube(new("http://localhost"));
            var sonarQubeUri = serverConnection.ServerUri;
            var project = new BoundServerProject("solution", "projectKey", serverConnection);
            var bindingConfiguration = new BindingConfiguration(project, SonarLintMode.Connected, null);

            var sharedBindingConfigProvider = new Mock<ISharedBindingConfigProvider>();
            sharedBindingConfigProvider.Setup(x => x.SaveSharedBinding(It.Is<SharedBindingConfigModel>(y => y.Uri == sonarQubeUri && y.ProjectKey == "projectKey"))).Returns("some Path");

            var messageBox = new Mock<IMessageBox>();

            var configurationProvider = CreateConfigurationProvider(bindingConfiguration);
            SaveSharedConnectionCommand testSubject = CreateTestSubject(configurationProvider, sharedBindingConfigProvider.Object, messageBox.Object);

            testSubject.Invoke(command, null);

            sharedBindingConfigProvider.Verify(x => x.SaveSharedBinding(It.Is<SharedBindingConfigModel>(y => y.Uri == sonarQubeUri && y.ProjectKey == "projectKey" && y.Organization == null && y.Region == null)), Times.Once);
            messageBox.Verify(mb => mb.Show(string.Format(Strings.SaveSharedConnectionCommand_SaveSuccess_Message, "some Path"), Strings.SaveSharedConnectionCommand_SaveSuccess_Caption, MessageBoxButton.OK, MessageBoxImage.Information), Times.Once);
        }

        [DataRow("US")]
        [DataRow("EU")]
        [DataTestMethod]
        public void Invoke_SonarCloudWithRegion_Success_InvokesCorrectly(string region)
        {
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var serverConnection = new ServerConnection.SonarCloud("organisationKey", CloudServerRegion.GetRegionByName(region));
            var sonarCloudUri = serverConnection.ServerUri;
            var project = new BoundServerProject("solution", "projectKey", serverConnection);
            var bindingConfiguration = new BindingConfiguration(project, SonarLintMode.Connected, null);

            var sharedBindingConfigProvider = new Mock<ISharedBindingConfigProvider>();
            sharedBindingConfigProvider.Setup(x => x.SaveSharedBinding(It.Is<SharedBindingConfigModel>(y => y.Uri == sonarCloudUri && y.ProjectKey == "projectKey" && y.Organization == "organisationKey" && y.Region == region))).Returns("some Path");

            var messageBox = new Mock<IMessageBox>();

            var configurationProvider = CreateConfigurationProvider(bindingConfiguration);
            SaveSharedConnectionCommand testSubject = CreateTestSubject(configurationProvider, sharedBindingConfigProvider.Object, messageBox.Object);

            testSubject.Invoke(command, null);

            sharedBindingConfigProvider.Verify(x => x.SaveSharedBinding(It.Is<SharedBindingConfigModel>(y => y.Uri == sonarCloudUri && y.ProjectKey == "projectKey" && y.Organization == "organisationKey" && y.Region == region)), Times.Once);
            messageBox.Verify(mb => mb.Show(string.Format(Strings.SaveSharedConnectionCommand_SaveSuccess_Message, "some Path"), Strings.SaveSharedConnectionCommand_SaveSuccess_Caption, MessageBoxButton.OK, MessageBoxImage.Information), Times.Once);
        }

        [TestMethod]
        public void Invoke_Fail_ShowFailMessage()
        {
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var project = new BoundServerProject("solution", "projectKey", new ServerConnection.SonarQube(new Uri("http://localhost")));
            var bindingConfiguration = new BindingConfiguration(project, SonarLintMode.Connected, null);

            var sharedBindingConfigProvider = new Mock<ISharedBindingConfigProvider>();
            sharedBindingConfigProvider.Setup(x => x.SaveSharedBinding(It.IsAny<SharedBindingConfigModel>())).Returns((string)null);

            var messageBox = new Mock<IMessageBox>();

            var configurationProvider = CreateConfigurationProvider(bindingConfiguration);
            SaveSharedConnectionCommand testSubject = CreateTestSubject(configurationProvider, sharedBindingConfigProvider.Object, messageBox.Object);

            testSubject.Invoke(command, null);

            messageBox.Verify(mb => mb.Show(Strings.SaveSharedConnectionCommand_SaveFail_Message, Strings.SaveSharedConnectionCommand_SaveFail_Caption, MessageBoxButton.OK, MessageBoxImage.Warning), Times.Once);
        }

        private static SaveSharedConnectionCommand CreateTestSubject(IConfigurationProvider configurationProvider, ISharedBindingConfigProvider sharedBindingConfigProvider = null, IMessageBox messageBox = null)
        {
            sharedBindingConfigProvider ??= Mock.Of<ISharedBindingConfigProvider>();
            messageBox ??= Mock.Of<IMessageBox>();

            return new SaveSharedConnectionCommand(configurationProvider, sharedBindingConfigProvider, messageBox);
        }

        private static IConfigurationProvider CreateConfigurationProvider(BindingConfiguration bindingConfiguration)
        {
            var configurationProvider = new Mock<IConfigurationProvider>();
            configurationProvider.Setup(cp => cp.GetConfiguration()).Returns(bindingConfiguration);
            return configurationProvider.Object;
        }
    }
}
