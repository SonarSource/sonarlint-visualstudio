/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ConnectionDisplay;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
public sealed partial class ConnectionInfoComponent : UserControl
{
    public static readonly DependencyProperty ConnectionNameFontWeightProp
        = DependencyProperty.Register(nameof(ConnectionNameFontWeight), typeof(FontWeight), typeof(ConnectionInfoComponent), new PropertyMetadata(FontWeights.DemiBold));
    public static readonly DependencyProperty ConnectionInfoProp = DependencyProperty.Register(nameof(ConnectionInfo), typeof(ConnectionInfo), typeof(ConnectionInfoComponent), new PropertyMetadata());
    public static readonly DependencyProperty TextAndIconVerticalAlignmentProp = DependencyProperty.Register(nameof(TextAndIconVerticalAlignment), typeof(VerticalAlignment),
        typeof(ConnectionInfoComponent), new PropertyMetadata(VerticalAlignment.Center));
    public static readonly DependencyProperty ConnectedModeServicesProp
        = DependencyProperty.Register(nameof(ConnectedModeServices), typeof(IConnectedModeUIServices), typeof(ConnectionInfoComponent));

    public ConnectionInfoComponent()
    {
        InitializeComponent();
    }

    public FontWeight ConnectionNameFontWeight
    {
        get => (FontWeight)GetValue(ConnectionNameFontWeightProp);
        set => SetValue(ConnectionNameFontWeightProp, value);
    }

    public ConnectionInfo ConnectionInfo
    {
        get => (ConnectionInfo)GetValue(ConnectionInfoProp);
        set => SetValue(ConnectionInfoProp, value);
    }

    public VerticalAlignment TextAndIconVerticalAlignment
    {
        get => (VerticalAlignment)GetValue(TextAndIconVerticalAlignmentProp);
        set => SetValue(TextAndIconVerticalAlignmentProp, value);
    }

    public IConnectedModeUIServices ConnectedModeServices
    {
        get => (IConnectedModeUIServices)GetValue(ConnectedModeServicesProp);
        set => SetValue(ConnectedModeServicesProp, value);
    }
}
