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
    public interface IProjectPropertyManager
    {
        /// <summary>
        /// Get all currently selected projects in the Solution Explorer.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Project> GetSelectedProjects();

        /// <summary>
        /// Get the project property value with the given name.
        /// </summary>
        /// <remarks>Will only look in unconditional ItemGroups</remarks>
        /// <param name="project">Project to get the property value for</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>Null if the property is undefined or not a boolean, true/false otherwise</returns>
        bool? GetBooleanProperty(Project project, string propertyName);

        /// <summary>
        /// Set the project property value with the given name.
        /// </summary>
        /// <remarks>
        /// Will only look in unconditional ItemGroups.<para/>
        /// Using null as the <paramref name="value"/> will clear the property.
        /// </remarks>
        /// <param name="project">Project to set the property value for</param>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Property value</param>
        void SetBooleanProperty(Project project, string propertyName, bool? value);
    }
}