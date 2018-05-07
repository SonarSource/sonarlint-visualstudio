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
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    internal static class WebServiceHelper
    {
        public static async Task<T> SafeServiceCall<T>(Func<Task<T>> call, ILogger logger)
        {
            try
            {
                return await call();
            }
            catch (HttpRequestException e)
            {
                // For some errors we will get an inner exception which will have a more specific information
                // that we would like to show i.e.when the host could not be resolved
                var innerException = e.InnerException as System.Net.WebException;
                logger.WriteLine(Strings.SonarQubeRequestFailed, e.Message, innerException?.Message);
            }
            catch (TaskCanceledException)
            {
                // Canceled or timeout
                logger.WriteLine(Strings.SonarQubeRequestTimeoutOrCancelled);
            }
            catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.SonarQubeRequestFailed, ex.Message, null);
            }

            return default(T);
        }

        public static async Task SafeServiceCall(Func<Task> call, ILogger logger)
        {
            await SafeServiceCall(async () => { await call(); return 0; }, logger);
        }

        // Real bug found in Roslyn - see https://github.com/dotnet/roslyn/pull/21258
        public static bool TryRedirect(AssemblyName name, byte[] token, int major, int minor, int build, int revision)
        {
            var version = new Version(major, minor, revision, build);
            if (KeysEqual(name.GetPublicKeyToken(), token) && name.Version < version)
            {
                name.Version = version;
                return true;
            }

            return false;
        }

        private static bool KeysEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
