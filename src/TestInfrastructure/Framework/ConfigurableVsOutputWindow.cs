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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsOutputWindow : IVsOutputWindow
    {
        private readonly IDictionary<Guid, IVsOutputWindowPane> panes = new Dictionary<Guid, IVsOutputWindowPane>();

        #region IVsOutputWindow

        int IVsOutputWindow.CreatePane(ref Guid rguidPane, string pszPaneName, int fInitVisible, int fClearWithSolution)
        {
            if (!this.HasPane(rguidPane))
            {
                var pane = new ConfigurableVsOutputWindowPane(pszPaneName,
                    Convert.ToBoolean(fInitVisible),
                    Convert.ToBoolean(fClearWithSolution));

                this.panes[rguidPane] = pane;
            }

            return VSConstants.S_OK;
        }

        int IVsOutputWindow.DeletePane(ref Guid rguidPane)
        {
            if (this.panes.ContainsKey(rguidPane))
            {
                this.panes.Remove(rguidPane);
                return VSConstants.S_OK;
            }

            return VSConstants.E_FAIL;
        }

        int IVsOutputWindow.GetPane(ref Guid rguidPane, out IVsOutputWindowPane ppPane)
        {
            if (this.panes.ContainsKey(rguidPane))
            {
                ppPane = this.panes[rguidPane];
                return VSConstants.S_OK;
            }

            ppPane = null;
            return VSConstants.E_FAIL;
        }

        #endregion

        public void AssertPaneExists(Guid paneId)
        {
            this.HasPane(paneId).Should().BeTrue($"Expected output window pane '{paneId}' to exist");
        }

        public bool HasPane(Guid paneId)
        {
            return this.panes.ContainsKey(paneId);
        }

        public void RegisterPane(Guid paneId, IVsOutputWindowPane pane)
        {
            this.panes[paneId] = pane;
        }

        public ConfigurableVsOutputWindowPane GetOrCreatePane(Guid paneId)
        {
            if (!this.HasPane(paneId))
            {
                this.RegisterPane(paneId, new ConfigurableVsOutputWindowPane());
            }

            var newPane = this.panes[paneId] as ConfigurableVsOutputWindowPane;
            newPane.Should().NotBeNull($"Expected pane to be of type {nameof(ConfigurableVsOutputWindowPane)}");

            return newPane;
        }

        public ConfigurableVsOutputWindowPane GetOrCreateSonarLintPane()
        {
            return this.GetOrCreatePane(VsShellUtils.SonarLintOutputPaneGuid);
        }
    }
}
