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
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class NewBindingWorkflowTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableHost host;
        private ConfigurableConfigurationProvider configWriter;

        private readonly BindCommandArgs ValidBindArgs =
            new BindCommandArgs("anykey", "anyname", new ConnectionInformation(new Uri("http://localhost:9000")));

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);

            configWriter = new ConfigurableConfigurationProvider();
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullHost_Throws()
        {
            // Arrange
            Action act = () => new NewBindingWorkflow(null, ValidBindArgs, configWriter);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("host");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullBindingArgs_Throws()
        {
            // Arrange
            Action act = () => new NewBindingWorkflow(host, null, configWriter);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("bindingArgs");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullConfigWriter_Throws()
        {
            // Arrange
            Action act = () => new NewBindingWorkflow(host, ValidBindArgs, null);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("configWriter");
        }

        [TestMethod]
        public void Run_SaveFails_WorkflowIsAborted()
        {
            // Arrange
            configWriter.WriteResultToReturn = false;

            var controller = new ConfigurableProgressController();
            var notifications = new ConfigurableProgressStepExecutionEvents();

            var testSubject = CreateTestSubject();

            // Act
            testSubject.SaveBindingInfo(controller, notifications, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(1);
            notifications.AssertProgressMessages(
                Strings.StartedSolutionBindingWorkflow,
                Strings.Bind_SavingBindingConfiguration);
            this.outputWindowPane.AssertOutputStrings(Strings.Bind_FailedToSaveConfiguration);
        }

        [TestMethod]
        public void Run_SaveSucceeds_WorkflowContinues()
        {
            // Arrange
            configWriter.WriteResultToReturn = true;

            var controller = new ConfigurableProgressController();
            var notifications = new ConfigurableProgressStepExecutionEvents();

            var testSubject = CreateTestSubject();

            // Act
            testSubject.SaveBindingInfo(controller, notifications, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(0);
            notifications.AssertProgressMessages(
                Strings.StartedSolutionBindingWorkflow,
                Strings.Bind_SavingBindingConfiguration,
                Strings.FinishedSolutionBindingWorkflowSuccessful);
        }

        private NewBindingWorkflow CreateTestSubject()
        {
            return new NewBindingWorkflow(host, ValidBindArgs, configWriter);
        }
    }
}
