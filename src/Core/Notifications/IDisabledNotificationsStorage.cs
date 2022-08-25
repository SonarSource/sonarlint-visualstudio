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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Resources;
using SonarLint.VisualStudio.Core.VsVersion;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Core.Notifications
{
    public interface IDisabledNotificationsStorage
    {
        void DisableNotification(string id);
        bool IsNotificationDisabled(string id);
    }

    [Export(typeof(IDisabledNotificationsStorage))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class DisabledNotificationsStorage : IDisabledNotificationsStorage
    {
        private readonly IVsVersionProvider vsVersionProvider;
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;

        private string filePath => GetFilePath();

        private DisabledNotifications disabledNotifications = null;
        private readonly object lockObject = new object();

        [ImportingConstructor]
        public DisabledNotificationsStorage(IVsVersionProvider vsVersionProvider, ILogger logger) : this(vsVersionProvider, logger, new FileSystem())
        {
        }

        internal /*for testing*/ DisabledNotificationsStorage(IVsVersionProvider vsVersionProvider, ILogger logger, IFileSystem fileSystem)
        {
            this.vsVersionProvider = vsVersionProvider;
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        public void DisableNotification(string id)
        {
            lock (lockObject)
            {
                if (IsNotificationDisabled(id))
                {
                    return;
                }
                if (disabledNotifications == null)
                {
                    return;
                }

                disabledNotifications.AddNotification(id);
                SaveNotifications();
            }
        }

        public bool IsNotificationDisabled(string id)
        {
            lock (lockObject)
            {
                if (disabledNotifications == null)
                {
                    disabledNotifications = ReadDisabledNotifications();
                }
                if (disabledNotifications == null)
                {
                    logger.WriteLine(Strings.DisabledNotificationsFailedToLoad);
                    return false;
                }
                return disabledNotifications.Notifications.Any(n => n.Id == id);
            }
        }

        private DisabledNotifications ReadDisabledNotifications()
        {
            try
            {
                if (!fileSystem.File.Exists(filePath)) 
                { 
                    logger.LogDebug($"[Notifications]Disabled notifications file does not exist. File: {filePath}"); 
                    return new DisabledNotifications(); 
                }

                var fileContent = fileSystem.File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<DisabledNotifications>(fileContent);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Strings.DisabledNotificationReadError, ex.Message));                
            }
            return null;
        }

        private void SaveNotifications()
        {
            try
            {
                var fileContent = JsonConvert.SerializeObject(disabledNotifications, Formatting.Indented);
                fileSystem.File.WriteAllText(filePath, fileContent);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Strings.DisabledNotificationSaveError, ex.Message));
            }
        }
        
        private string GetFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string fullPath = Path.Combine(appData, "SonarLint for Visual Studio", vsVersionProvider.Version.MajorInstallationVersion, "disabledNotifications.txt");

            return fullPath;
        }
    }
}
