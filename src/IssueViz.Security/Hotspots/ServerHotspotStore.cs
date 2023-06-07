/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots
{
    internal interface IServerHotspotStore
    {
        void Refresh(IList<SonarQubeHotspot> serverHotspots);

        IList<SonarQubeHotspot> GetAl();

        event EventHandler ServerHotspotStoreRefreshed;
    }

    //[Export(typeof(IServerHotspotStore))]
    //[PartCreationPolicy(CreationPolicy.Shared)]
    //internal class ServerHotspotStore : IServerHotspotStore
    //{
    //    private IList<SonarQubeHotspot> currentHotspots = new List<SonarQubeHotspot>();

    //    public event EventHandler ServerHotspotStoreRefreshed;

    //    public IList<SonarQubeHotspot> GetAl()
    //    {
    //        return currentHotspots;
    //    }

    //    public void Refresh(IList<SonarQubeHotspot> serverHotspots)
    //    {
    //        currentHotspots = serverHotspots;
    //        InvokeRefreshed();
    //    }

    //    private void InvokeRefreshed()
    //    {
    //        ServerHotspotStoreRefreshed.Invoke(this, null);
    //    }
    //}
}
