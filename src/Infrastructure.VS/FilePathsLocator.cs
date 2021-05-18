/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    public interface IFilePathsLocator
    {
        /// <summary>
        /// Returns the absolute file paths of <see cref="fileName"/> in the given <see cref="IVsHierarchy"/>
        /// </summary>
        IReadOnlyList<string> Locate(IVsHierarchy hierarchyToSearch, string fileName);
    }

    [Export(typeof(IFilePathsLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class FilePathsLocator : IFilePathsLocator
    {
        public IReadOnlyList<string> Locate(IVsHierarchy hierarchyToSearch, string fileName)
        {
            if (hierarchyToSearch == null)
            {
                throw new ArgumentNullException(nameof(hierarchyToSearch));
            }

            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var files = new List<string>();
            var itemIdsInProject = GetAllItems(hierarchyToSearch);

            foreach (var vsItemId in itemIdsInProject)
            {
                var absoluteItemFilePath = GetItemFilePath(hierarchyToSearch, vsItemId);

                if (string.IsNullOrEmpty(absoluteItemFilePath))
                {
                    continue;
                }

                if (Path.GetFileName(absoluteItemFilePath).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(absoluteItemFilePath);
                }
            }

            return files;
        }

        private IEnumerable<VSConstants.VSITEMID> GetAllItems(IVsHierarchy vsHierarchy)
        {
            // Based on https://github.com/microsoft/VSSDK-Extensibility-Samples/blob/c6b80a4ff7023578649d7edecc8fd6cd8a34da10/Code_Sweep/C%23/VsPackage/BuildManager.cs#L450
            return ChildrenOf(vsHierarchy, (uint)VSConstants.VSITEMID.Root);
        }

        private static IEnumerable<VSConstants.VSITEMID> ChildrenOf(IVsHierarchy hierarchy, uint rootId)
        {
            var result = new List<VSConstants.VSITEMID>();

            for (var itemId = FirstChild(hierarchy, rootId);
                itemId != (uint)VSConstants.VSITEMID.Nil;
                itemId = NextSibling(hierarchy, itemId))
            {
                result.Add((VSConstants.VSITEMID)itemId);
                result.AddRange(ChildrenOf(hierarchy, itemId));
            }

            return result;
        }

        private static uint FirstChild(IVsHierarchy hierarchy, uint rootId) =>
            GetItemId(hierarchy, rootId, __VSHPROPID.VSHPROPID_FirstChild);

        private static uint NextSibling(IVsHierarchy hierarchy, uint firstId) =>
            GetItemId(hierarchy, firstId, __VSHPROPID.VSHPROPID_NextSibling);

        private static uint GetItemId(IVsHierarchy hierarchy, uint itemId, __VSHPROPID propId)
        {
            hierarchy.GetProperty(itemId, (int)propId, out var idObj);

            return idObj == null ? (uint)VSConstants.VSITEMID.Nil : (uint)(int)idObj;
        }

        private string GetItemFilePath(IVsHierarchy hierarchy, VSConstants.VSITEMID itemId)
        {
            int hr = hierarchy.GetCanonicalName((uint)itemId, out var filePath);
            Debug.Assert(hr == VSConstants.S_OK, "GetCanonicalName failed");

            return filePath;
        }
    }
}
