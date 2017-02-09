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
using System.Collections;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// EnvDTE implementation of <see cref="Solution"/> and <see cref="Solution2"/>
    /// </summary>
    public partial class SolutionMock : Solution, Solution2
    {
        public SolutionMock(DTEMock dte, string solutionFile)
            : base(solutionFile)
        {
            this.dte = dte;
            if (this.dte != null)
            {
                this.dte.Solution = this;
            }
        }

        DTE Solution2.DTE
        {
            get
            {
                return this.dte;
            }
        }

        DTE Solution2.Parent
        {
            get
            {
                return this.dte;
            }
        }

        int Solution2.Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string Solution2.FileName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Properties Solution2.Properties
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool Solution2.IsDirty
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

        string Solution2.FullName
        {
            get
            {
                return this.FilePath;
            }
        }

        bool Solution2.Saved
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

        Globals Solution2.Globals
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        AddIns Solution2.AddIns
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        dynamic Solution2.ExtenderNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string Solution2.ExtenderCATID
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool Solution2.IsOpen
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        SolutionBuild Solution2.SolutionBuild
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Projects Solution2.Projects
        {
            get
            {
                return ((_Solution)this).Projects;
            }
        }

        DTE _Solution.DTE
        {
            get
            {
                return this.dte;
            }
        }

        DTE _Solution.Parent
        {
            get
            {
                return this.dte;
            }
        }

        int _Solution.Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string _Solution.FileName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Properties _Solution.Properties
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool _Solution.IsDirty
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

        string _Solution.FullName
        {
            get
            {
                return this.FilePath;
            }
        }

        bool _Solution.Saved
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

        Globals _Solution.Globals
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        AddIns _Solution.AddIns
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object _Solution.ExtenderNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string _Solution.ExtenderCATID
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool _Solution.IsOpen
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        SolutionBuild _Solution.SolutionBuild
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Projects _Solution.Projects
        {
            get
            {
                return new _ProjectsMock(this);
            }
        }

        Project _Solution.Item(object index)
        {
            throw new NotImplementedException();
        }

        IEnumerator _Solution.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        void _Solution.SaveAs(string FileName)
        {
            throw new NotImplementedException();
        }

        Project _Solution.AddFromTemplate(string FileName, string Destination, string ProjectName, bool Exclusive)
        {
            throw new NotImplementedException();
        }

        Project _Solution.AddFromFile(string FileName, bool Exclusive)
        {
            throw new NotImplementedException();
        }

        void _Solution.Open(string FileName)
        {
            throw new NotImplementedException();
        }

        void _Solution.Close(bool SaveFirst)
        {
            throw new NotImplementedException();
        }

        void _Solution.Remove(Project proj)
        {
            throw new NotImplementedException();
        }

        void _Solution.Create(string Destination, string Name)
        {
            throw new NotImplementedException();
        }

        ProjectItem _Solution.FindProjectItem(string FileName)
        {
            throw new NotImplementedException();
        }

        string _Solution.ProjectItemsTemplatePath(string ProjectKind)
        {
            throw new NotImplementedException();
        }

        string _Solution.get_TemplatePath(string ProjectType)
        {
            throw new NotImplementedException();
        }

        object _Solution.get_Extender(string ExtenderName)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        Project Solution2.Item(object index)
        {
            throw new NotImplementedException();
        }

        IEnumerator Solution2.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        void Solution2.SaveAs(string FileName)
        {
            throw new NotImplementedException();
        }

        Project Solution2.AddFromTemplate(string FileName, string Destination, string ProjectName, bool Exclusive)
        {
            throw new NotImplementedException();
        }

        Project Solution2.AddFromFile(string FileName, bool Exclusive)
        {
            throw new NotImplementedException();
        }

        void Solution2.Open(string FileName)
        {
            throw new NotImplementedException();
        }

        void Solution2.Close(bool SaveFirst)
        {
            throw new NotImplementedException();
        }

        void Solution2.Remove(Project proj)
        {
            var projectMock = proj as ProjectMock;
            if (projectMock == null)
                return;

            this.RemoveProject(projectMock);
        }

        void Solution2.Create(string Destination, string Name)
        {
            throw new NotImplementedException();
        }

        ProjectItem Solution2.FindProjectItem(string FileName)
        {
            throw new NotImplementedException();
        }

        string Solution2.ProjectItemsTemplatePath(string ProjectKind)
        {
            throw new NotImplementedException();
        }

        Project Solution2.AddSolutionFolder(string Name)
        {
            this.projects.ContainsKey(Name).Should().BeFalse("Solution folder already exists");
            var solutionFolder = this.AddOrGetProject(Name);
            solutionFolder.ProjectKind = ProjectSystemHelper.VsProjectItemKindSolutionFolder;

            return solutionFolder;
        }

        string Solution2.GetProjectTemplate(string TemplateName, string Language)
        {
            throw new NotImplementedException();
        }

        string Solution2.GetProjectItemTemplate(string TemplateName, string Language)
        {
            throw new NotImplementedException();
        }

        string Solution2.get_TemplatePath(string ProjectType)
        {
            throw new NotImplementedException();
        }

        dynamic Solution2.get_Extender(string ExtenderName)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #region Projects mock

        private class _ProjectsMock : Projects
        {
            private readonly SolutionMock parent;

            public _ProjectsMock(SolutionMock parent)
            {
                this.parent = parent;
            }

            public int Count
            {
                get
                {
                    return this.parent.Projects.Count();
                }
            }

            public DTE DTE
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string Kind
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public DTE Parent
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public Properties Properties
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IEnumerator GetEnumerator()
            {
                return this.parent.Projects.GetEnumerator();
            }

            public Project Item(object index)
            {
                throw new NotImplementedException();
            }
        }

        #endregion Projects mock
    }
}