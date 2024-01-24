/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Runtime.InteropServices;
using System.Windows.Documents;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Controls;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.Education
{
    public interface IRuleHelpToolWindow
    {
        void UpdateContent(FlowDocument newContent);
    }

    [Guid(ToolWindowIdAsString)]
    internal class RuleHelpToolWindow : ToolWindowPane, IRuleHelpToolWindow
    {
        private const string ToolWindowIdAsString = "9E74B368-9FC3-47B0-A1C7-2DBA3A2C1762";
        public static readonly Guid ToolWindowId = new Guid(ToolWindowIdAsString);
        private readonly RuleHelpUserControl control;

        public RuleHelpToolWindow(IBrowserService browserService, IEducation education, ILogger logger)
        {
            Caption = Resources.RuleHelpToolWindowCaption;
            control = new RuleHelpUserControl(browserService, education, logger);
            Content = control;
        }

        public void UpdateContent(FlowDocument newContent)
        {
            control.docViewer.Document = newContent;
        }
    }
}
