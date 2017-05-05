using System;
using System.IO;
using System.Linq;
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
            Assert.IsFalse(daemon.IsInstalled());
            Assert.IsFalse(daemon.IsRunning);
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
            daemon.Stop();
        }

        [TestMethod]
        [Ignore]
        public void Install_Reinstall_Run()
        {
            daemon.Install();
            Assert.AreEqual(1, Directory.EnumerateFiles(tempPath).Count());
            Assert.AreEqual(1, Directory.EnumerateDirectories(storagePath).Count());
            Assert.IsTrue(daemon.IsInstalled());
            Assert.IsFalse(daemon.IsRunning);

            daemon.Install();
            Assert.AreEqual(1, Directory.EnumerateFiles(tempPath).Count());
            Assert.AreEqual(1, Directory.EnumerateDirectories(storagePath).Count());
            Assert.IsTrue(daemon.IsInstalled());
            Assert.IsFalse(daemon.IsRunning);

            daemon.Start();
            Assert.IsTrue(daemon.IsInstalled());
            Assert.IsTrue(daemon.IsRunning);
            daemon.Stop();
            Assert.IsFalse(daemon.IsRunning);
        }

        [TestCleanup]
        public void CleanUp()
        {
            //ForceDeleteDirectory(tempPath);
            //ForceDeleteDirectory(storagePath);
        }

        private static void ForceDeleteDirectory(string path)
        {
            var files = Directory.GetFiles(path);
            var directories = Directory.GetDirectories(path);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in directories)
            {
                ForceDeleteDirectory(dir);
            }

            Directory.Delete(path, true);
        }
    }
}
