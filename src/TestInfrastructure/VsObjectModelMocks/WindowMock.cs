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

using System;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class WindowMock : Window
    {
        #region Test helpers

        public bool Active
        {
            get;
            set;
        }

        #endregion Test helpers

        #region Window

        bool Window.AutoHides
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        string Window.Caption
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        EnvDTE.Windows Window.Collection
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ContextAttributes Window.ContextAttributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Document Window.Document
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        DTE Window.DTE
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        int Window.Height
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        int Window.HWnd
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool Window.IsFloating
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        string Window.Kind
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        int Window.Left
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        bool Window.Linkable
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        Window Window.LinkedWindowFrame
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        LinkedWindows Window.LinkedWindows
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object Window.Object
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string Window.ObjectKind
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Project Window.Project
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ProjectItem Window.ProjectItem
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object Window.Selection
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        int Window.Top
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        vsWindowType Window.Type
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool Window.Visible
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        int Window.Width
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        vsWindowState Window.WindowState
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        void Window.Activate()
        {
            this.Active = true;
        }

        void Window.Attach(int lWindowHandle)
        {
            throw new NotImplementedException();
        }

        void Window.Close(vsSaveChanges SaveChanges)
        {
            throw new NotImplementedException();
        }

        void Window.Detach()
        {
            throw new NotImplementedException();
        }

        object Window.get_DocumentData(string bstrWhichData)
        {
            throw new NotImplementedException();
        }

        void Window.SetFocus()
        {
            throw new NotImplementedException();
        }

        void Window.SetKind(vsWindowType eKind)
        {
            throw new NotImplementedException();
        }

        void Window.SetSelectionContainer(ref object[] Objects)
        {
            throw new NotImplementedException();
        }

        void Window.SetTabPicture(object Picture)
        {
            throw new NotImplementedException();
        }

        #endregion Window
    }
}