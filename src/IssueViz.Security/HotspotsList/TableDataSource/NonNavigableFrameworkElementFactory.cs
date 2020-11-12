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

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell.Interop;
using Color = System.Windows.Media.Color;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource
{
    internal interface INonNavigableFrameworkElementFactory
    {
        FrameworkElement Create(string content);
    }

    internal class NonNavigableFrameworkElementFactory : INonNavigableFrameworkElementFactory
    {
        private readonly IVsUIShell2 vsUiShell;

        public NonNavigableFrameworkElementFactory(IServiceProvider serviceProvider)
        {
            vsUiShell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell2;
        }

        public FrameworkElement Create(string content)
        {
            var control = new TextBlock
            {
                Text = content,
                FontStyle = FontStyles.Italic,
                ToolTip = HotspotsListResources.ERR_CannotNavigateTooltip,
                Style = new Style(typeof(TextBlock))
                {
                    Triggers =
                    {
                        new DataTrigger
                        {
                            Binding = new Binding("IsSelected"),
                            Value = true,
                            Setters =
                            {
                                new Setter
                                {
                                    Property = Control.ForegroundProperty,
                                    Value = new SolidColorBrush(GetInactiveSelectedTextColor())
                                }
                            }
                        },
                        new DataTrigger
                        {
                            Binding = new Binding("IsSelected"),
                            Value = false,
                            Setters =
                            {
                                new Setter
                                {
                                    Property = Control.ForegroundProperty,
                                    Value = new SolidColorBrush(GetInactiveTextColor())
                                }
                            }
                        }
                    }
                }
            };

            return control;
        }

        private Color GetInactiveSelectedTextColor() => GetColor(__VSSYSCOLOREX.VSCOLOR_COMMANDBAR_TEXT_SELECTED);

        private Color GetInactiveTextColor() => GetColor(__VSSYSCOLOREX.VSCOLOR_COMMANDBAR_TEXT_INACTIVE);

        private Color GetColor(__VSSYSCOLOREX vsColor)
        {
            vsUiShell.GetVSSysColorEx((int)vsColor, out var rgbValue);

            var color = ColorTranslator.FromWin32((int)rgbValue);
            var wpfColor = Color.FromRgb(color.R, color.G, color.B);

            return wpfColor;
        }
    }
}
