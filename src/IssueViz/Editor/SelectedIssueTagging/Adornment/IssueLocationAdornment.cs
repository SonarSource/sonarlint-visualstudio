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

using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text.Formatting;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment
{
    internal class IssueLocationAdornment : Border
    {
        public IAnalysisIssueLocationVisualization LocationViz { get; }

        public IssueLocationAdornment(IAnalysisIssueLocationVisualization locationViz, IFormattedLineSource formattedLineSource) 
        {
            // We can't store the formatted line source since it might change
            // e.g. if the user changes the font size
            LocationViz = locationViz;

            Margin = new Thickness(3, 0, 3, 0); // Space between this UI element and the editor text
            Padding = new Thickness(0);  // Space between the side of the control and its content    
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(1);

            // Visible content of the adornment
            Child = new TextBlock
            {
                Text = locationViz.Label,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(4, 0, 4, 0)
            };

            ToolTip = new TextBlock
            {
                Text = LocationViz.Location.Message
            };

            Update(formattedLineSource);
        }

        public void Update(IFormattedLineSource formattedLineSource)
        {
            var textBlock = Child as TextBlock;

            textBlock.Foreground = formattedLineSource.DefaultTextProperties.ForegroundBrush;
            textBlock.FontSize = formattedLineSource.DefaultTextProperties.FontRenderingEmSize - 2;
            textBlock.FontFamily = formattedLineSource.DefaultTextProperties.Typeface.FontFamily;

            var themeColors = ThemeColors.BasedOnText(textBlock.Foreground);
            Background = themeColors.BackgroundBrush;
            BorderBrush = themeColors.BorderBrush;
        }
    }
}
