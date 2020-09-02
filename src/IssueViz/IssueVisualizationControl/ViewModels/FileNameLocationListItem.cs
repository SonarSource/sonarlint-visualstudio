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
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels
{
    internal interface IFileNameLocationListItem : ILocationListItem, INotifyPropertyChanged, IDisposable
    {
        string FullPath { get; }
        string FileName { get; }
        object Icon { get; }
    }

    internal sealed class FileNameLocationListItem : IFileNameLocationListItem
    {
        private readonly IVsImageService2 vsImageService;
        private readonly ILogger logger;
        private readonly IAnalysisIssueLocationVisualization location;

        public string FullPath { get; private set; }
        public string FileName { get; private set; }
        public object Icon { get; private set; }

        public FileNameLocationListItem(IAnalysisIssueLocationVisualization location, IVsImageService2 vsImageService, ILogger logger)
        {
            this.location = location;
            this.vsImageService = vsImageService;
            this.logger = logger;
            location.PropertyChanged += Location_PropertyChanged;

            UpdateState();
        }

        private void Location_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IAnalysisIssueLocationVisualization.CurrentFilePath))
            {
                UpdateState();
            }
        }

        private void UpdateState()
        {
            FullPath = location.CurrentFilePath;
            FileName = Path.GetFileName(FullPath);
            Icon = GetImageMonikerForFile(FullPath);

            NotifyPropertyChanged(string.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            location.PropertyChanged -= Location_PropertyChanged;
        }

        private object GetImageMonikerForFile(string filePath)
        {
            try
            {
                return vsImageService.GetImageMonikerForFile(filePath);
            }
            catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
            {
                logger.WriteLine(Resources.ERR_FailedToGetFileImageMoniker, filePath, e);

                return KnownMonikers.Blank;
            }
        }
    }
}
