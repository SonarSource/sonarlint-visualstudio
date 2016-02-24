//-----------------------------------------------------------------------
// <copyright file="ProjectMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ProjectMock : VsUIHierarchyMock, IVsProject, Project
    {
        private readonly Dictionary<string, uint> files = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        private readonly PropertiesMock properties;
        private readonly ConfigurationManagerMock configurationManager = new ConfigurationManagerMock();

        public DTE DTE { get; set; }

        public ProjectMock(string projectFile)
            : base(projectFile, (uint)VSConstants.VSITEMID.Root)
        {
            ((Project)this).Name = Path.GetFileName(projectFile);
            this.properties = new PropertiesMock(this);
        }

        public bool IsLoaded
        {
            get; set;
        }

        public IReadOnlyDictionary<string, uint> Files
        {
            get
            {
                return this.files;
            }
        }

        public string ProjectKind
        {
            get;
            set;
        }

        public PropertiesMock Properties
        {
            get { return this.properties; }
        }

        public ConfigurationManagerMock ConfigurationManager
        {
            get { return this.configurationManager; }
        }

        #region Project
        string Project.Name
        {
            get;
            set;
        }

        string Project.FileName
        {
            get
            {
                return this.FilePath;
            }
        }

        bool Project.IsDirty
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

        Projects Project.Collection
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        DTE Project.DTE
        {
            get
            {
                return this.DTE;
            }
        }

        string Project.Kind
        {
            get
            {
                return this.ProjectKind;
            }
        }

        ProjectItems Project.ProjectItems
        {
            get
            {
                ProjectItems projectItems = new ProjectItemsMock(this);
                this.Files.Keys.ToList().ForEach(f => projectItems.AddFromFile(f));
                return projectItems;
            }
        }

        Properties Project.Properties
        {
            get
            {
                return this.properties;
            }
        }

        string Project.UniqueName
        {
            get
            {
                return this.FilePath;
            }
        }

        dynamic Project.Object
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        dynamic Project.ExtenderNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string Project.ExtenderCATID
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string Project.FullName
        {
            get
            {
                return this.FilePath;
            }
        }

        bool Project.Saved
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

        ConfigurationManager Project.ConfigurationManager
        {
            get
            {
                return this.configurationManager;
            }
        }

        Globals Project.Globals
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ProjectItem Project.ParentProjectItem
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        CodeModel Project.CodeModel
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        void Project.SaveAs(string NewFileName)
        {
            throw new NotImplementedException();
        }

        void Project.Save(string FileName)
        {
            throw new NotImplementedException();
        }

        void Project.Delete()
        {
            throw new NotImplementedException();
        }

        dynamic Project.get_Extender(string ExtenderName)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IVsProject
        int IVsProject.IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid)
        {
            pfFound = 0;
            pitemid = 0;

            if (this.Files.TryGetValue(pszMkDocument, out pitemid))
            {
                pfFound = 1;
            }
            return VSConstants.S_OK;
        }

        int IVsProject.GetMkDocument(uint itemid, out string pbstrMkDocument)
        {
            pbstrMkDocument = null;
            if (this.ItemId == itemid)
            {
                // Project has the matching item id
                pbstrMkDocument = this.FilePath;
            }
            else
            {
                // Check the project files
                pbstrMkDocument = this.Files.Where(kv => kv.Value == itemid).Select(kv => kv.Key).SingleOrDefault();
            }

            return pbstrMkDocument == null ? VSConstants.E_FAIL : VSConstants.S_OK;
        }

        int IVsProject.OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
        {
            throw new NotImplementedException();
        }

        int IVsProject.GetItemContext(uint itemid, out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
        {
            throw new NotImplementedException();
        }

        int IVsProject.GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName)
        {
            throw new NotImplementedException();
        }

        int IVsProject.AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult)
        {
            throw new NotImplementedException();
        }
        #endregion

        public uint AddOrGetFile(string filePath)
        {
            uint fileId;
            if (!this.Files.TryGetValue(filePath, out fileId))
            {
                fileId = AllocateItemId();
                this.files[filePath] = fileId;
            }
            return fileId;
        }

        public void RemoveFile(string filePath)
        {
            this.files.Remove(filePath);
        }

        public void SetCSProjectKind()
        {
            this.ProjectKind = ProjectSystemHelper.CSharpProjectKind;
        }

        public void SetVBProjectKind()
        {
            this.ProjectKind = ProjectSystemHelper.VbProjectKind;
        }
    }
}
