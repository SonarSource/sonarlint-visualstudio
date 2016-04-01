//-----------------------------------------------------------------------
// <copyright file="NotificationIds.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    internal static class NotificationIds
    {
        public static readonly Guid FailedToConnectId = new Guid("39A75D18-6CF4-44CE-996C-CD692977668F");
        public static readonly Guid FailedToBindId = new Guid("5A38773F-89F6-49ED-8B67-1A82A8182589");
        public static readonly Guid FailedToFindBoundProjectKeyId = new Guid("4A92944A-2585-442D-8821-DE235DA9E478");
        public static readonly Guid WarnServerTrustId = new Guid("{F9A383D5-47ED-439E-A1DB-7A1083062CCD}");
        public static readonly Guid BadServerPluginId = new Guid("{F89A8FAB-6EF1-4EB5-A1F7-A197AEF9DC8C}");
        public static readonly Guid RuleSetConflictsId = new Guid("{D9DBFF58-B6D1-43ED-BDFB-083D4A5ECFF5}");
    }
}
