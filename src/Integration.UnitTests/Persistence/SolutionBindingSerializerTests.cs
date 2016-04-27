//-----------------------------------------------------------------------
// <copyright file="SolutionBindingSerializerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using System;
using System.IO;

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
            // Setup
            SolutionBindingSerializer testSubject = this.CreateTestSubject();

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.WriteSolutionBinding(null));
        }

        [TestMethod]
        public void SolutionBindingSerializer_WriteSolutionBinding_ReadSolutionBinding()
        {
            // Setup
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var creds = new BasicAuthCredentials("user", "pwd".ConvertToSecureString());
            var projectKey = "MyProject Key";
            var written = new BoundSonarQubeProject(serverUri, projectKey, creds);

            // Act (write)
            string output = testSubject.WriteSolutionBinding(written);
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();
            Assert.IsNotNull(output, "Expected a real file");
            this.TestContext.AddResultFile(output);
            Assert.IsTrue(File.Exists(output), "Expected a real file");

            // Verify
            this.store.AssertHasCredentials(serverUri);

            // Act (read)
            BoundSonarQubeProject read = testSubject.ReadSolutionBinding();

            // Verify
            var newCreds = read.Credentials as BasicAuthCredentials;
            Assert.AreNotEqual(creds, newCreds, "Different credential instance were expected");
            Assert.AreEqual(creds.UserName, newCreds.UserName);
            Assert.AreEqual(creds.Password.ConvertToUnsecureString(), newCreds.Password.ConvertToUnsecureString());
            Assert.AreEqual(written.ServerUri, read.ServerUri);
            this.outputPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SolutionBindingSerializer_WriteSolutionBinding_ReadSolutionBinding_OnRealStore()
        {
            // Setup
            var testSubject = new SolutionBindingSerializer(this.serviceProvider);
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var projectKey = "MyProject Key";
            testSubject.Store.DeleteCredentials(serverUri);

            // Case 1: has credentials
            var creds = new BasicAuthCredentials("user", "pwd".ConvertToSecureString());
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

            // Verify
            var newCreds = read.Credentials as BasicAuthCredentials;
            Assert.AreNotEqual(creds, newCreds, "Different credential instance were expected");
            Assert.AreEqual(creds.UserName, newCreds.UserName);
            Assert.AreEqual(creds.Password.ConvertToUnsecureString(), newCreds?.Password.ConvertToUnsecureString());
            Assert.AreEqual(written.ServerUri, read.ServerUri);
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

            // Verify
            Assert.IsNull(read.Credentials);
            Assert.AreEqual(written.ServerUri, read.ServerUri);
            this.outputPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SolutionBindingSerializer_ReadSolutionBinding_InvalidData()
        {
            // Setup
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var creds = new BasicAuthCredentials("user", "pwd".ConvertToSecureString());
            var projectKey = "MyProject Key";
            var written = new BoundSonarQubeProject(serverUri, projectKey, creds);
            string output = testSubject.WriteSolutionBinding(written);
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();
            Assert.IsNotNull(output, "Expected a real file");
            File.WriteAllText(output, "bla bla bla: bla");
            
            // Act (read)
            BoundSonarQubeProject read = testSubject.ReadSolutionBinding();

            // Verify
            Assert.IsNull(read, "Not expecting any binding information in case of error");
            this.outputPane.AssertOutputStrings(1);
        }

        [TestMethod]
        public void SolutionBindingSerializer_ReadSolutionBinding_NoFile()
        {
            // Setup
            SolutionBindingSerializer testSubject = this.CreateTestSubject();

            // Act (read)
            BoundSonarQubeProject read = testSubject.ReadSolutionBinding();

            // Verify
            Assert.IsNull(read, "Not expecting any binding information when there is no file");
            this.outputPane.AssertOutputStrings(0);
        }


        [TestMethod]
        public void SolutionBindingSerializer_ReadSolutionBinding_IOError()
        {
            // Setup
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var creds = new BasicAuthCredentials("user", "pwd".ConvertToSecureString());
            var projectKey = "MyProject Key";
            var written = new BoundSonarQubeProject(serverUri, projectKey, creds);
            string output = testSubject.WriteSolutionBinding(written);
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();
            using (new FileStream(output, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // Act (read)
                BoundSonarQubeProject read = testSubject.ReadSolutionBinding();

                // Verify
                Assert.IsNull(read, "Not expecting any binding information in case of error");
                this.outputPane.AssertOutputStrings(1);
            }
        }

        [TestMethod]
        public void SolutionBindingSerializer_ReadSolutionBinding_OnClosedSolution()
        {
            // Setup
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            this.dte.Solution = new SolutionMock(dte, "" /*When the solution is closed the file is empty*/);

            // Act (read)
            BoundSonarQubeProject read = testSubject.ReadSolutionBinding();

            // Verify
            Assert.IsNull(read);
        }

        [TestMethod]
        public void SolutionBindingSerializer_WriteSolutionBinding_IOError()
        {
            // Setup
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var creds = new BasicAuthCredentials("user", "pwd".ConvertToSecureString());
            var projectKey = "MyProject Key";
            var written = new BoundSonarQubeProject(serverUri, projectKey, creds);
            string output = testSubject.WriteSolutionBinding(written);
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();

            using (new FileStream(output, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // Act (write again)
                string output2 = testSubject.WriteSolutionBinding(written);
                this.sourceControlledFileSystem.WritePendingErrorsExpected();

                // Verify
                Assert.AreEqual(output, output2, "Same output is expected");
                this.outputPane.AssertOutputStrings(1);
            }
        }

        [TestMethod]
        public void SolutionBindingSerializer_WriteSolutionBinding_AddConfigFileToSolutionItemsFolder()
        {
            // Setup
            SolutionBindingSerializer testSubject = this.CreateTestSubject();
            var serverUri = new Uri("http://xxx.www.zzz/yyy:9000");
            var creds = new BasicAuthCredentials("user", "pwd".ConvertToSecureString());
            var projectKey = "MyProject Key";
            var toWrite = new BoundSonarQubeProject(serverUri, projectKey, creds);
            ProjectMock solutionProject = (ProjectMock)this.projectSystemHelper.SolutionItemsProject;

            // Act
            string output = testSubject.WriteSolutionBinding(toWrite);

            // Verify that not actually done anything until the pending files were written
            this.store.AssertHasNoCredentials(serverUri);
            Assert.IsFalse(solutionProject.Files.ContainsKey(output), "Not expected to be added to solution items folder just yet");

            // Act (write pending)
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();

            // Verify
            this.store.AssertHasCredentials(serverUri);
            Assert.IsTrue(solutionProject.Files.ContainsKey(output), "File {0} was not added to project", output);

            // Act (write again)
            string output2 = testSubject.WriteSolutionBinding(toWrite);
            this.sourceControlledFileSystem.WritePendingNoErrorsExpected();

            // Verify
            Assert.AreEqual(output, output2, "Should be the same file");
            this.store.AssertHasCredentials(serverUri);
            Assert.IsTrue(solutionProject.Files.ContainsKey(output), "File {0} should remain in the project project", output);
        }

        #region Helpers
        private SolutionBindingSerializer CreateTestSubject()
        {
            return new SolutionBindingSerializer(this.serviceProvider, this.store);
        }
        #endregion
    }
}
