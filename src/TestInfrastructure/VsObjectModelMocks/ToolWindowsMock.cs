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
using EnvDTE80;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ToolWindowsMock : ToolWindows
    {
        private readonly DTEMock parent;

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

        #endregion Test helpers

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

        #endregion ToolWindows
    }
}