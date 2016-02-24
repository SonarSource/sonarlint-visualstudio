//-----------------------------------------------------------------------
// <copyright file="ProgressNotificationListenerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Progress;
using System;

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
            // Setup
            var serviceProvider = new ConfigurableServiceProvider();
            var outputWindowPane = new ConfigurableVsGeneralOutputWindowPane();
            serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), outputWindowPane);
            var progressEvents = new ConfigurableProgressEvents();
            var testSubject = new ProgressNotificationListener(serviceProvider, progressEvents);
            string message1 = "Hello world";
            string formattedMessage2 = "Bye bye";

            // Step 1: no formatting
            // Act
            progressEvents.SimulateStepExecutionChanged(message1, 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1);

            // Step 2: same message as before (ignore)
            // Act
            progressEvents.SimulateStepExecutionChanged(message1, 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1);

            // Step 3: whitespace message
            // Act
            progressEvents.SimulateStepExecutionChanged(" \t", 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1);

            // Step 4: formatting
            testSubject.MessageFormat = "XXX{0}YYY";
            // Act
            progressEvents.SimulateStepExecutionChanged(formattedMessage2, 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1, "XXX" + formattedMessage2 + "YYY");

            // Step 5: different message than the previous one
            testSubject.MessageFormat = null;
            // Act
            progressEvents.SimulateStepExecutionChanged(message1, 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1, "XXX" + formattedMessage2 + "YYY", message1);

            // Step 6: dispose
            testSubject.Dispose();
            // Act
            progressEvents.SimulateStepExecutionChanged("123", 0);

            // Verify
            outputWindowPane.AssertOutputStrings(message1, "XXX" + formattedMessage2 + "YYY", message1);
        }
    }
}
