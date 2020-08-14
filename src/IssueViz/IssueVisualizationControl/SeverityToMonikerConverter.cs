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
using System.Globalization;
using System.Windows.Data;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Helpers;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl
{
    public class SeverityToMonikerConverter : IValueConverter
    {
        private readonly IAnalysisSeverityToVsSeverityConverter analysisSeverityToVsSeverityConverter;

        public SeverityToMonikerConverter()
            : this(new AnalysisSeverityToVsSeverityConverter())
        {
        }

        internal SeverityToMonikerConverter(IAnalysisSeverityToVsSeverityConverter analysisSeverityToVsSeverityConverter)
        {
            this.analysisSeverityToVsSeverityConverter = analysisSeverityToVsSeverityConverter;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var severity = (AnalysisIssueSeverity)value;
            var vsSeverity = analysisSeverityToVsSeverityConverter.Convert(severity);

            switch (vsSeverity)
            {
                case __VSERRORCATEGORY.EC_ERROR:
                    return KnownMonikers.StatusError;
                case __VSERRORCATEGORY.EC_WARNING:
                    return KnownMonikers.StatusWarning;
                default:
                    return KnownMonikers.StatusInformation;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
