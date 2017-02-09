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

using FluentAssertions;
using SonarLint.VisualStudio.Integration.Progress;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableProgressStepRunner : IProgressStepRunnerWrapper
    {
        private int abortAllNumberOfCalls;
        private IProgressControlHost currentHost;

        #region IProgressStepRunnerWrapper

        void IProgressStepRunnerWrapper.AbortAll()
        {
            this.abortAllNumberOfCalls++;
        }

        void IProgressStepRunnerWrapper.ChangeHost(IProgressControlHost host)
        {
            host.Should().NotBeNull();

            this.currentHost = host;
        }

        #endregion IProgressStepRunnerWrapper

        #region Test helper

        public void AssertAbortAllCalled(int expectedNumberOfTimes)
        {
            this.abortAllNumberOfCalls.Should().Be(expectedNumberOfTimes, "AbortAll was not called expected number of times");
        }

        public void AssertCurrentHost(IProgressControlHost expectedHost)
        {
            this.currentHost.Should().Be(expectedHost);
        }

        public void AssertNoCurrentHost()
        {
            this.currentHost.Should().BeNull();
        }

        #endregion Test helper
    }
}