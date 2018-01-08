/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
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

        #endregion IVsOutputWindow

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
            if (newPane == null)
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith($"Expected pane to be of type {nameof(ConfigurableVsOutputWindowPane)}");
            }

            return newPane;
        }

        public ConfigurableVsOutputWindowPane GetOrCreateSonarLintPane()
        {
            return this.GetOrCreatePane(VsShellUtils.SonarLintOutputPaneGuid);
        }
    }
}