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
using SonarLint.VisualStudio.Core.VsVersion;

namespace SonarLint.VisualStudio.Core.Notifications
{
    public interface IDisabledNotificationsStorage
    {
        void DisableNotification(string id);
        bool IsNotificationDisabled(string id);
    }

    [Export(typeof(IDisabledNotificationsStorage))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DisabledNotificationsStorage : IDisabledNotificationsStorage
    {
        private readonly IVsVersionProvider vsVersionProvider;
        private readonly IFileSystem fileSystem;
        private readonly string filePath;

        private IList<string> disabledNotifications = null;

        [ImportingConstructor]
        public DisabledNotificationsStorage(IVsVersionProvider vsVersionProvider) : this(vsVersionProvider, new FileSystem())
        {

        }

        internal /*for testing*/ DisabledNotificationsStorage(IVsVersionProvider vsVersionProvider, IFileSystem fileSystem)
        {
            this.vsVersionProvider = vsVersionProvider;
            this.fileSystem = fileSystem;

            filePath = GetFilePath();
        }

        public void DisableNotification(string id)
        {
            if(IsNotificationDisabled(id))
            {
                return;
            }

            disabledNotifications.Add(id);
            SaveNotifications();
        }

        public bool IsNotificationDisabled(string id)
        {
            if (disabledNotifications == null)
            {
                disabledNotifications = ReadDisabledNotifications();
            }
            return disabledNotifications.Contains(id);
        }

        private IList<string> ReadDisabledNotifications()
        {
            return fileSystem.File.ReadAllLines(filePath).ToList();
        }

        private void SaveNotifications()
        {
            fileSystem.File.WriteAllLines(filePath, disabledNotifications);
        }
        
        private string GetFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string fullPath = Path.Combine(appData, "SonarLint for Visual Studio", vsVersionProvider.Version.MajorInstallationVersion, "disabledNotifications.txt");

            return fullPath;
        }


    }
}
