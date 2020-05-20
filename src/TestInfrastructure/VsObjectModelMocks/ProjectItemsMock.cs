/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ProjectItemsMock : ProjectItems
    {
        private readonly List<FileProjectItemMock> items = new List<FileProjectItemMock>();

        public ProjectItemsMock(ProjectMock parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            this.Project = parent;
            this.items.AddRange(items);
        }

        public FileProjectItemMock this[string filePath]
        {
            get { return items.FirstOrDefault(x => x.Name == filePath); }
        }

        private void RemoveProjectItem(FileProjectItemMock item)
        {
            items.Remove(item);
            Project.RemoveFile(item.Name);
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
            this.items.Any(pi => StringComparer.OrdinalIgnoreCase.Equals(pi.Name, FileName)).Should().BeFalse("File already exists in project items");

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
            return this.items[(int)index - 1]; // VS item indexing starts at 1
        }

        #endregion ProjectItems

        #region FileProjectItemMock

        // Simplification over the exact structure
        public class FileProjectItemMock : ProjectItem
        {
            public PropertiesMock PropertiesMock { get; }

            public FileProjectItemMock(ProjectItemsMock parent, string file)
            {
                this.Parent = parent;
                this.Name = file;
                this.PropertiesMock = new PropertiesMock(this);
                this.PropertiesMock.RegisterKnownProperty(Constants.ItemTypePropertyKey);
                this.PropertiesMock.RegisterKnownProperty(Constants.FullPathPropertyKey);
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
                get { return new ProjectItemsMock(Parent.Project); }
            }

            public Properties Properties
            {
                get { return this.PropertiesMock; }
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
                return Name;
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
                this.Parent.RemoveProjectItem(this);
            }

            public void Save(string FileName = "")
            {
                throw new NotImplementedException();
            }

            public bool SaveAs(string NewFileName)
            {
                throw new NotImplementedException();
            }

            #endregion ProjectItem
        }

        #endregion FileProjectItemMock
    }
}
