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
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ActiveSolutionTrackerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;

        public ActiveSolutionTrackerTests()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock();
            this.serviceProvider.RegisterService(typeof(SVsSolution), this.solutionMock);
        }

        [Fact]
        public void ActiveSolutionTracker_Dispose()
        {
            // Arrange
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;
            testSubject.Dispose();

            // Act
            this.solutionMock.SimulateSolutionClose();
            this.solutionMock.SimulateSolutionOpen();

            // Assert
            counter.Should().Be(0, nameof(testSubject.ActiveSolutionChanged) + " was not expected to be raised since disposed");
        }

        [Fact]
        public void ActiveSolutionTracker_RaiseEventOnSolutionOpen()
        {
            // Arrange
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;

            // Act
            this.solutionMock.SimulateSolutionOpen();

            // Assert
            counter.Should().Be(1, nameof(testSubject.ActiveSolutionChanged) + " was expected to be raised");
        }

        [Fact]
        public void ActiveSolutionTracker_RaiseEventOnSolutionClose()
        {
            // Arrange
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;

            // Act
            this.solutionMock.SimulateSolutionClose();

            // Assert
            counter.Should().Be(1, nameof(testSubject.ActiveSolutionChanged) + " was expected to be raised");
        }

        [Fact]
        public void ActiveSolutionTracker_DontRaiseEventOnProjectChanges()
        {
            // Arrange
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;
            var project = this.solutionMock.AddOrGetProject("project", isLoaded: false);

            // Act
            this.solutionMock.SimulateProjectLoad(project);
            this.solutionMock.SimulateProjectUnload(project);
            this.solutionMock.SimulateProjectOpen(project);
            this.solutionMock.SimulateProjectClose(project);

            // Assert
            counter.Should().Be(0, nameof(testSubject.ActiveSolutionChanged) + " was not expected to be raised");
        }
    }
}
