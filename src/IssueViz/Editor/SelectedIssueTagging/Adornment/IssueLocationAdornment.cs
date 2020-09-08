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

using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Formatting;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment
{
    internal class IssueLocationAdornment : Button
    {
        private readonly TextBlock textBlock;
        public IAnalysisIssueLocationVisualization Location { get; }

        public IssueLocationAdornment(ISelectedIssueLocationTag issueTag, IFormattedLineSource formattedLineSource)
        {
            // We can't store the formatted line source since it might change
            // e.g. if the user changes the font size
            Location = issueTag.Location;

            Margin = new System.Windows.Thickness(1, 0, 1, 0); // Space between this UI element and the editor text
            Padding = new System.Windows.Thickness(0);  // Space between the side of the control and its content    

            Background = Brushes.Pink;
            BorderBrush = Brushes.Red;
            BorderThickness = new System.Windows.Thickness(1);

            // Visible content of the adornment
            textBlock = new TextBlock();
            FormatText(formattedLineSource);
            Content = textBlock;
        }

        private void FormatText(IFormattedLineSource formattedLineSource)
        {
            // Don't need to set the height of this control explicitly -
            // it will resize to accommodate the child control

            textBlock.Text = Location.StepNumber.ToString();
            textBlock.FontSize = formattedLineSource.DefaultTextProperties.FontRenderingEmSize;
            textBlock.FontFamily = formattedLineSource.DefaultTextProperties.Typeface.FontFamily;
            textBlock.Padding = new System.Windows.Thickness(0.2, 0, 0.2, 0);
            textBlock.Margin = new System.Windows.Thickness(0);
        }
    }
}
