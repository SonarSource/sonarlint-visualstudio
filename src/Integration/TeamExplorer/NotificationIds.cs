/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    internal static class NotificationIds
    {
        public static readonly Guid FailedToConnectId = new Guid("39A75D18-6CF4-44CE-996C-CD692977668F");
        public static readonly Guid FailedToBindId = new Guid("5A38773F-89F6-49ED-8B67-1A82A8182589");
        public static readonly Guid FailedToFindBoundProjectKeyId = new Guid("4A92944A-2585-442D-8821-DE235DA9E478");
        public static readonly Guid WarnServerTrustId = new Guid("{F9A383D5-47ED-439E-A1DB-7A1083062CCD}");
        public static readonly Guid BadSonarQubePluginId = new Guid("{F89A8FAB-6EF1-4EB5-A1F7-A197AEF9DC8C}");
        public static readonly Guid RuleSetConflictsId = new Guid("{D9DBFF58-B6D1-43ED-BDFB-083D4A5ECFF5}");
    }
}
