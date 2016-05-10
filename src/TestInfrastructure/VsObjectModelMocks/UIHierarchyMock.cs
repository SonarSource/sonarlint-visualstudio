//-----------------------------------------------------------------------
// <copyright file="UIHierarchyMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
