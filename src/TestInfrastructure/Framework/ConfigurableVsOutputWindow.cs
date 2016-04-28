//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsOutputWindow.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

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
            Assert.IsTrue(this.HasPane(paneId), $"Expected output window pane '{paneId}' to exist");
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
                Assert.Inconclusive($"Expected pane to be of type {nameof(ConfigurableVsOutputWindowPane)}");
            }

            return newPane;
        }

        public ConfigurableVsOutputWindowPane GetOrCreateSonarLintPane()
        {
            return this.GetOrCreatePane(VsShellUtils.SonarLintOutputPaneGuid);
        }
    }
}
