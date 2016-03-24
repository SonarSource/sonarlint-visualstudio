//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsInfoBarUIElement.cs" company="SonarSource SA and Microsoft Corporation">
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
using System.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsInfoBarUIElement : IVsInfoBarUIElement
    {
        private int cookies;
        private readonly Dictionary<uint, IVsInfoBarUIEvents> sinks = new Dictionary<uint, IVsInfoBarUIEvents>();

        #region IVsInfoBarUIElement
        int IVsInfoBarUIElement.Advise(IVsInfoBarUIEvents eventSink, out uint cookie)
        {
            Assert.IsNotNull(eventSink);

            cookie = (uint)Interlocked.Increment(ref this.cookies);
            this.sinks[cookie] = eventSink;

            return VSConstants.S_OK;
        }

        int IVsInfoBarUIElement.Close()
        {
            Assert.IsFalse(this.IsClosed, "Already closed");

            this.IsClosed = true;
            this.SimulateClosedEvent();

            return VSConstants.S_OK;
        }

        int IVsUIElement.GetUIObject(out object ppUnk)
        {
            throw new NotImplementedException();
        }

        int IVsInfoBarUIElement.GetUIObject(out object ppUnk)
        {
            throw new NotImplementedException();
        }

        int IVsUIElement.get_DataSource(out IVsUISimpleDataSource ppDataSource)
        {
            throw new NotImplementedException();
        }

        int IVsInfoBarUIElement.get_DataSource(out IVsUISimpleDataSource ppDataSource)
        {
            throw new NotImplementedException();
        }

        int IVsUIElement.put_DataSource(IVsUISimpleDataSource pDataSource)
        {
            throw new NotImplementedException();
        }

        int IVsInfoBarUIElement.put_DataSource(IVsUISimpleDataSource pDataSource)
        {
            throw new NotImplementedException();
        }

        int IVsUIElement.TranslateAccelerator(IVsUIAccelerator pAccel)
        {
            throw new NotImplementedException();
        }

        int IVsInfoBarUIElement.TranslateAccelerator(IVsUIAccelerator pAccel)
        {
            throw new NotImplementedException();
        }

        int IVsInfoBarUIElement.Unadvise(uint cookie)
        {
            Assert.IsTrue(this.sinks.ContainsKey(cookie));

            this.sinks.Remove(cookie);

            return VSConstants.S_OK;
        }
        #endregion

        #region Test helpers
        public bool IsClosed
        {
            get;
            set;
        }

        public void SimulateClickEvent(IVsInfoBarActionItem item = null)
        {
            this.sinks.Values.ToList().ForEach(v => v.OnActionItemClicked(this, item));
        }

        public void SimulateClosedEvent()
        {
            this.sinks.Values.ToList().ForEach(v => v.OnClosed(this));
        }

        public IVsInfoBar Model
        {
            get;
            set;
        }
        #endregion

    }
}
