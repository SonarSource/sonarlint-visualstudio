//-----------------------------------------------------------------------
// <copyright file="WindowMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System;

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
        #endregion

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
        #endregion
    }
}
