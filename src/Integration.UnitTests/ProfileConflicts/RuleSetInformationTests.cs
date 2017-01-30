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


using Xunit;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests.ProfileConflicts
{
    public class RuleSetInformationTests
    {
        [Fact]
        public void Ctor_WithNullProjectName_ThrowsArgumentNullException()
        {
            // Arrange
            string baselineRuleSet = "br";
            string projectRuleSet = "pr";

            // Act
            Action act = () => new RuleSetInformation(null, baselineRuleSet, projectRuleSet, null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Ctor_WithNullBaselineRuleSet_ThrowsArgumentNullException()
        {
            // Arrange
            string projectFullName = "p";
            string projectRuleSet = "pr";

            // Act
            Action act = () => new RuleSetInformation(projectFullName, null, projectRuleSet, null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Ctor_WithNullProjectRuleSet_ThrowsArgumentNullException()
        {
            // Arrange
            string projectFullName = "p";
            string baselineRuleSet = "br";

            // Act
            Action act = () => new RuleSetInformation(projectFullName, baselineRuleSet, null, null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }
    }
}
