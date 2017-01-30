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
 using Xunit;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.UnitTests.State
{
    public class TransferableVisualStateTests
    {
        public TransferableVisualStateTests()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [Fact]
        public void TransferableVisualState_DefaultState()
        {
            // Arrange
            var testSubject = new TransferableVisualState();

            // Assert
            testSubject.HasBoundProject
                .Should().BeFalse();
            testSubject.IsBusy
                .Should().BeFalse();
            testSubject.ConnectedServers.Should().NotBeNull();
            testSubject.ConnectedServers.Should().HaveCount(0);
        }

        [Fact]
        public void TransferableVisualState_BoundProjectManagement()
        {
            // Arrange
            var testSubject = new TransferableVisualState();
            var server = new ServerViewModel(new Integration.Service.ConnectionInformation(new System.Uri("http://server")));
            var project1 = new ProjectViewModel(server, new Integration.Service.ProjectInformation());
            var project2 = new ProjectViewModel(server, new Integration.Service.ProjectInformation());

            // Act (bind to something)
            testSubject.SetBoundProject(project1);

            // Assert
            testSubject.HasBoundProject.Should().BeTrue();
            project1.IsBound.Should().BeTrue();
            project2.IsBound.Should().BeFalse();
            server.ShowAllProjects.Should().BeFalse();

            // Act (bind to something else)
            testSubject.SetBoundProject(project2);

            // Assert
            testSubject.HasBoundProject.Should().BeTrue();
            project1.IsBound.Should().BeFalse();
            project2.IsBound.Should().BeTrue();
            server.ShowAllProjects.Should().BeFalse();

            // Act(clear binding)
            testSubject.ClearBoundProject();

            // Assert
            testSubject.HasBoundProject.Should().BeFalse();
            project1.IsBound.Should().BeFalse();
            project2.IsBound.Should().BeFalse();
            server.ShowAllProjects.Should().BeTrue();
        }
    }
}
