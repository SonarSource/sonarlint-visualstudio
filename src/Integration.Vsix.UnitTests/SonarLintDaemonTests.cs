/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarLintDaemonTests
    {
        private string tempPath;
        private string storagePath;
        private SonarLintDaemon daemon;

        [TestInitialize]
        public void SetUp()
        {
            tempPath = Path.Combine(Path.GetRandomFileName());
            storagePath = Path.Combine(Path.GetRandomFileName());
            Directory.CreateDirectory(tempPath);
            Directory.CreateDirectory(storagePath);
            daemon = new SonarLintDaemon(SonarLintDaemon.daemonVersion, storagePath, tempPath);
        }

        [TestMethod]
        public void Not_Installed()
        {
            daemon.IsInstalled.Should().BeFalse();
            daemon.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Run_Without_Install()
        {
            daemon.Start();
        }

        [TestMethod]
        public void Stop_Without_Start_Has_No_Effect()
        {
            daemon.IsRunning.Should().BeFalse(); // Sanity test
            daemon.Stop();
            daemon.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        [Ignore]
        public void Install_Reinstall_Run()
        {
            InstallDaemon();
            Directory.GetFiles(tempPath).Length.Should().Be(1);
            Directory.GetDirectories(storagePath).Length.Should().Be(1);
            Assert.IsTrue(daemon.IsInstalled);
            Assert.IsFalse(daemon.IsRunning);

            InstallDaemon();
            Directory.GetFiles(tempPath).Length.Should().Be(1);
            Directory.GetDirectories(storagePath).Length.Should().Be(1);
            daemon.IsInstalled.Should().BeTrue();
            daemon.IsRunning.Should().BeFalse();

            daemon.Start();
            daemon.IsInstalled.Should().BeTrue();
            daemon.IsRunning.Should().BeTrue();
            daemon.Stop();
            daemon.IsRunning.Should().BeFalse();
        }

        [TestCleanup]
        public void CleanUp()
        {
            ForceDeleteDirectory(tempPath);
            ForceDeleteDirectory(storagePath);
        }

        private static void ForceDeleteDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Thread.Sleep(1);
                Directory.Delete(path, true);
            }
        }

        private void InstallDaemon()
        {
            daemon.Install((object sender, DownloadProgressChangedEventArgs e) => { }, (object sender, AsyncCompletedEventArgs e) => { });
        }
    }
}
