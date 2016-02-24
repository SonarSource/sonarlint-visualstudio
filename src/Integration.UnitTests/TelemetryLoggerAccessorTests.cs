//-----------------------------------------------------------------------
// <copyright file="TelemetryLoggerAccessorTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TelemetryLoggerAccessorTests
    {
        [TestMethod]
        public void TelemetryLoggerAccessor_GetLogger()
        {
            // Setup
            ConfigurableServiceProvider sp = new ConfigurableServiceProvider();
            var loggerInstance = new ConfigurableTelemetryLogger();
            var componentModel = ConfigurableComponentModel.CreateWithExports(
                MefTestHelpers.CreateExport<ITelemetryLogger>(loggerInstance));
            sp.RegisterService(typeof(SComponentModel), componentModel);

            // Act
            ITelemetryLogger logger = TelemetryLoggerAccessor.GetLogger(sp);

            // Verify
            Assert.AreSame(loggerInstance, logger, "Failed to find the MEF service: {0}", nameof(ITelemetryLogger));
        }
    }
}
