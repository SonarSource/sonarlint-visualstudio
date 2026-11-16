/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using System.Runtime.InteropServices;
using System.Threading;

namespace SonarLint.VisualStudio.E2E
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class ComMessageFilter : IOleMessageFilter, IDisposable
    {
        private const int ServerCallRetryLater = 2;
        private const int RetryDelayMilliseconds = 250;
        private const uint RetryTimeoutMilliseconds = 30000;
        private IOleMessageFilter previousFilter;
        private int registeredThreadId;

        public int BusyCallCount { get; private set; }

        public static ComMessageFilter Register()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException("The COM message filter requires an STA thread.");
            }
            var filter = new ComMessageFilter();
            var result = CoRegisterMessageFilter(filter, out filter.previousFilter);
            if (result != 0)
            {
                throw new InvalidOperationException($"Could not register the COM message filter (HRESULT 0x{result:X8}).");
            }
            filter.registeredThreadId = Thread.CurrentThread.ManagedThreadId;
            return filter;
        }

        public int HandleInComingCall(int callType, IntPtr taskCaller, int tickCount, IntPtr interfaceInfo) => 0;

        public int RetryRejectedCall(IntPtr taskCallee, uint elapsedMilliseconds, int rejectType)
        {
            if (rejectType != ServerCallRetryLater) { return -1; }
            BusyCallCount++;
            // Do not retry permanent rejections or wait indefinitely for a busy IDE.
            return elapsedMilliseconds < RetryTimeoutMilliseconds ? RetryDelayMilliseconds : -1;
        }

        public int MessagePending(IntPtr taskCallee, int tickCount, int pendingType) => 2;

        public void Dispose()
        {
            if (registeredThreadId == 0) { return; }
            if (registeredThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new InvalidOperationException("Restore the COM message filter on the thread that registered it.");
            }
            var result = CoRegisterMessageFilter(previousFilter, out _);
            if (result != 0)
            {
                throw new InvalidOperationException($"Could not restore the COM message filter (HRESULT 0x{result:X8}).");
            }
            registeredThreadId = 0;
            previousFilter = null;
        }

        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldFilter);
    }

    [ComImport]
    [Guid("00000016-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleMessageFilter
    {
        [PreserveSig]
        int HandleInComingCall(int callType, IntPtr taskCaller, int tickCount, IntPtr interfaceInfo);

        [PreserveSig]
        int RetryRejectedCall(IntPtr taskCallee, uint elapsedMilliseconds, int rejectType);

        [PreserveSig]
        int MessagePending(IntPtr taskCallee, int tickCount, int pendingType);
    }
}
