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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Progress;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProgressNotificationListenerTests
    {
        [TestMethod]
        public void ProgressNotificationListener_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ProgressNotificationListener(null, new ConfigurableProgressEvents()));
            Exceptions.Expect<ArgumentNullException>(() => new ProgressNotificationListener(new ConfigurableServiceProvider(), null));
        }

        [TestMethod]
        public void ProgressNotificationListener_RespondToStepExecutionChangedEvent()
        {
            // Arrange
            var serviceProvider = new ConfigurableServiceProvider();

            var outputWindow = new ConfigurableVsOutputWindow();
            var outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            var progressEvents = new ConfigurableProgressEvents();
            var testSubject = new ProgressNotificationListener(serviceProvider, progressEvents);
            string message1 = "Hello world";
            string formattedMessage2 = "Bye bye";

            // Step 1: no formatting
            // Act
            progressEvents.SimulateStepExecutionChanged(message1, 0);

            // Assert
            outputWindowPane.AssertOutputStrings(message1);

            // Step 2: same message as before (ignore)
            // Act
            progressEvents.SimulateStepExecutionChanged(message1, 0);

            // Assert
            outputWindowPane.AssertOutputStrings(message1);

            // Step 3: whitespace message
            // Act
            progressEvents.SimulateStepExecutionChanged(" \t", 0);

            // Assert
            outputWindowPane.AssertOutputStrings(message1);

            // Step 4: formatting
            testSubject.MessageFormat = "XXX{0}YYY";
            // Act
            progressEvents.SimulateStepExecutionChanged(formattedMessage2, 0);

            // Assert
            outputWindowPane.AssertOutputStrings(message1, "XXX" + formattedMessage2 + "YYY");

            // Step 5: different message than the previous one
            testSubject.MessageFormat = null;
            // Act
            progressEvents.SimulateStepExecutionChanged(message1, 0);

            // Assert
            outputWindowPane.AssertOutputStrings(message1, "XXX" + formattedMessage2 + "YYY", message1);

            // Step 6: dispose
            testSubject.Dispose();
            // Act
            progressEvents.SimulateStepExecutionChanged("123", 0);

            // Assert
            outputWindowPane.AssertOutputStrings(message1, "XXX" + formattedMessage2 + "YYY", message1);
        }
    }
}