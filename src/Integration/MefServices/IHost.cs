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

using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration
{
    internal interface IHost : IServiceProvider
    {
        /// <summary>
        /// The UI thread dispatcher. Not null.
        /// </summary>
        Dispatcher UIDispatcher { get; }

        /// <summary>
        /// <see cref="ISonarQubeServiceWrapper"/>. Not null.
        /// </summary>
        ISonarQubeServiceWrapper SonarQubeService { get; }

        /// <summary>
        /// The visual state manager. Not null.
        /// </summary>
        IStateManager VisualStateManager { get; }

        /// <summary>
        /// The currently active section. Null when no active section.
        /// </summary>
        ISectionController ActiveSection { get; }

        /// <summary>
        /// Sets the <see cref="ActiveSection"/> with the specified <paramref name="section"/>
        /// </summary>
        /// <param name="section">Required</param>
        void SetActiveSection(ISectionController section);

        /// <summary>
        /// Change event when the <see cref="ActiveSection"/> changed
        /// </summary>
        event EventHandler ActiveSectionChanged;

        /// <summary>
        /// Clears the <see cref="ActiveSection"/>
        /// </summary>
        void ClearActiveSection();
    }
}
