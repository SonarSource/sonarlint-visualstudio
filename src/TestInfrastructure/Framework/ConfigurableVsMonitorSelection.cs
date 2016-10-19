//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsMonitorSelection.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsMonitorSelection : IVsMonitorSelection
    {
        private readonly Dictionary<Guid, uint> cmdContexts = new Dictionary<Guid, uint>();
        private readonly Dictionary<uint, bool> activeContexts = new Dictionary<uint, bool>();
        private readonly Dictionary<uint, IVsSelectionEvents> synks = new Dictionary<uint, IVsSelectionEvents>();
        private int allocatedSynks = 0;

        #region IVsMonitorSelection
        int IVsMonitorSelection.AdviseSelectionEvents(IVsSelectionEvents pSink, out uint pdwCookie)
        {
            pdwCookie = (uint)++allocatedSynks;
            synks.Add(pdwCookie, pSink);
            return VSConstants.S_OK;
        }

        int IVsMonitorSelection.GetCmdUIContextCookie(ref Guid rguidCmdUI, out uint pdwCmdUICookie)
        {
            if (!cmdContexts.TryGetValue(rguidCmdUI, out pdwCmdUICookie))
            {
                cmdContexts[rguidCmdUI] = pdwCmdUICookie = (uint)cmdContexts.Count + 1;
                activeContexts[pdwCmdUICookie] = false;
            }

            return VSConstants.S_OK;
        }

        int IVsMonitorSelection.GetCurrentElementValue(uint elementid, out object pvarValue)
        {
            throw new NotImplementedException();
        }

        int IVsMonitorSelection.GetCurrentSelection(out IntPtr ppHier, out uint pitemid, out IVsMultiItemSelect ppMIS, out IntPtr ppSC)
        {
            throw new NotImplementedException();
        }

        int IVsMonitorSelection.IsCmdUIContextActive(uint dwCmdUICookie, out int pfActive)
        {
            pfActive = 0;
            bool active;
            if (this.activeContexts.TryGetValue(dwCmdUICookie, out active))
            {
                pfActive = active ? 1 : 0;
                return VSConstants.S_OK;
            }
            else
            {
                return VSConstants.DISP_E_PARAMNOTFOUND;
            }
        }

        int IVsMonitorSelection.SetCmdUIContext(uint dwCmdUICookie, int fActive)
        {
            if (this.cmdContexts.Values.Any(v => v == dwCmdUICookie))
            {
                this.activeContexts[dwCmdUICookie] = fActive != 0;
                this.synks.Values.ToList().ForEach(s => s.OnCmdUIContextChanged(dwCmdUICookie, fActive));
                return VSConstants.S_OK;
            }
            else
            {
                return VSConstants.DISP_E_PARAMNOTFOUND;
            }
        }

        int IVsMonitorSelection.UnadviseSelectionEvents(uint dwCookie)
        {
            if (synks.Remove(dwCookie))
            {
                return VSConstants.S_OK;
            }
            return VSConstants.E_FAIL;
        }
        #endregion

        #region Test helpers
        public void RegisterKnownContext(Guid contextId)
        {
            uint unusedCookie;
            ((IVsMonitorSelection)this).GetCmdUIContextCookie(ref contextId, out unusedCookie);
        }

        public void SetContext(Guid contextId, bool activate)
        {
            uint cookie;

            var monitorSelection = (IVsMonitorSelection)this;
            Assert.IsTrue(ErrorHandler.Succeeded(monitorSelection.GetCmdUIContextCookie(ref contextId, out cookie)));
            Assert.IsTrue(ErrorHandler.Succeeded(monitorSelection.SetCmdUIContext(cookie, activate ? 1 : 0)));
        }

        public IEnumerable<Guid> UIContexts
        {
            get
            {
                return this.cmdContexts.Keys;
            }
        }
        #endregion
    }
}
