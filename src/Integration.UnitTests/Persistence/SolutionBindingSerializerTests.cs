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
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionBindingSerializerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsOutputWindowPane outputPane;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableSourceControlledFileSystem sourceControlledFileSystem;
        private ConfigurableSolutionRuleSetsInformationProvider solutionRuleSetsInfoProvider;
        private DTEMock dte;
        private ConfigurableCredentialStore store;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            this.dte = new DTEMock();
            this.dte.Solution = new SolutionMock(dte, Path.Combine(this.TestContext.TestRunDirectory, this.TestContext.TestName, "solution.sln"));
            this.serviceProvider.RegisterService(typeof(DTE), this.dte);

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.store = new ConfigurableCredentialStore();
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.projectSystemHelper.SolutionItemsProject = this.dte.Solution.AddOrGetProject("Solution Items");
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);

            this.sourceControlledFileSystem = new ConfigurableSourceControlledFileSystem();
            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), this.sourceControlledFileSystem);

            this.solutionRuleSetsInfoProvider = new ConfigurableSolutionRuleSetsInformationProvider();
            this.solutionRuleSetsInfoProvider.SolutionRootFolder = Path.GetDirectoryName(this.dte.Solution.FilePath);
            this.serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider), this.solutionRuleSetsInfoProvider);
        }

        [TestMethod]
        public void SolutionBindingSerializer_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingSerializer(null));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingSerializer(this.serviceProvider, null));
        }

        [TestMethod]
        public void SolutionBindingSerializer_WriteSolutionBinding_ArgChecks()
        {
            // Arrange
            SolutionBindingSerializer testSubject = this.CreateTestSubject();

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.WriteSolutionBinding(null));
        }

        [TestMethod]
        public void SolutionBindingSerializer_WriteSolutionBinding_ReadSolutionBinding()
        {
            // Arrange
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var creds = new BasicAuthCredentials("user", "pwd".ToSecureString());
            var projectKey = "MyProject Key";
            var written = new BoundSonarQubeProject(serverUri, projectKey, creds);

            // Act (write)
            string output = testSubject.WriteSolutionBinding(written);
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();
            output.Should().NotBeNull("Expected a real file");
            this.TestContext.AddResultFile(output);
            File.Exists(output).Should().BeTrue("Expected a real file");

            // Assert
            this.store.data.Should().ContainKey(serverUri);

            // Act (read)
            BoundSonarQubeProject read = testSubject.ReadSolutionBinding();

            // Assert
            var newCreds = read.Credentials as BasicAuthCredentials;
            newCreds.Should().NotBe(creds, "Different credential instance were expected");
            newCreds.UserName.Should().Be(creds.UserName);
            newCreds.Password.ToUnsecureString().Should().Be(creds.Password.ToUnsecureString());
            read.ServerUri.Should().Be(written.ServerUri);
            this.outputPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SolutionBindingSerializer_WriteSolutionBinding_ReadSolutionBinding_WithProfiles()
        {
            // Arrange
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var creds = new BasicAuthCredentials("user", "pwd".ToSecureString());
            var projectKey = "MyProject Key";
            var written = new BoundSonarQubeProject(serverUri, projectKey, creds);
            written.Profiles = new Dictionary<Language, ApplicableQualityProfile>();
            written.Profiles[Language.VBNET] = new ApplicableQualityProfile { ProfileKey = "VB" };
            written.Profiles[Language.CSharp] = new ApplicableQualityProfile { ProfileKey = "CS", ProfileTimestamp = DateTime.Now };

            // Act (write)
            string output = testSubject.WriteSolutionBinding(written);
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();
            output.Should().NotBeNull("Expected a real file");
            this.TestContext.AddResultFile(output);
            File.Exists(output).Should().BeTrue("Expected a real file");

            // Assert
            this.store.data.Should().ContainKey(serverUri);

            // Act (read)
            BoundSonarQubeProject read = testSubject.ReadSolutionBinding();

            // Assert
            var newCreds = read.Credentials as BasicAuthCredentials;
            newCreds.Should().NotBe(creds, "Different credential instance were expected");
            newCreds.UserName.Should().Be(creds.UserName);
            newCreds.Password.ToUnsecureString().Should().Be(creds.Password.ToUnsecureString());
            read.ServerUri.Should().Be(written.ServerUri);
            (read.Profiles?.Count ?? 0).Should().Be(2);
            read.Profiles[Language.VBNET].ProfileKey.Should().Be(written.Profiles[Language.VBNET].ProfileKey);
            read.Profiles[Language.VBNET].ProfileTimestamp.Should().Be(written.Profiles[Language.VBNET].ProfileTimestamp);
            read.Profiles[Language.CSharp].ProfileKey.Should().Be(written.Profiles[Language.CSharp].ProfileKey);
            read.Profiles[Language.CSharp].ProfileTimestamp.Should().Be(written.Profiles[Language.CSharp].ProfileTimestamp);
            this.outputPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SolutionBindingSerializer_WriteSolutionBinding_ReadSolutionBinding_OnRealStore()
        {
            // Arrange
            var testSubject = new SolutionBindingSerializer(this.serviceProvider);
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var projectKey = "MyProject Key";
            testSubject.Store.DeleteCredentials(serverUri);

            // Case 1: has credentials
            var creds = new BasicAuthCredentials("user", "pwd".ToSecureString());
            var written = new BoundSonarQubeProject(serverUri, projectKey, creds);

            // Act (write + read)
            BoundSonarQubeProject read = null;
            try
            {
                testSubject.WriteSolutionBinding(written);
                this.sourceControlledFileSystem.WritePendingNoErrorsExpected();
                read = testSubject.ReadSolutionBinding();
            }
            finally
            {
                testSubject.Store.DeleteCredentials(serverUri);
            }

            // Assert
            var newCreds = read.Credentials as BasicAuthCredentials;
            newCreds.Should().NotBe(creds, "Different credential instance were expected");
            newCreds.UserName.Should().Be(creds.UserName);
            newCreds?.Password.ToUnsecureString().Should().Be(creds.Password.ToUnsecureString());
            read.ServerUri.Should().Be(written.ServerUri);
            this.outputPane.AssertOutputStrings(0);

            // Case 2: has not credentials (anonymous)
            creds = null;
            written = new BoundSonarQubeProject(serverUri, projectKey, creds);

            // Act (write + read)
            read = null;
            try
            {
                testSubject.WriteSolutionBinding(written);
                read = testSubject.ReadSolutionBinding();
            }
            finally
            {
                testSubject.Store.DeleteCredentials(serverUri);
            }

            // Assert
            read.Credentials.Should().BeNull();
            read.ServerUri.Should().Be(written.ServerUri);
            this.outputPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SolutionBindingSerializer_ReadSolutionBinding_InvalidData()
        {
            // Arrange
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var creds = new BasicAuthCredentials("user", "pwd".ToSecureString());
            var projectKey = "MyProject Key";
            var written = new BoundSonarQubeProject(serverUri, projectKey, creds);
            string output = testSubject.WriteSolutionBinding(written);
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();
            output.Should().NotBeNull("Expected a real file");
            File.WriteAllText(output, "bla bla bla: bla");

            // Act (read)
            BoundSonarQubeProject read = testSubject.ReadSolutionBinding();

            // Assert
            read.Should().BeNull("Not expecting any binding information in case of error");
            this.outputPane.AssertOutputStrings(1);
        }

        [TestMethod]
        public void SolutionBindingSerializer_ReadSolutionBinding_NoFile()
        {
            // Arrange
            SolutionBindingSerializer testSubject = this.CreateTestSubject();

            // Act (read)
            BoundSonarQubeProject read = testSubject.ReadSolutionBinding();

            // Assert
            read.Should().BeNull("Not expecting any binding information when there is no file");
            this.outputPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SolutionBindingSerializer_ReadSolutionBinding_IOError()
        {
            // Arrange
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var creds = new BasicAuthCredentials("user", "pwd".ToSecureString());
            var projectKey = "MyProject Key";
            var written = new BoundSonarQubeProject(serverUri, projectKey, creds);
            string output = testSubject.WriteSolutionBinding(written);
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();
            using (new FileStream(output, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // Act (read)
                BoundSonarQubeProject read = testSubject.ReadSolutionBinding();

                // Assert
                read.Should().BeNull("Not expecting any binding information in case of error");
                this.outputPane.AssertOutputStrings(1);
            }
        }

        [TestMethod]
        public void SolutionBindingSerializer_ReadSolutionBinding_OnClosedSolution()
        {
            // Arrange
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            this.dte.Solution = new SolutionMock(dte, "" /*When the solution is closed the file is empty*/);

            // Act (read)
            BoundSonarQubeProject read = testSubject.ReadSolutionBinding();

            // Assert
            read.Should().BeNull();
        }

        [TestMethod]
        public void SolutionBindingSerializer_WriteSolutionBinding_IOError()
        {
            // Arrange
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var creds = new BasicAuthCredentials("user", "pwd".ToSecureString());
            var projectKey = "MyProject Key";
            var written = new BoundSonarQubeProject(serverUri, projectKey, creds);
            string output = testSubject.WriteSolutionBinding(written);
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();

            using (new FileStream(output, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // Act (write again)
                string output2 = testSubject.WriteSolutionBinding(written);
                this.sourceControlledFileSystem.WritePendingErrorsExpected();

                // Assert
                output2.Should().Be(output, "Same output is expected");
                this.outputPane.AssertOutputStrings(1);
            }
        }

        [TestMethod]
        public void SolutionBindingSerializer_WriteSolutionBinding_AddConfigFileToSolutionItemsFolder()
        {
            // Arrange
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var creds = new BasicAuthCredentials("user", "pwd".ToSecureString());
            var projectKey = "MyProject Key";
            var toWrite = new BoundSonarQubeProject(serverUri, projectKey, creds);
            ProjectMock solutionProject = (ProjectMock)this.projectSystemHelper.SolutionItemsProject;

            // Act
            string output = testSubject.WriteSolutionBinding(toWrite);

            // Assert that not actually done anything until the pending files were written
            this.store.data.Should().NotContainKey(serverUri);
            solutionProject.Files.ContainsKey(output).Should().BeFalse("Not expected to be added to solution items folder just yet");

            // Act (write pending)
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();

            // Assert
            this.store.data.Should().ContainKey(serverUri);
            solutionProject.Files.ContainsKey(output).Should().BeTrue("File {0} was not added to project", output);

            // Act (write again)
            string output2 = testSubject.WriteSolutionBinding(toWrite);
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();

            // Assert
            output2.Should().Be(output, "Should be the same file");
            this.store.data.Should().ContainKey(serverUri);
            solutionProject.Files.ContainsKey(output).Should().BeTrue("File {0} should remain in the project", output);
        }

        #region Helpers

        private SolutionBindingSerializer CreateTestSubject()
        {
            return new SolutionBindingSerializer(this.serviceProvider, this.store);
        }

        #endregion Helpers
    }
}