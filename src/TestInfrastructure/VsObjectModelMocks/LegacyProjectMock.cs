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
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Flavor;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Mock for managed projects that use the legacy C#/VB project system
    /// </summary>
    public class LegacyProjectMock : ProjectMock,
        IVsAggregatableProjectCorrected // we use this interface as a marker to differentiate between legacy and new projects (because
            // the interface is not implemented by the new project system).
    {
        private string aggregateProjectTypeGuids = string.Empty;

        public LegacyProjectMock(string projectFile)
            :base(projectFile)
        {
        }

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

        #endregion IVsAggregatableProjectCorrected

        public void SetAggregateProjectTypeString(string str)
        {
            this.aggregateProjectTypeGuids = str;
        }

        public void SetAggregateProjectTypeGuids(params Guid[] guids)
        {
            this.aggregateProjectTypeGuids = string.Join(";", guids.Select(x => x.ToString("N"))) ?? string.Empty;
        }

        public IEnumerable<Guid> GetAggregateProjectTypeGuids()
        {
            return this.aggregateProjectTypeGuids.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(Guid.Parse);
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
