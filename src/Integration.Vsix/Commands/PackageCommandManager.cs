﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class PackageCommandManager
    {
        private readonly IMenuCommandService menuService;
        public PackageCommandManager(IMenuCommandService menuService)
        {
            if (menuService == null)
            {
                throw new ArgumentNullException(nameof(menuService));
            }

            this.menuService = menuService;
        }

        public void Initialize(
            ITeamExplorerController teamExplorerController, IProjectPropertyManager projectPropertyManager,
            IProjectToLanguageMapper projectToLanguageMapper, IActiveDocumentLocator activeDocumentLocator,
            ISonarLanguageRecognizer sonarLanguageRecognizer, IAnalysisRequester analysisRequester)
        {
            // Buttons
            this.RegisterCommand((int)PackageCommandId.ManageConnections, new ManageConnectionsCommand(teamExplorerController));
            this.RegisterCommand((int)PackageCommandId.ProjectExcludePropertyToggle, new ProjectExcludePropertyToggleCommand(projectPropertyManager, projectToLanguageMapper));
            this.RegisterCommand((int)PackageCommandId.ProjectTestPropertyAuto, new ProjectTestPropertySetCommand(projectPropertyManager, projectToLanguageMapper, null));
            this.RegisterCommand((int)PackageCommandId.ProjectTestPropertyTrue, new ProjectTestPropertySetCommand(projectPropertyManager, projectToLanguageMapper, true));
            this.RegisterCommand((int)PackageCommandId.ProjectTestPropertyFalse, new ProjectTestPropertySetCommand(projectPropertyManager, projectToLanguageMapper, false));
            this.RegisterCommand((int)PackageCommandId.AnalyzeCPPFile, new CFamilyTriggerAnalysisCommand(activeDocumentLocator, sonarLanguageRecognizer, analysisRequester));
            // Menus
            this.RegisterCommand((int)PackageCommandId.ProjectSonarLintMenu, new ProjectSonarLintMenuCommand(projectPropertyManager, projectToLanguageMapper));
        }

        internal /* testing purposes */ OleMenuCommand RegisterCommand(int commandId, VsCommandBase command)
        {
            return this.AddCommand(new Guid(CommonGuids.CommandSet), commandId, command.Invoke, command.QueryStatus);
        }

        private OleMenuCommand AddCommand(Guid commandGroupGuid, int commandId, EventHandler invokeHandler, EventHandler beforeQueryStatus)
        {
            CommandID idObject = new CommandID(commandGroupGuid, commandId);
            OleMenuCommand command = new OleMenuCommand(invokeHandler, delegate
            { }, beforeQueryStatus, idObject);
            this.menuService.AddCommand(command);
            return command;
        }
    }
}
