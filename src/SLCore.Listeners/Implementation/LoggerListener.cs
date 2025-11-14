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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Logger;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class LoggerListener(ILogger logger) : ILoggerListener
{
    private readonly ILogger logger = logger.ForContext(SLCoreStrings.SLCoreName);

    public void Log(LogParams parameters)
    {
        var additionalContext = new MessageLevelContext { VerboseContext = [parameters.loggerName, parameters.configScopeId, parameters.threadName] };

        if (parameters.message != null)
        {
            switch (parameters.level)
            {
                case LogLevel.ERROR or LogLevel.WARN:
                    logger.WriteLine(additionalContext, parameters.message);
                    break;
                case LogLevel.INFO or LogLevel.DEBUG or LogLevel.TRACE:
                    logger.LogVerbose(additionalContext, parameters.message);
                    break;
            }
        }

        if (parameters.stackTrace != null)
        {
            logger.LogVerbose(additionalContext, parameters.stackTrace);
        }
    }
}
