/*
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
using EnvDTE;

namespace SonarLint.VisualStudio.TestInfrastructure
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

        #endregion Test helpers

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

        #endregion UIHierarchy
    }
}