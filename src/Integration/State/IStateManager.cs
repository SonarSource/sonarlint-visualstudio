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
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.State
{
    /// <summary>
    /// Manages the view model state (also encapsulates it)
    /// </summary>
    internal interface IStateManager
    {
        /// <summary>
        /// The underlying managed visual state
        /// </summary>
        /// <remarks>The state should not be manipulated directly, it exposed only for data binding purposes</remarks>
        TransferableVisualState ManagedState { get; }

        /// <summary>
        /// Event fired when <see cref="IsBusy"/> is changed. The arguments will include the new value.
        /// </summary>
        event EventHandler<bool> IsBusyChanged;

        /// <summary>
        /// Event fired when the SonarQube project binding of the solution changes.
        /// </summary>
        event EventHandler BindingStateChanged;

        bool IsBusy { get; set; }

        bool HasBoundProject { get; }

        bool IsConnected { get; }

        string BoundProjectKey { get; set; }

        IEnumerable<ConnectionInformation> GetConnectedServers();

        ConnectionInformation GetConnectedServer(ProjectInformation project);

        void SetProjects(ConnectionInformation connection, IEnumerable<ProjectInformation> projects);

        void SetBoundProject(ProjectInformation project);

        void ClearBoundProject();

        void SyncCommandFromActiveSection();
    }
}
