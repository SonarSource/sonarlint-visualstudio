using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class IProjectSystemHelperExtensionsTests
    {
        #region Test boilerplate

        private ConfigurableVsProjectSystemHelper projectSystem;

        [TestInitialize]
        public void TestInitialize()
        {
            var sp = new ConfigurableServiceProvider();
            this.projectSystem = new ConfigurableVsProjectSystemHelper(sp);
        }

        #endregion

        [TestMethod]
        public void IProjectSystemHelperExtensions_IsKnownTestProject_ArgChecks()
        {
            // Setup
            IVsHierarchy vsProject = new ProjectMock("myproject.proj");

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => IProjectSystemHelperExtensions.IsKnownTestProject(null, vsProject));
            Exceptions.Expect<ArgumentNullException>(() => IProjectSystemHelperExtensions.IsKnownTestProject(this.projectSystem, null));
        }

        [TestMethod]
        public void IProjectSystemHelperExtensions_IsKnownTestProject_IsTestProject_ReturnsTrue()
        {
            // Setup
            var vsProject = new ProjectMock("myproject.proj");
            vsProject.SetAggregateProjectTypeGuids(ProjectSystemHelper.TestProjectKindGuid);

            // Act + Verify
            Assert.IsTrue(IProjectSystemHelperExtensions.IsKnownTestProject(this.projectSystem, vsProject),
                "Expected project with test project kind to be known test project");
        }

        [TestMethod]
        public void IProjectSystemHelperExtensions_IsKnownTestProject_IsNotTestProject_ReturnsFalse()
        {
            // Setup
            var vsProject = new ProjectMock("myproject.proj");

            // Act + Verify
            Assert.IsFalse(IProjectSystemHelperExtensions.IsKnownTestProject(this.projectSystem, vsProject),
                "Expected project without test project kind NOT to be known test project");
        }
    }
}
