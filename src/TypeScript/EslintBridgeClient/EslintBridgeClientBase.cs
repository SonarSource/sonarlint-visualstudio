﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    internal abstract class EslintBridgeClientBase : IDisposable
    {
        protected readonly string analyzeEndpoint;
        protected readonly IEslintBridgeProcess eslintBridgeProcess;
        protected readonly IEslintBridgeHttpWrapper httpWrapper;
        private readonly IEslintBridgeKeepAlive keepAlive;
        private bool isDisposed;

        protected EslintBridgeClientBase(string analyzeEndpoint, IEslintBridgeProcess eslintBridgeProcess, IEslintBridgeHttpWrapper httpWrapper, IEslintBridgeKeepAlive keepAlive)
        {
            this.analyzeEndpoint = analyzeEndpoint;
            this.eslintBridgeProcess = eslintBridgeProcess;
            this.httpWrapper = httpWrapper;
            this.keepAlive = keepAlive;
        }

        public async Task Close()
        {
            try
            {
                if (eslintBridgeProcess.IsRunning)
                {
                    await EslintBridgeHttpHelper.MakeCallAsync(eslintBridgeProcess, httpWrapper, "close", null, CancellationToken.None);
                }
            }
            catch
            {
                // nothing to do if the call failed
            }
            finally
            {
                eslintBridgeProcess.Stop();
            }
        }

        public async void Dispose()
        {
            await Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async Task Dispose(bool disposing)
        {
            if (disposing && !isDisposed)
            {
                try
                {
                    await Close();
                }
                catch
                {
                    // nothing to do if the call failed
                }

                eslintBridgeProcess.Dispose();
                httpWrapper.Dispose();
                keepAlive.Dispose();
                isDisposed = true;
            }
        }
    }
}
