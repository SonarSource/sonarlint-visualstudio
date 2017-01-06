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

using EnvDTE;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// SonarQube-bound project discovery
    /// </summary>
    internal interface ISolutionBindingInformationProvider : ILocalService
    {
        /// <summary>
        /// Return all the SonarQube bound projects in the current solution.
        /// It's up to the caller to make sure that the solution is fully loaded before calling this method.
        /// </summary>
        /// <remarks>Internally projects are filtered using the <see cref="IProjectSystemFilter"/> service.
        /// <seealso cref="IProjectSystemHelper.GetFilteredSolutionProjects"/></remarks>
        /// <returns>Will always return an instance, never a null</returns>
        IEnumerable<Project> GetBoundProjects();

        /// <summary>
        /// Return all the SonarQube unbound projects in the current solution.
        /// It's up to the caller to make sure that the solution is fully loaded before calling this method.
        /// </summary>
        /// <remarks>Internally projects are filtered using the <see cref="IProjectSystemFilter"/> service.
        /// <seealso cref="IProjectSystemHelper.GetFilteredSolutionProjects"/></remarks>
        /// <returns>Will always return an instance, never a null</returns>
        IEnumerable<Project> GetUnboundProjects();

        /// <summary>
        /// Returns whether the solution is bound to SonarQube
        /// </summary>
        bool IsSolutionBound();
    }
}
