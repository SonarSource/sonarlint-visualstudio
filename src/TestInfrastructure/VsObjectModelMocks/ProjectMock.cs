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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    // Note: this mock is for new-style project system i.e. https://github.com/dotnet/project-system
    // See the separate LegacyProjectMock class for the old-style mock.
    public class ProjectMock : VsUIHierarchyMock, IVsProject, Project, IVsBuildPropertyStorage
    {
        private readonly Dictionary<string, uint> files = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, string> buildProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public DTE DTE { get; set; }

        public ProjectMock(string projectFile)
            : base(projectFile, (uint)VSConstants.VSITEMID.Root)
        {
            ((Project)this).Name = Path.GetFileName(projectFile);
            this.Properties = new PropertiesMock(this);
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

        public PropertiesMock Properties { get; }

        public ConfigurationManagerMock ConfigurationManager
        {
            get;
            // Setter for testing
            set;
        } = new ConfigurationManagerMock();

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
                return this.Properties;
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
                return this.ConfigurationManager;
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

        #endregion Project

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

        #endregion IVsProject

        #region IVsBuildPropertyStorage

        int IVsBuildPropertyStorage.GetPropertyValue(string pszPropName, string pszConfigName, uint storage, out string pbstrPropValue)
        {
            string propertyKey = GetInternalBuildPropertyKey(pszPropName, pszConfigName);

            pbstrPropValue = null;
            if (this.buildProperties.ContainsKey(propertyKey))
            {
                pbstrPropValue = this.buildProperties[propertyKey];
                return VSConstants.S_OK;
            }

            return ProjectSystemHelper.E_XML_ATTRIBUTE_NOT_FOUND;
        }

        int IVsBuildPropertyStorage.SetPropertyValue(string pszPropName, string pszConfigName, uint storage, string pszPropValue)
        {
            string propertyKey = GetInternalBuildPropertyKey(pszPropName, pszConfigName);

            this.buildProperties[propertyKey] = pszPropValue;
            return VSConstants.S_OK;
        }

        int IVsBuildPropertyStorage.RemoveProperty(string pszPropName, string pszConfigName, uint storage)
        {
            string propertyKey = GetInternalBuildPropertyKey(pszPropName, pszConfigName);

            if (this.buildProperties.ContainsKey(propertyKey))
            {
                this.buildProperties.Remove(propertyKey);
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

        private static string GetInternalBuildPropertyKey(string propertyName, string configurationName)
        {
            return $"{propertyName}_{configurationName}";
        }

        #endregion IVsBuildPropertyStorage

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

        public void SetProjectKind(Guid kind)
        {
            this.ProjectKind = kind.ToString("N");
        }

        public void SetCSProjectKind()
        {
            this.ProjectKind = ProjectSystemHelper.CSharpProjectKind;
        }

        public void SetVBProjectKind()
        {
            this.ProjectKind = ProjectSystemHelper.VbProjectKind;
        }

        public string GetBuildProperty(string propertyName, string configuration = "")
        {
            string value;
            ((IVsBuildPropertyStorage)this).GetPropertyValue(propertyName, configuration, (uint)_PersistStorageType.PST_PROJECT_FILE, out value);
            return value;
        }

        public void SetBuildProperty(string propertyName, string value, string configuration = "")
        {
            ((IVsBuildPropertyStorage)this).SetPropertyValue(propertyName, configuration, (uint)_PersistStorageType.PST_PROJECT_FILE, value);
        }

        public void ClearBuildProperty(string propertyName, string configuration = "")
        {
            ((IVsBuildPropertyStorage)this).RemoveProperty(propertyName, configuration, (uint)_PersistStorageType.PST_PROJECT_FILE);
        }
    }
}
