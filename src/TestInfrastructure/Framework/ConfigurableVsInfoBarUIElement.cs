/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsInfoBarUIElement : IVsInfoBarUIElement
    {
        private int cookies;
        private readonly Dictionary<uint, IVsInfoBarUIEvents> sinks = new Dictionary<uint, IVsInfoBarUIEvents>();

        #region IVsInfoBarUIElement

        int IVsInfoBarUIElement.Advise(IVsInfoBarUIEvents eventSink, out uint cookie)
        {
            eventSink.Should().NotBeNull();

            cookie = (uint)Interlocked.Increment(ref this.cookies);
            this.sinks[cookie] = eventSink;

            return VSConstants.S_OK;
        }

        int IVsInfoBarUIElement.Close()
        {
            this.IsClosed.Should().BeFalse("Already closed");

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
            this.sinks.Should().ContainKey(cookie);

            this.sinks.Remove(cookie);

            return VSConstants.S_OK;
        }

        #endregion IVsInfoBarUIElement

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

        #endregion Test helpers
    }
}