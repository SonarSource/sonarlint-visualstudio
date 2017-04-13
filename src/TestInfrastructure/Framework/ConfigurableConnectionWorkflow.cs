/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using FluentAssertions;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Service;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableConnectionWorkflow : IConnectionWorkflowExecutor
    {
        private readonly ISonarQubeServiceWrapper sonarQubeService;

        internal int NumberOfCalls { get; private set; }
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
            this.NumberOfCalls++;
            information.Should().NotBeNull("Should not request to establish to a null connection");
            // Simulate the expected behavior in product
            if (!this.sonarQubeService.TryGetProjects(information, CancellationToken.None, out this.lastConnectedProjects))
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith("Failed to establish connection");
            }
        }

        #endregion IConnectionWorkflowExecutor
    }
}