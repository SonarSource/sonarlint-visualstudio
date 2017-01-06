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

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Provides the UX for RuleSet conflict - detection and auto-fix
    /// </summary>
    /// <seealso cref="IConflictsManager"/>
    /// <seealso cref="IRuleSetInspector"/>
    internal interface IRuleSetConflictsController : ILocalService
    {
        /// <summary>
        /// Checks whether the current solution has projects with conflicts RuleSets.
        /// The check is against the solution level RuleSet (if solution is bound).
        /// </summary>
        /// <returns>Whether has conflicts (in which case there will be a UX to auto-fix them as well)</returns>
        bool CheckForConflicts();

        /// <summary>
        /// Clears any UX that was activated part of <see cref="CheckForConflicts"/>
        /// </summary>
        void Clear();
    }
}
