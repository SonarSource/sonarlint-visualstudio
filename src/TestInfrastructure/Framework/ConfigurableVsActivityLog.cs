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

using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsActivityLog : IVsActivityLog
    {
        #region IVsActivityLog
        int IVsActivityLog.LogEntry(uint actType, string pszSource, string pszDescription)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryGuid(uint actType, string pszSource, string pszDescription, Guid guid)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryGuidHr(uint actType, string pszSource, string pszDescription, Guid guid, int hr)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryGuidHrPath(uint actType, string pszSource, string pszDescription, Guid guid, int hr, string pszPath)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryGuidPath(uint actType, string pszSource, string pszDescription, Guid guid, string pszPath)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryHr(uint actType, string pszSource, string pszDescription, int hr)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryHrPath(uint actType, string pszSource, string pszDescription, int hr, string pszPath)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryPath(uint actType, string pszSource, string pszDescription, string pszPath)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }
        #endregion
    }
}
