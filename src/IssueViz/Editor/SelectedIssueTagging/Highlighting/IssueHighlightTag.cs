﻿/*
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

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Highlight
{
    internal class IssueHighlightTag : TextMarkerTag
    {
        public IssueHighlightTag(Brush textBrush)
            : base(ThemeColors.IsLightTheme(textBrush)
                ? LightIssueHighlightFormatDefinition.FormatName
                : DarkIssueHighlightFormatDefinition.FormatName)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(FormatName)]
    [UserVisible(true)]
    internal class LightIssueHighlightFormatDefinition : BaseIssueHighlightFormatDefinition
    {
        public const string FormatName = "MarkerFormatDefinition/SLVS_Light_IssueHighlightFormatDefinition";

        public LightIssueHighlightFormatDefinition()
            : base(ThemeColors.LightTheme.Highlight)
        {
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(FormatName)]
    [UserVisible(true)]
    internal class DarkIssueHighlightFormatDefinition : BaseIssueHighlightFormatDefinition
    {
        public const string FormatName = "MarkerFormatDefinition/SLVS_Dark_IssueHighlightFormatDefinition";

        public DarkIssueHighlightFormatDefinition()
            : base(ThemeColors.DarkTheme.Highlight)
        {
        }
    }

    internal abstract class BaseIssueHighlightFormatDefinition : MarkerFormatDefinition
    {
        protected BaseIssueHighlightFormatDefinition(Brush fillBrush)
        {
            Fill = fillBrush;
            ForegroundColor = Colors.Transparent;
            DisplayName = "Highlight Issue";
            BackgroundCustomizable = true;
            ForegroundCustomizable = true;
            ZOrder = 5;
        }
    }
}
