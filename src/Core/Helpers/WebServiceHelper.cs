﻿/*
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
using System.Net.Http;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.Core
{
    public static class WebServiceHelper
    {
        public static async Task<T> SafeServiceCallAsync<T>(Func<Task<T>> call, ILogger logger)
        {
            try
            {
                return await call();
            }
            catch (HttpRequestException e)
            {
                // For some errors we will get an inner exception which will have a more specific information
                // that we would like to show i.e. when the host could not be resolved
                var innerException = e.InnerException as System.Net.WebException;
                logger.WriteLine(CoreStrings.SonarQubeRequestFailed, e.Message, innerException?.Message);
            }
            catch (TaskCanceledException)
            {
                // Canceled or timeout
                logger.WriteLine(CoreStrings.SonarQubeRequestTimeoutOrCancelled);
            }
            catch (OperationCanceledException)
            {
                // Canceled or timeout
                logger.WriteLine(CoreStrings.SonarQubeRequestTimeoutOrCancelled);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(CoreStrings.SonarQubeRequestFailed, ex.Message, null);
            }

            return default(T);
        }

        public static async Task SafeServiceCallAsync(Func<Task> call, ILogger logger)
        {
            await SafeServiceCallAsync(async () => { await call(); return 0; }, logger);
        }
    }
}
