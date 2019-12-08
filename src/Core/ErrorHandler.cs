/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;

// Note: functionally equivalent to Microsoft.VisualStudio.ErrorHandler

namespace SonarLint.VisualStudio.Core
{
    public static class ErrorHandler
    {
        // Note: same set of exceptions as used by Microsoft.VisualStudio.ErrorHandler.IsCriticalException
        private static readonly HashSet<Type> criticalExceptions = new HashSet<Type>
        {
            typeof(StackOverflowException),
            typeof(AccessViolationException),
            typeof(AppDomainUnloadedException),
            typeof(BadImageFormatException),
            typeof(DivideByZeroException)
        };

        /// <summary>
        /// Returns whether the exception is critical, or is an aggregate exception that contains
        /// a critical exception.
        /// </summary>
        /// <remarks>This function is a combination of the VS ErrorHandler.IsCriticalException and
        /// ErrorHandler.ContainsCriticalException methods</remarks>
        public static bool IsCriticalException(Exception ex)
        {
            return criticalExceptions.Contains(ex.GetType()) ||
                (ex is AggregateException aggregateException && aggregateException.InnerExceptions.Any(IsCriticalException));
        }
    }
}
