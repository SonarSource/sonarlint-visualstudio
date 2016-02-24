//-----------------------------------------------------------------------
// <copyright file="ToolWindowsMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using EnvDTE80;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ToolWindowsMock : ToolWindows
    {
        private DTEMock parent;

        public ToolWindowsMock(DTEMock parent)
        {
            this.parent = parent;
            this.SolutionExplorer = new UIHierarchyMock(parent);
        }

        #region Test helpers
        public UIHierarchyMock SolutionExplorer
        {
            get;
            set;
        }
        #endregion

        #region ToolWindows
        CommandWindow ToolWindows.CommandWindow
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        DTE ToolWindows.DTE
        {
            get
            {
                return this.parent;
            }
        }

        ErrorList ToolWindows.ErrorList
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        OutputWindow ToolWindows.OutputWindow
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        UIHierarchy ToolWindows.SolutionExplorer
        {
            get
            {
                return this.SolutionExplorer;
            }
        }

        TaskList ToolWindows.TaskList
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ToolBox ToolWindows.ToolBox
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object ToolWindows.GetToolWindow(string Name)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
