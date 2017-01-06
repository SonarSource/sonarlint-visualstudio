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

using Microsoft.TeamFoundation.Client.CommandTarget;
using System;

namespace SonarLint.VisualStudio.Integration
{
    internal abstract class HostedCommandControllerBase : IOleCommandTarget
    {
        protected HostedCommandControllerBase(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider
        {
            get;
        }

        #region IOleCommandTarget
        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return this.OnQueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return this.OnExec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
        #endregion

        #region Ole command extensibility
        /// <summary>
        /// Redirected call from <see cref="IOleCommandTarget.QueryStatus(ref Guid, uint, OLECMD[], IntPtr)"/>
        /// </summary>
        protected virtual int OnQueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return (int)OleConstants.OLECMDERR_E_UNKNOWNGROUP;

        }

        /// <summary>
        /// Redirected call from <see cref="IOleCommandTarget.Exec(ref Guid, uint, uint, IntPtr, IntPtr)"/>
        /// </summary>
        protected virtual int OnExec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return (int)OleConstants.OLECMDERR_E_UNKNOWNGROUP;
        }
        #endregion
    }
}
