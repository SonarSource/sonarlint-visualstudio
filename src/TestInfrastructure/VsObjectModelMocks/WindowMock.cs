/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class WindowMock : Window
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