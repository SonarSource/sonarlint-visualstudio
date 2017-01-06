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

using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Handlers conflict resolution at the solution level
    /// </summary>
    public interface IConflictsManager : ILocalService
    {
        /// <summary>
        /// Checks the current solution for conflicts
        /// </summary>
        /// <returns>Not null. Will be empty when there are no conflicts</returns>
        /// <remarks>This method is supposed to run just after the solution was bound to SonarQube project. Other cases may return invalid results</remarks>
        IReadOnlyList<ProjectRuleSetConflict> GetCurrentConflicts();
    }
}
