using System;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class ProjectCapabilityTestProjectIndicatorTests
    {
        private ProjectCapabilityTestProjectIndicator testSubject;
        private static Mock<IVsBooleanSymbolExpressionEvaluator> booleanEvaluator;
        private static Mock<ILocalRegistry> localRegister;
        private ConfigurableServiceProvider serviceProvider;
        private const string TestCapability = "TestContainer";

        [TestInitialize]
        public void TestInit()
        {
            serviceProvider = new ConfigurableServiceProvider();
            var configurableVsProjectSystemHelper = new ConfigurableVsProjectSystemHelper(serviceProvider);
            serviceProvider.RegisterService(typeof(IProjectSystemHelper), configurableVsProjectSystemHelper);

            testSubject = new ProjectCapabilityTestProjectIndicator(serviceProvider);
        }

        // [TestMethod]
        // public void Ctor_NullServiceProvider_ArgumentNullException()
        // {
        //     Action act = () => new ProjectCapabilityTestProjectIndicator(null);
        //
        //     act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        // }
        //
        // [TestMethod]
        // public void IsTestProject_ProjectHasNoCapabilities_False()
        // {
        //     var projectMock = new ProjectMock("csproj.csproj");
        //
        //     var actual = testSubject.IsTestProject(projectMock);
        //     actual.Should().BeFalse();
        // }

        [TestMethod]
        public void IsTestProject_ProjectHasNonTestCapability_False()
        {
            var projectMock = new ProjectMock("csproj.csproj");
            SetCapability(projectMock, "some other capability");

            var actual = testSubject.IsTestProject(projectMock);
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void IsTestProject_ProjectHasTestCapability_True()
        {
            booleanEvaluator = new Mock<IVsBooleanSymbolExpressionEvaluator>();
            booleanEvaluator
                .Setup(x => x.EvaluateExpression(TestCapability, TestCapability))
                .Returns(true);

            localRegister = new Mock<ILocalRegistry>();
            var iidIunknown = VSConstants.IID_IUnknown;
            var iUnknownForObject = Marshal.GetIUnknownForObject(booleanEvaluator.Object);

            localRegister
                .Setup(x => x.CreateInstance(typeof(BooleanSymbolExpressionEvaluator).GUID, (object)null,
                    ref iidIunknown, 1U, out iUnknownForObject));

            serviceProvider.RegisterService(typeof(SLocalRegistry), localRegister.Object);
            serviceProvider.RegisterService(typeof(SVsActivityLog), Mock.Of<IVsActivityLog>());
            ServiceProvider.CreateFromSetSite(serviceProvider);


            var projectMock = new ProjectMock("csproj.csproj");
            SetCapability(projectMock, TestCapability);

            try
            {
                var actual = testSubject.IsTestProject(projectMock);
                actual.Should().BeTrue();
            }
            catch (Exception e)
            {
                localRegister
                    .Verify(x => x.CreateInstance(typeof(BooleanSymbolExpressionEvaluator).GUID, (object)null,
                        ref iidIunknown, 1U, out iUnknownForObject));

                Console.WriteLine(e);
                throw;
            }
        
        }

        private static void SetCapability(ProjectMock projectMock, string capability)
        {
            var vsHierarchy = projectMock as IVsHierarchy;
            vsHierarchy.SetProperty(VSConstants.VSITEMID_ROOT, -2124, capability);
        }

        private static void SetupCapabilityEvaluator(ConfigurableServiceProvider serviceProvider)
        {

        }
    }
}
