/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
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