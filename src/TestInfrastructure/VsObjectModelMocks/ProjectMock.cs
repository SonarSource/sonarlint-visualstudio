//-----------------------------------------------------------------------
// <copyright file="ProjectMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ProjectMock : VsUIHierarchyMock, IVsProject, Project, IVsBuildPropertyStorage, IVsAggregatableProjectCorrected
    {
        private readonly Dictionary<string, uint> files = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        private readonly PropertiesMock properties;
        private readonly ConfigurationManagerMock configurationManager = new ConfigurationManagerMock();
        private readonly IDictionary<string, string> buildProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string aggregateProjectTypeGuids = string.Empty;

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

        #region IVsBuildPropertyStorage

        int IVsBuildPropertyStorage.GetPropertyValue(string pszPropName, string pszConfigName, uint storage, out string pbstrPropValue)
        {
            pbstrPropValue = null;
            if (this.buildProperties.ContainsKey(pszPropName))
            {
                pbstrPropValue = this.buildProperties[pszPropName];
            }

            return VSConstants.S_OK;
        }

        int IVsBuildPropertyStorage.SetPropertyValue(string pszPropName, string pszConfigName, uint storage, string pszPropValue)
        {
            this.buildProperties[pszPropName] = pszPropValue;
            return VSConstants.S_OK;
        }

        int IVsBuildPropertyStorage.RemoveProperty(string pszPropName, string pszConfigName, uint storage)
        {
            if (this.buildProperties.ContainsKey(pszPropName))
            {
                this.buildProperties.Remove(pszPropName);
            }

            return VSConstants.S_OK;
        }

        int IVsBuildPropertyStorage.GetItemAttribute(uint item, string pszAttributeName, out string pbstrAttributeValue)
        {
            throw new NotImplementedException();
        }

        int IVsBuildPropertyStorage.SetItemAttribute(uint item, string pszAttributeName, string pszAttributeValue)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IVsAggregatableProjectCorrected

        int IVsAggregatableProjectCorrected.SetInnerProject(IntPtr punkInnerIUnknown)
        {
            throw new NotImplementedException();
        }

        int IVsAggregatableProjectCorrected.InitializeForOuter(string pszFilename, string pszLocation, string pszName, uint grfCreateFlags, ref Guid iidProject, out IntPtr ppvProject, out int pfCanceled)
        {
            throw new NotImplementedException();
        }

        int IVsAggregatableProjectCorrected.OnAggregationComplete()
        {
            throw new NotImplementedException();
        }

        int IVsAggregatableProjectCorrected.GetAggregateProjectTypeGuids(out string pbstrProjTypeGuids)
        {
            pbstrProjTypeGuids = this.aggregateProjectTypeGuids;
            return VSConstants.S_OK;
        }

        int IVsAggregatableProjectCorrected.SetAggregateProjectTypeGuids(string lpstrProjTypeGuids)
        {
            this.aggregateProjectTypeGuids = lpstrProjTypeGuids;
            return VSConstants.S_OK;
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

        public string GetBuildProperty(string propertyName)
        {
            string value;
            ((IVsBuildPropertyStorage)this).GetPropertyValue(propertyName, string.Empty, (uint)_PersistStorageType.PST_PROJECT_FILE, out value);
            return value;
        }

        public void SetBuildProperty(string propertyName, string value)
        {
            ((IVsBuildPropertyStorage)this).SetPropertyValue(propertyName, string.Empty, (uint)_PersistStorageType.PST_PROJECT_FILE, value);
        }

        public void ClearBuildProperty(string propertyName)
        {
            ((IVsBuildPropertyStorage)this).RemoveProperty(propertyName, string.Empty, (uint)_PersistStorageType.PST_PROJECT_FILE);
        }

        public void SetAggregateProjectTypeGuids(params Guid[] guids)
        {
            this.aggregateProjectTypeGuids = string.Join(";", guids.Select(x => x.ToString("N"))) ?? string.Empty;
        }

        public void SetTestProject()
        {
            this.SetAggregateProjectTypeGuids(ProjectSystemHelper.TestProjectKindGuid);
        }

        public void ClearProjectKind()
        {
            this.aggregateProjectTypeGuids = string.Empty;
        }
    }
}
