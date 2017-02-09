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