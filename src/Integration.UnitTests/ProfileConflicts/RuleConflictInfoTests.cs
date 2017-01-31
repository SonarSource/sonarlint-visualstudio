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

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Xunit;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;
using System.Collections.Generic;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests.ProfileConflicts
{
    public class RuleConflictInfoTests
    {
        [Fact]
        public void Ctor_WithNullRulesMap_ThrowsArgumentNullException()
        {
            // Arrange
            IDictionary<RuleReference, RuleAction> rulesMapNull = null;
            IEnumerable<RuleReference> ruleRefs = new RuleReference[0];

            // Act
            Action act = () => new RuleConflictInfo(ruleRefs, rulesMapNull);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Ctor_WithNullRuleRefs_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<RuleReference> ruleRefsNull = null;
            IDictionary<RuleReference, RuleAction> rulesMap = new Dictionary<RuleReference, RuleAction>();

            // Act
            Action act = () => new RuleConflictInfo(ruleRefsNull, rulesMap);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }
    }
}
