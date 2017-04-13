/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

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

        #endregion IVsMonitorSelection

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
            ErrorHandler.Succeeded(monitorSelection.GetCmdUIContextCookie(ref contextId, out cookie)).Should().BeTrue();
            ErrorHandler.Succeeded(monitorSelection.SetCmdUIContext(cookie, activate ? 1 : 0)).Should().BeTrue();
        }

        public IEnumerable<Guid> UIContexts
        {
            get
            {
                return this.cmdContexts.Keys;
            }
        }

        #endregion Test helpers
    }
}