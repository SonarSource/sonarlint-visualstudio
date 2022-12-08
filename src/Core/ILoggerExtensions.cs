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

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    public static class ILoggerExtensions
    {
        private static bool shouldLogDebug;

        static ILoggerExtensions() => Initialize(false);

        internal /* for testing*/ static void Initialize(bool shouldLogDebug)
        {
            ShouldLogDebug(shouldLogDebug);
        }

        public static void ShouldLogDebug(bool enable)
        {
            shouldLogDebug = enable;
        }

        /// <summary>
        /// Logs messages only when an environment variable is set. This is temporary
        /// solution for not having log verbosity setting.
        /// </summary>
        public static void LogDebug(this ILogger logger, string message, params object[] args)
        {
            if(shouldLogDebug)
            {
                var text = args.Length == 0 ? message : string.Format(message, args);
                logger.WriteLine("DEBUG: " + text);
            }
        }

        /// <summary>
        /// Extended debug logging that includes file, caller, thread and timestamp.
        public static void LogDebugExtended(this ILogger logger, string message, [CallerFilePath] string callerFilePath = null, [CallerMemberName] string callerMemberName = null)
        {
            if (!shouldLogDebug)
            {
                return;
            }

            var fileName = Path.GetFileNameWithoutExtension(callerFilePath); 
            var text = $"DEBUG: [{fileName}] [{callerMemberName}] [Thread: {Thread.CurrentThread.ManagedThreadId}, {DateTime.Now.ToString("hh:mm:ss.fff")}]  {message}";
            logger.WriteLine(text);
        }
    }
}
