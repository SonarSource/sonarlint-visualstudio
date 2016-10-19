//-----------------------------------------------------------------------
// <copyright file="ConfigurableConnectionWorkflow.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableConnectionWorkflow : IConnectionWorkflowExecutor
    {
        private readonly ISonarQubeServiceWrapper sonarQubeService;

        private int numberOfCalls;
        private ProjectInformation[] lastConnectedProjects;

        public ConfigurableConnectionWorkflow(ISonarQubeServiceWrapper sonarQubeService)
        {
            if (sonarQubeService == null)
            {
                throw new ArgumentNullException(nameof(sonarQubeService));
            }

            this.sonarQubeService = sonarQubeService;
        }

        #region IConnectionWorkflowExecutor

        void IConnectionWorkflowExecutor.EstablishConnection(ConnectionInformation information)
        {
            this.numberOfCalls++;
            Assert.IsNotNull(information, "Should not request to establish to a null connection");
            // Simulate the expected behavior in product
            if (!this.sonarQubeService.TryGetProjects(information, CancellationToken.None, out this.lastConnectedProjects))
            {
                Assert.Fail("Failed to establish connection");
            }
        }

        #endregion

        #region Test helpers

        public void AssertEstablishConnectionCalled(int expectedNumberOfCalls)
        {
            Assert.AreEqual(expectedNumberOfCalls, this.numberOfCalls, "EstablishConnection was called unexpected number of times");
        }
        #endregion
    }
}
