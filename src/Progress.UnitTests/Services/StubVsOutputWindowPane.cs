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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation for <see cref="IVsOutputWindowPane"/>
    /// </summary>
    public class StubVsOutputWindowPane : IVsOutputWindowPane
    {
        public bool IsActivated { get; private set; }
        public bool IsWrittenToOutputWindow { get; private set; }

        #region Configuration
        public Action<string> OutputStringThreadSafeAction { get; set; }
        #endregion

        #region IVsOutputWindowPane
        int IVsOutputWindowPane.Activate()
        {
            this.IsActivated = true;
            return VSConstants.S_OK;
        }

        int IVsOutputWindowPane.Clear()
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.FlushToTaskList()
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.GetName(ref string pbstrPaneName)
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.Hide()
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.OutputString(string pszOutputString)
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.OutputStringThreadSafe(string pszOutputString)
        {
            this.IsWrittenToOutputWindow = true;
            this.OutputStringThreadSafeAction?.Invoke(pszOutputString);

            return VSConstants.S_OK;
        }

        int IVsOutputWindowPane.OutputTaskItemString(string pszOutputString, VSTASKPRIORITY priority, VSTASKCATEGORY category, string pszSubcategory, int bitmap, string pszFilename, uint lineNum, string pszTaskItemText)
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.OutputTaskItemStringEx(string pszOutputString, VSTASKPRIORITY priority, VSTASKCATEGORY category, string pszSubcategory, int bitmap, string pszFilename, uint lineNum, string pszTaskItemText, string pszLookupKwd)
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.SetName(string pszPaneName)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Test helpers
        public void Reset()
        {
            this.IsActivated = false;
            this.IsWrittenToOutputWindow = false;
        }
        #endregion
    }
}
