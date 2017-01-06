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
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Data-only class that references the fixed <see cref="RuleSet"/> and has the list of fixes applied to get it into that state
    /// </summary>
    public class FixedRuleSetInfo
    {
        public FixedRuleSetInfo(RuleSet ruleSet, IEnumerable<string> includesReset, IEnumerable<string> rulesDeleted)
        {
            if (ruleSet == null)
            {
                throw new ArgumentNullException(nameof(ruleSet));
            }

            this.FixedRuleSet = ruleSet;
            this.IncludesReset = includesReset ?? Enumerable.Empty<string>();
            this.RulesDeleted = rulesDeleted ?? Enumerable.Empty<string>();
        }

        public RuleSet FixedRuleSet { get; }

        public IEnumerable<string> IncludesReset { get; }

        public IEnumerable<string> RulesDeleted { get; }
    }
}
