/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

namespace SonarLint.VisualStudio.ConnectedMode.UI
{
    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    public partial class SuccessMessage : UserControl
    {
        public static readonly DependencyProperty
            TextProp = DependencyProperty.Register(nameof(Text), typeof(string), typeof(SuccessMessage), new PropertyMetadata(null, OnSuccessPropChanged));

        public static readonly DependencyProperty IsSuccessProp = DependencyProperty.Register(nameof(IsTextSet), typeof(bool), typeof(SuccessMessage));

        public string Text
        {
            get => (string)GetValue(TextProp);
            set => SetValue(TextProp, value);
        }

        public bool IsTextSet
        {
            get => (bool)GetValue(IsSuccessProp);
            set => SetValue(IsSuccessProp, value);
        }

        public SuccessMessage()
        {
            InitializeComponent();
        }

        private static void OnSuccessPropChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is SuccessMessage userControl)
            {
                userControl.IsTextSet = !string.IsNullOrEmpty(userControl.Text);
            }
        }

        private void FadeIn_OnCompleted(object sender, EventArgs e) => IsTextSet = false;
    }
}
