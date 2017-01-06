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

using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    internal static class IProjectSystemHelperExtensions
    {
        /// <summary>
        /// Returns whether or not a project is of a known test project type.
        /// </summary>
        public static bool IsKnownTestProject(this IProjectSystemHelper projectSystem, IVsHierarchy vsProject)
        {
            if (projectSystem == null)
            {
                throw new ArgumentNullException(nameof(projectSystem));
            }

            if (vsProject == null)
            {
                throw new ArgumentNullException(nameof(vsProject));
            }

            return projectSystem.GetAggregateProjectKinds(vsProject).Contains(ProjectSystemHelper.TestProjectKindGuid);
        }
    }
}
