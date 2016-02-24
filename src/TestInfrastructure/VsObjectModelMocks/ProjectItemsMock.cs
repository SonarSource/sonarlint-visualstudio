//-----------------------------------------------------------------------
// <copyright file="ProjectItemsMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ProjectItemsMock : ProjectItems
    {
        private List<ProjectItem> items = new List<ProjectItem>();

        public ProjectItemsMock(ProjectMock parent, params ProjectItem[] items)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            this.Project = parent;
            this.items.AddRange(items);
        }

        public ProjectMock Project
        {
            get;
        }

        #region ProjectItems
        Project ProjectItems.ContainingProject
        {
            get
            {
                return this.Project;
            }
        }

        int ProjectItems.Count
        {
            get
            {
                return this.items.Count;
            }
        }

        DTE ProjectItems.DTE
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string ProjectItems.Kind
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object ProjectItems.Parent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ProjectItem ProjectItems.AddFolder(string Name, string Kind)
        {
            throw new NotImplementedException();
        }

        ProjectItem ProjectItems.AddFromDirectory(string Directory)
        {
            throw new NotImplementedException();
        }

        ProjectItem ProjectItems.AddFromFile(string FileName)
        {
            Assert.IsFalse(this.items.Any(pi => StringComparer.OrdinalIgnoreCase.Equals(pi.Name, FileName)), "File already exists in project items");

            this.Project.AddOrGetFile(FileName);
            FileProjectItemMock fileItem = new FileProjectItemMock(this, FileName);
            this.items.Add(fileItem);
            return fileItem;
        }

        ProjectItem ProjectItems.AddFromFileCopy(string FilePath)
        {
            throw new NotImplementedException();
        }

        ProjectItem ProjectItems.AddFromTemplate(string FileName, string Name)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        IEnumerator ProjectItems.GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        ProjectItem ProjectItems.Item(object index)
        {
            return this.items[(int)index + 1];
        }
        #endregion ProjectItems

        #region FileProjectItemMock
        // Simplification over the exact structure
        internal class FileProjectItemMock : ProjectItem
        {
            public FileProjectItemMock(ProjectItemsMock parent, string file)
            {
                this.Parent = parent;
                this.Name = file;
                this.Properties = new PropertiesMock(this);
            }

            public ProjectItemsMock Parent
            {
                get;
            }

            #region ProjectItem
            public ProjectItems Collection
            {
                get
                {
                    return this.Parent;
                }
            }

            public ConfigurationManager ConfigurationManager
            {
                get;
            } = new ConfigurationManagerMock();

            public Project ContainingProject
            {
                get
                {
                    return this.Parent.Project;
                }
            }

            public Document Document
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public DTE DTE
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string ExtenderCATID
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public object ExtenderNames
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public FileCodeModel FileCodeModel
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public short FileCount
            {
                get
                {
                    return 1;
                }
            }

            public bool IsDirty
            {
                get;
                set;
            }

            public string Kind
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }

            public object Object
            {
                get;
                set;
            }

            public ProjectItems ProjectItems
            {
                get
                {
                    return null;
                }
            }

            public Properties Properties
            {
                get;
            }

            public bool Saved
            {
                get;
                set;
            }

            public Project SubProject
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public void Delete()
            {
                throw new NotImplementedException();
            }

            public void ExpandView()
            {
                throw new NotImplementedException();
            }

            public object get_Extender(string ExtenderName)
            {
                throw new NotImplementedException();
            }

            public string get_FileNames(short index)
            {
                throw new NotImplementedException();
            }

            public bool get_IsOpen(string ViewKind = "{FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF}")
            {
                throw new NotImplementedException();
            }

            public Window Open(string ViewKind = "{00000000-0000-0000-0000-000000000000}")
            {
                throw new NotImplementedException();
            }

            public void Remove()
            {
                throw new NotImplementedException();
            }

            public void Save(string FileName = "")
            {
                throw new NotImplementedException();
            }

            public bool SaveAs(string NewFileName)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
        #endregion
    }
}
