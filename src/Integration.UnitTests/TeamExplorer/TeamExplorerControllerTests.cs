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

using Microsoft.TeamFoundation.Controls;
using Xunit;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    public class TeamExplorerControllerTests
    {
        [Fact]
        public void Ctor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new TeamExplorerController(null);

            // Assert

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Ctor_WithInvalidServiceProvider_ThrowsArgumentException()
        {
            // Arrange
            var serviceProvider = new ConfigurableServiceProvider(false);

            // Act
            Action act = () => new TeamExplorerController(serviceProvider);

            // Assert
            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void TeamExplorerController_Ctor()
        {
            // Test case 1: no Team Explorer service
            // Arrange
            var serviceProvider = new ConfigurableServiceProvider(false);
            // Test case 2: has TE service
            // Arrange
            var teService = new ConfigurableTeamExplorer();
            serviceProvider.RegisterService(typeof(ITeamExplorer), teService);

            // Act + Assert
            var testSubject = new TeamExplorerController(serviceProvider);
            testSubject.TeamExplorer.Should().Be(teService, "Unexpected Team Explorer service");
        }

        [Fact]
        public void TeamExplorerController_ShowConnectionsPage()
        {
            // Arrange
            var startPageId = new Guid(TeamExplorerPageIds.GitCommits);

            var serviceProvider = new ConfigurableServiceProvider();
            var teService = new ConfigurableTeamExplorer(startPageId);
            serviceProvider.RegisterService(typeof(ITeamExplorer), teService);

            var sonarPageId = new Guid(SonarQubePage.PageId);
            var sonarPageInstance = new ConfigurableTeamExplorerPage(sonarPageId);
            teService.AvailablePages.Add(sonarPageId, sonarPageInstance);

            var testSubject = new TeamExplorerController(serviceProvider);

            // Act
            testSubject.ShowSonarQubePage();

            // Assert
            teService.AssertCurrentPage(sonarPageId);
        }
    }
}
