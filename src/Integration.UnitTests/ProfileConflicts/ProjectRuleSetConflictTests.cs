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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectRuleSetConflictTests
    {
        [TestMethod]
        public void ProjectRuleSetConflict_ArgChecks()
        {
            var info = new RuleSetInformation("projectFullName", "baselineRuleSet", "projectRuleSet", new string[0]);
            var conflict = new RuleConflictInfo();

            Exceptions.Expect<ArgumentNullException>(() => new ProjectRuleSetConflict(null, info));
            Exceptions.Expect<ArgumentNullException>(() => new ProjectRuleSetConflict(conflict, null));

            Assert.IsNotNull(new ProjectRuleSetConflict(conflict, info), "Not expecting this to fail, just to make the static analyzer happy");
        }
    }
}
