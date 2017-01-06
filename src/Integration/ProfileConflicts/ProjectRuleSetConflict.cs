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

using System;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Data-only class that a <see cref="RuleConflictInfo"/> and also captures the information used to find that conflict
    /// </summary>
    /// <seealso cref="IRuleSetInspector"/>
    public class ProjectRuleSetConflict
    {
        public ProjectRuleSetConflict(RuleConflictInfo conflict, RuleSetInformation aggregate)
        {
            if (conflict == null)
            {
                throw new ArgumentNullException(nameof(conflict));
            }

            if (aggregate == null)
            {
                throw new ArgumentNullException(nameof(aggregate));
            }


            this.Conflict = conflict;
            this.RuleSetInfo = aggregate;
        }

        public RuleSetInformation RuleSetInfo { get; }

        public RuleConflictInfo Conflict { get; }
    }
}