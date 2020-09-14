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

using System.Linq;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix.ErrorList
{
    internal interface IIssuesSnapshotFactory : ITableEntriesSnapshotFactory
    {
        IIssuesSnapshot CurrentSnapshot { get; }

        /// <summary>
        /// Returns true/false if the factory was updated due to the rename
        /// </summary>
        bool HandleFileRename(string oldFilePath, string newFilePath);
    }

    /// <summary>
    /// Plumbing for passing data items to the ErrorList
    /// </summary>
    /// <remarks>
    /// See the README.md in this folder for more information
    /// </remarks>
    internal class IssuesSnapshotFactory : TableEntriesSnapshotFactoryBase, IIssuesSnapshotFactory
    {
        public IIssuesSnapshot CurrentSnapshot { get; private set; }

        public IssuesSnapshotFactory(IIssuesSnapshot snapshot)
        {
            this.CurrentSnapshot = snapshot;
        }

        public void UpdateSnapshot(IIssuesSnapshot snapshot)
        {
            this.CurrentSnapshot = snapshot;
        }

        #region ITableEntriesSnapshotFactory members

        public override int CurrentVersionNumber => CurrentSnapshot.VersionNumber;

        public override ITableEntriesSnapshot GetCurrentSnapshot() => CurrentSnapshot;

        public override ITableEntriesSnapshot GetSnapshot(int versionNumber)
        {
            // In theory the snapshot could change in the middle of the return statement so snap the snapshot just to be safe.
            var snapshot = this.CurrentSnapshot;
            return (versionNumber == snapshot.VersionNumber) ? snapshot : null;
        }

        #endregion

        public bool HandleFileRename(string oldFilePath, string newFilePath)
        {
            var locationsInOldFile = CurrentSnapshot.GetLocationsVizsForFile(oldFilePath);

            foreach (var location in locationsInOldFile)
            {
                location.CurrentFilePath = newFilePath;
            }

            var factoryChanged = true;
            var renamedAnalyzedFile = PathHelper.IsMatchingPath(oldFilePath, CurrentSnapshot.AnalyzedFilePath);

            if (renamedAnalyzedFile)
            {
                UpdateSnapshot(CurrentSnapshot.CreateUpdatedSnapshot(newFilePath));
            }
            else if (locationsInOldFile.Any())
            {
                CurrentSnapshot.IncrementVersion();
            }
            else
            {
                factoryChanged = false;
            }

            return factoryChanged;
        }
    }
}
