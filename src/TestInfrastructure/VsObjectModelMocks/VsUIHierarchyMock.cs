//-----------------------------------------------------------------------
// <copyright file="VsUIHierarchyMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class VsUIHierarchyMock : IVsUIHierarchy
    {
        private static int allocatedItemIds = 0;
        private readonly Dictionary<uint, Dictionary<int, object>> properties = new Dictionary<uint, Dictionary<int, object>>();
        private readonly Dictionary<uint, IVsHierarchyEvents> sinks = new Dictionary<uint, IVsHierarchyEvents>();
        private int sinkCookies = 0;

        protected static uint AllocateItemId()
        {
            return (uint)Interlocked.Increment(ref allocatedItemIds);
        }

        public VsUIHierarchyMock(string filePath)
            : this(filePath, AllocateItemId())
        {
        }

        public VsUIHierarchyMock(string filePath, uint itemId)
        {
            this.FilePath = filePath;
            this.ItemId = itemId;
        }

        public uint ItemId
        {
            get;
        }

        public string FilePath
        {
            get;
        }

        public void SetProperty(uint itemId, int propId, object value)
        {
            Dictionary<int, object> itemProperties;
            if (!this.properties.TryGetValue(itemId, out itemProperties))
            {
                this.properties[itemId] = itemProperties = new Dictionary<int, object>();
            }
            itemProperties[propId] = value;
        }

        public void RemoveProperty(uint itemId, int propId)
        {
            this.properties[itemId].Remove(propId);
        }

        public void RemoveProperties(uint itemId)
        {
            this.properties.Remove(itemId);
        }

        protected void SimulateSccChange(uint itemId)
        {
            this.sinks.Values.ToList().ForEach(s =>
            {
                s.OnPropertyChanged(itemId, (int)__VSHPROPID.VSHPROPID_StateIconIndex, 0);
            });
        }

        public void AssertNoHierarchyEventSinks()
        {
            Assert.AreEqual(0, this.sinks.Count, "Unexpected number of sync, forgot to Unadvise?");
        }

        #region IVsUIHierarchy
        int IVsHierarchy.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.QueryClose(out int pfCanClose)
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.Close()
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.GetGuidProperty(uint itemid, int propid, out Guid pguid)
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.SetGuidProperty(uint itemid, int propid, ref Guid rguid)
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.GetProperty(uint itemid, int propid, out object pvar)
        {
            pvar = null;
            Dictionary<int, object> itemProperties;
            if (this.properties.TryGetValue(itemid, out itemProperties))
            {
                if (itemProperties.TryGetValue(propid, out pvar))
                {
                    return VSConstants.S_OK;
                }
            }

            return VSConstants.E_FAIL;
        }

        int IVsHierarchy.SetProperty(uint itemid, int propid, object var)
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested)
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.GetCanonicalName(uint itemid, out string pbstrName)
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.ParseCanonicalName(string pszName, out uint pitemid)
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.Unused0()
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
        {
            pdwCookie = (uint)Interlocked.Increment(ref this.sinkCookies);
            this.sinks[pdwCookie] = pEventSink;
            return VSConstants.S_OK;
        }

        int IVsHierarchy.UnadviseHierarchyEvents(uint dwCookie)
        {
            bool existed = this.sinks.ContainsKey(dwCookie);
            this.sinks.Remove(dwCookie);
            return existed ? VSConstants.S_OK : VSConstants.E_FAIL;
        }

        int IVsHierarchy.Unused1()
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.Unused2()
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.Unused3()
        {
            throw new NotImplementedException();
        }

        int IVsHierarchy.Unused4()
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.QueryClose(out int pfCanClose)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.Close()
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.GetGuidProperty(uint itemid, int propid, out Guid pguid)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.SetGuidProperty(uint itemid, int propid, ref Guid rguid)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.GetProperty(uint itemid, int propid, out object pvar)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.SetProperty(uint itemid, int propid, object var)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.GetCanonicalName(uint itemid, out string pbstrName)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.ParseCanonicalName(string pszName, out uint pitemid)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.Unused0()
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.UnadviseHierarchyEvents(uint dwCookie)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.Unused1()
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.Unused2()
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.Unused3()
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.Unused4()
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.QueryStatusCommand(uint itemid, ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            throw new NotImplementedException();
        }

        int IVsUIHierarchy.ExecCommand(uint itemid, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
