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

extern alias versionSpecificShellFramework;

using System;
using SonarLint.VisualStudio.Core;
using TableDataSource = versionSpecificShellFramework::Microsoft.VisualStudio.Shell.TableManager.ITableDataSource;
using TableDataSink = versionSpecificShellFramework::Microsoft.VisualStudio.Shell.TableManager.ITableDataSink;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsControl.VsTableControl
{
    internal class HotspotsTableDataSource : TableDataSource, IDisposable
    {
        public string SourceTypeIdentifier { get; } = HotspotsTableConstants.TableSourceTypeIdentifier;
        public string Identifier { get; } = HotspotsTableConstants.TableIdentifier;
        public string DisplayName { get; } = HotspotsTableConstants.TableDisplayName;

        public IDisposable Subscribe(TableDataSink sink)
        {
            return new ExecuteOnDispose(() => { });
        }

        public void Dispose()
        {
        }
    }
}
