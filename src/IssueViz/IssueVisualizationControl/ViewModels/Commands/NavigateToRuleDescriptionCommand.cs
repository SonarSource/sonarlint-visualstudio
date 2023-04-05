/*
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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands
{
    public interface INavigateToRuleDescriptionCommand : ICommand
    {
    }

    [Export(typeof(INavigateToRuleDescriptionCommand))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class NavigateToRuleDescriptionCommand : DelegateCommand, INavigateToRuleDescriptionCommand
    {
        [ImportingConstructor]
        public NavigateToRuleDescriptionCommand(IEducation educationService)
            : base(parameter =>
                {
                    var paramObject = (NavigateToRuleDescriptionCommandParam)parameter;
                    if (SonarCompositeRuleId.TryParse(paramObject.FullRuleKey, out var ruleId))
                    {
                        educationService.ShowRuleHelp(ruleId, paramObject.Context);
                    }
                },
                parameter => parameter is NavigateToRuleDescriptionCommandParam s &&
                    !string.IsNullOrEmpty(s.FullRuleKey) &&
                SonarCompositeRuleId.TryParse(s.FullRuleKey, out var _))
        {
        }
    }

    internal class NavigateToRuleDescriptionCommandParam
    {
        public string FullRuleKey { get; set; }
        public string Context { get; set; }
    }

    public class NavigateToRuleDescriptionCommandConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is string && values[1] is string)
            {
                return new NavigateToRuleDescriptionCommandParam { FullRuleKey = (string)values[0], Context = (string)values[1] };
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            Debug.Fail("We should not hit here");
            return null;
        }
    }
}
