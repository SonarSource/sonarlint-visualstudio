/*
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

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging.Inline
{
    internal class InlineErrorAdornment : Border
    {
        public IInlineErrorTag InlineErrorTag { get; }

        public InlineErrorAdornment(IInlineErrorTag inlineErrorTag, IFormattedLineSource formattedLineSource) 
        {
            // We can't store the formatted line source since it might change
            // e.g. if the user changes the font size
            InlineErrorTag = inlineErrorTag;

            //IssueViz = issueViz;

            Margin = new Thickness(3, 0, 3, 0); // Space between this UI element and the editor text
            Padding = new Thickness(0);  // Space between the side of the control and its content    
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(1);

            // Visible content of the adornment
            var issueViz = inlineErrorTag.LocationTagSpans[0].Tag.IssueViz;

            var text = issueViz.RuleId + ": " + issueViz.Issue.PrimaryLocation.Message;
            if (inlineErrorTag.LocationTagSpans.Length > 1)
            {
                text = $"[{inlineErrorTag.LocationTagSpans.Length} issues] " + text;
            }

            var firstFix = issueViz.QuickFixes?.FirstOrDefault(fix => fix.CanBeApplied(formattedLineSource.SourceTextSnapshot));

            Child = firstFix == null
                ? new TextBlock
                {
                    Text = text,
                    FontWeight = FontWeights.SemiBold,
                    Padding = new Thickness(4, 0, 4, 0)
                }
                : (UIElement) new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        GetQuickFixButton(),
                        new TextBlock
                        {
                            Text = text,
                            FontWeight = FontWeights.SemiBold,
                            Padding = new Thickness(4, 0, 4, 0)
                        }
                    }
                };

            ToolTip = new TextBlock
            {
                Text = issueViz.Issue.PrimaryLocation.Message
            };

            Update(formattedLineSource);

            UIElement GetQuickFixButton()
            {
                var icon = new CrispImage { Moniker = KnownMonikers.Checkmark };
                icon.MouseLeftButtonDown += (sender, e) =>
                {
                    var textEdit = formattedLineSource.SourceTextSnapshot.TextBuffer.CreateEdit();

                    foreach (var edit in firstFix.EditVisualizations)
                    {
                        var updatedSpan = new SpanTranslator().TranslateTo(edit.Span, formattedLineSource.SourceTextSnapshot, SpanTrackingMode.EdgeExclusive);

                        textEdit.Replace(updatedSpan, edit.Edit.NewText);
                    }

                    issueViz.InvalidateSpan();
                    textEdit.Apply();
                };

                return icon;
            }
        }

        /// <summary>
        /// Updates the adornment's color and font to match the VS theme.
        /// </summary>
        /// <remarks>
        /// <see cref="formattedLineSource"/> can be null if the adornment is created when the view is initialized.
        /// If it is null, we will not apply VS theme colors and instead will rely on VS to call us again
        /// when the view is initialized and <see cref="formattedLineSource"/> is no longer null.
        /// </remarks>
        public void Update(IFormattedLineSource formattedLineSource)
        {
            if (formattedLineSource == null)
            {
                return;
            }

            var textBlock = Child as TextBlock ?? (Child as StackPanel).Children[1] as TextBlock;

            textBlock.Foreground = formattedLineSource.DefaultTextProperties.ForegroundBrush;
            textBlock.FontSize = formattedLineSource.DefaultTextProperties.FontRenderingEmSize - 2;
            textBlock.FontFamily = formattedLineSource.DefaultTextProperties.Typeface.FontFamily;

            var themeColors = ThemeColors.BasedOnText(textBlock.Foreground);
            Background = themeColors.BackgroundBrush;
            BorderBrush = themeColors.BorderBrush;
        }
    }
}
