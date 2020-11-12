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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource
{
    internal interface IHotspotTableEntryFactory
    {
        ITableEntry Create(IAnalysisIssueVisualization issueVisualization);
    }

    [Export(typeof(IHotspotTableEntryFactory))]
    internal class HotspotTableEntryFactory : IHotspotTableEntryFactory
    {
        private readonly IVsUIShell2 vsUiShell;

        [ImportingConstructor]
        public HotspotTableEntryFactory([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            vsUiShell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell2;
        }

        public ITableEntry Create(IAnalysisIssueVisualization issueVisualization)
        {
            if (issueVisualization == null)
            {
                throw new ArgumentNullException(nameof(issueVisualization));
            }

            if (!(issueVisualization.Issue is IHotspot))
            {
                throw new InvalidCastException($"{nameof(issueVisualization.Issue)} is not {nameof(IHotspot)}");
            }

            return new HotspotTableEntry(issueVisualization, new HotspotTableEntryWpfElementFactory(vsUiShell, issueVisualization));
        }
    }
}
