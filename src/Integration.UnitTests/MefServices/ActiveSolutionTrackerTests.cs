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
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ActiveSolutionTrackerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock();
            this.serviceProvider.RegisterService(typeof(SVsSolution), this.solutionMock);
        }

        [TestMethod]
        public void ActiveSolutionTracker_Dispose()
        {
            // Setup
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;
            testSubject.Dispose();

            // Act
            this.solutionMock.SimulateSolutionClose();
            this.solutionMock.SimulateSolutionOpen();

            // Verify
            counter.Should().Be(0, nameof(testSubject.ActiveSolutionChanged) + " was not expected to be raised since disposed");
        }

        [TestMethod]
        public void ActiveSolutionTracker_RaiseEventOnSolutionOpen()
        {
            // Setup
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;

            // Act
            this.solutionMock.SimulateSolutionOpen();

            // Verify
            counter.Should().Be(1, nameof(testSubject.ActiveSolutionChanged) + " was expected to be raised");
        }

        [TestMethod]
        public void ActiveSolutionTracker_RaiseEventOnSolutionClose()
        {
            // Setup
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;

            // Act
            this.solutionMock.SimulateSolutionClose();

            // Verify
            counter.Should().Be(1, nameof(testSubject.ActiveSolutionChanged) + " was expected to be raised");
        }

        [TestMethod]
        public void ActiveSolutionTracker_DontRaiseEventOnProjectChanges()
        {
            // Setup
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;
            var project = this.solutionMock.AddOrGetProject("project", isLoaded:false);

            // Act
            this.solutionMock.SimulateProjectLoad(project);
            this.solutionMock.SimulateProjectUnload(project);
            this.solutionMock.SimulateProjectOpen(project);
            this.solutionMock.SimulateProjectClose(project);

            // Verify
            counter.Should().Be(0, nameof(testSubject.ActiveSolutionChanged) + " was not expected to be raised");
        }
    }
}
