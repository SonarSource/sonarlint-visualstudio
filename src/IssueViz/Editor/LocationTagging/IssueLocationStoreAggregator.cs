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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging
{
    public interface IIssueLocationStoreAggregator : IIssueLocationStore
    {
    }

    [Export(typeof(IIssueLocationStoreAggregator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class IssueLocationStoreAggregator : IIssueLocationStoreAggregator
    {
        private readonly IEnumerable<IIssueLocationStore> locationStores;

        [ImportingConstructor]
        public IssueLocationStoreAggregator([ImportMany] IEnumerable<IIssueLocationStore> locationStores)
        {
            this.locationStores = locationStores;
        }

        public event EventHandler<IssuesChangedEventArgs> IssuesChanged
        {
            add
            {
                foreach (var issueLocationStore in locationStores)
                {
                    issueLocationStore.IssuesChanged += value;
                }
            }
            remove
            {
                foreach (var issueLocationStore in locationStores)
                {
                    issueLocationStore.IssuesChanged -= value;
                }
            }
        }

        public IEnumerable<IAnalysisIssueLocationVisualization> GetLocations(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return locationStores.SelectMany(x => x.GetLocations(filePath));
        }

        public void Refresh(IEnumerable<string> affectedFilePaths)
        {
            if (affectedFilePaths == null)
            {
                throw new ArgumentNullException(nameof(affectedFilePaths));
            }

            foreach (var issueLocationStore in locationStores)
            {
                issueLocationStore.Refresh(affectedFilePaths);
            }
        }
    }
}
