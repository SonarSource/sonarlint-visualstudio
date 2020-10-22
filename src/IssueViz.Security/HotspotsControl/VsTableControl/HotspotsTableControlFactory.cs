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

using System.ComponentModel.Composition;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsControl.VsTableControl
{
    internal interface IHotspotsTableControlFactory
    {
        IHotspotsTableControl Get();
    }

    [Export(typeof(IHotspotsTableControlFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class HotspotsTableControlFactory : IHotspotsTableControlFactory
    {
        private readonly ITableManagerProvider tableManagerProvider;
        private readonly IWpfTableControlProvider wpfTableControlProvider;

        [ImportingConstructor]
        public HotspotsTableControlFactory(ITableManagerProvider tableManagerProvider, IWpfTableControlProvider wpfTableControlProvider)
        {
            this.tableManagerProvider = tableManagerProvider;
            this.wpfTableControlProvider = wpfTableControlProvider;
        }

        public IHotspotsTableControl Get()
        {
            var hotspotsTableControl = new HotspotsTableControl(tableManagerProvider, wpfTableControlProvider);
            return hotspotsTableControl;
        }
    }
}
