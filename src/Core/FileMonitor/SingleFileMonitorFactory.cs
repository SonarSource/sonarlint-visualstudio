/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.IO.Abstractions;

namespace SonarLint.VisualStudio.Core.FileMonitor;

public interface ISingleFileMonitorFactory
{
    ISingleFileMonitor Create(string filePathToMonitor);
}

[Export(typeof(ISingleFileMonitorFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class SingleFileMonitorFactory : ISingleFileMonitorFactory
{
    private readonly ILogger logger;

    [ImportingConstructor]
    public SingleFileMonitorFactory(ILogger logger)
    {
        this.logger = logger;
    }

    public ISingleFileMonitor Create(string filePathToMonitor)
    {
        if (filePathToMonitor == null)
        {
            throw new ArgumentNullException(nameof(filePathToMonitor));
        }

        return new SingleFileMonitor(new FileSystemWatcherFactory(), new FileSystem(), filePathToMonitor, logger);
    }
}
