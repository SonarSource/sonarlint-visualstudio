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

using EnvDTE;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class UIHierarchyMock : UIHierarchy
    {
        private readonly DTEMock dte;

        public UIHierarchyMock(DTEMock dte)
        {
            this.dte = dte;
            this.Window = new WindowMock();
        }

        #region Test helpers
        public WindowMock Window
        {
            get;
            set;
        }
        #endregion

        #region UIHierarchy
        public DTE DTE
        {
            get
            {
                return this.dte;
            }
        }

        public Window Parent
        {
            get
            {
                return this.Window;
            }
        }

        public object SelectedItems
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public UIHierarchyItems UIHierarchyItems
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void DoDefaultAction()
        {
            throw new NotImplementedException();
        }

        public UIHierarchyItem GetItem(string Names)
        {
            throw new NotImplementedException();
        }

        public void SelectDown(vsUISelectionType How, int Count)
        {
            throw new NotImplementedException();
        }

        public void SelectUp(vsUISelectionType How, int Count)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
