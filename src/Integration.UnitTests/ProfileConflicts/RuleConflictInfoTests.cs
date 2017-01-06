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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests.ProfileConflicts
{
    [TestClass]
    public class RuleConflictInfoTests
    {
        [TestMethod]
        public void RuleConflictInfo_Ctor_ArgChecks()
        {
            // Setup
            IEnumerable<RuleReference> ruleRefs = null;
            IDictionary<RuleReference, RuleAction> rulesMap = null;

            IEnumerable<RuleReference> ruleRefsNull = new RuleReference[0];
            IDictionary<RuleReference, RuleAction> rulesMapNull = new Dictionary<RuleReference, RuleAction>();

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => new RuleConflictInfo(ruleRefsNull, rulesMap));
            Exceptions.Expect<ArgumentNullException>(() => new RuleConflictInfo(ruleRefs, rulesMapNull));
        }
    }
}
