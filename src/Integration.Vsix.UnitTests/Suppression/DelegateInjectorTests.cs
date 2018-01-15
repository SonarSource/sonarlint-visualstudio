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
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;

/* Note: these tests load dummy assemblies so each test is run in a
 * separate AppDomain to ensure there are no side-effects on other tests.
 */

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class DelegateInjectorTests : MarshalByRefObject
    {
        public TestContext TestContext { get; set; }

        private const string SourceCodeWithStaticProperty = @"
namespace SonarAnalyzer.Helpers
{
  class SonarAnalysisContext
  {
    public static System.Func<Microsoft.CodeAnalysis.SyntaxTree, Microsoft.CodeAnalysis.Diagnostic, bool> ShouldDiagnosticBeReported { get; set; }
  }
}";
        private const string SonarAnalyzerAssemblyName = "SonarAnalyzer";

        private static ILogger dummyServiceProvider = new Mock<ILogger>().Object;
        private static Func<SyntaxTree, Diagnostic, bool> dummyFunction = (s, d) => true;

        [TestMethod]
        public void Injector_InvalidArguments()
        {
            Action op = () => new DelegateInjector(null, dummyServiceProvider);
            op.ShouldThrow<ArgumentNullException>();

            op = () => new DelegateInjector(dummyFunction, null);
            op.ShouldThrow<ArgumentNullException>();

            op = () => new DelegateInjector(null, null);
            op.ShouldThrow<ArgumentNullException>();
        }

        [TestMethod]
        public void Injector_ValidPreLoadedAssemblies_SetsPropertyOk()
        {
            using (var wrapper = new TestDomainWrapper<DelegateInjectorTests>())
            {
                wrapper.RemoteObject.Execute_ValidPreLoadedAssemblies_SetsPropertyOk();
            }
        }

        private void Execute_ValidPreLoadedAssemblies_SetsPropertyOk()
        {
            // Arrange
            // Multiple assemblies, some of which are valid
            Assembly withoutPropertyAsm = CreateAndLoadAssembly("withoutProperty", "2.9", "class MyClass{}");

            Assembly validAsm1 = CreateAndLoadAssembly(SonarAnalyzerAssemblyName.ToLowerInvariant(), "101.0.1", SourceCodeWithStaticProperty);
            Assembly validAsm2 = CreateAndLoadAssembly(SonarAnalyzerAssemblyName.ToUpperInvariant(), "101.0.2", SourceCodeWithStaticProperty);

            // Sanity check before starting
            AssertSuppressionPropertyIsNotSet(validAsm1);
            AssertSuppressionPropertyIsNotSet(validAsm2);
            AssertSuppressionPropertyDoesNotExist(withoutPropertyAsm);

            // Act - should set the property for all valid loaded assemblies
            using (new DelegateInjector(dummyFunction, dummyServiceProvider))
            {
                // nothing to do here
            };

            // Assert
            AssertSuppressionPropertyIsSet(validAsm1);
            AssertSuppressionPropertyIsSet(validAsm2);
        }

        [TestMethod]
        public void Injector_OldSonarAnalyzerWithoutProperty_NoError()
        {
            using (var wrapper = new TestDomainWrapper<DelegateInjectorTests>())
            {
                wrapper.RemoteObject.Execute_OldSonarAnalyzerWithoutProperty_NoError();
            }
        }

        private void Execute_OldSonarAnalyzerWithoutProperty_NoError()
        {
            // Arrange
            const string sourceContextButNoProperty = @"
namespace SonarAnalyzer.Helpers
{
  class SonarAnalysisContext {}
}";
            Assembly asm = CreateAndLoadAssembly(SonarAnalyzerAssemblyName, "102.0", sourceContextButNoProperty);

            // Act - should not error
            using (new DelegateInjector(dummyFunction, dummyServiceProvider))
            {
                // nothing to do here
            };
        }

        [TestMethod]
        public void Injector_WrongAssemblyName_NoErrorAndPropertyNotSet()
        {
            using (var wrapper = new TestDomainWrapper<DelegateInjectorTests>())
            {
                wrapper.RemoteObject.Execute_WrongAssemblyName_NoErrorAndPropertyNotSet();
            }
        }

        private void Execute_WrongAssemblyName_NoErrorAndPropertyNotSet()
        {
            // Arrange
            Assembly asm = CreateAndLoadAssembly("wrongAsmName", "103.0", SourceCodeWithStaticProperty);

            // Act
            using (new DelegateInjector(dummyFunction, dummyServiceProvider))
            {
                // nothing to do here
            };

            // Assert
            AssertSuppressionPropertyIsNotSet(asm);
        }

        [TestMethod]
        public void Injector_ErrorsSettingPropertyAreSuppressed()
        {
            using (var wrapper = new TestDomainWrapper<DelegateInjectorTests>())
            {
                wrapper.RemoteObject.Execute_ErrorsSettingPropertyAreSuppressed();
            }
        }

        private void Execute_ErrorsSettingPropertyAreSuppressed()
        {
            // Test that error setting the property are not propagated.

            // To do this, use an assembly where the namespace etc match, but the
            // property has an incompatible type -> setting the property will fail
            const string SourceCodeWithBadStaticProperty = @"
namespace SonarAnalyzer.Helpers
{
  class SonarAnalysisContext
  {
    public static string ShouldDiagnosticBeReported { get; set; }
  }
}";
            // Arrange
            Assembly asm = CreateAndLoadAssembly(SonarAnalyzerAssemblyName, "104.0", SourceCodeWithBadStaticProperty);
            AssertSuppressionPropertyIsNotSet(asm);

            // Act - should not error
            using (new AssertIgnoreScope()) // Missing output window service
            {
                using (new DelegateInjector(dummyFunction, dummyServiceProvider))
                {
                    // nothing to do here
                };

                AssertSuppressionPropertyIsNotSet(asm);
            }
        }

        [TestMethod]
        public void Injector_MonitorAssemblyLoading_SetsPropertyOk()
        {
            using (var wrapper = new TestDomainWrapper<DelegateInjectorTests>())
            {
                wrapper.RemoteObject.Execute_MonitorAssemblyLoading_SetsPropertyOk();
            }
        }

        private void Execute_MonitorAssemblyLoading_SetsPropertyOk()
        {
            // Act and assert - should set the property for valid new assemblies as they are loaded
            using (new DelegateInjector(dummyFunction, dummyServiceProvider))
            {
                Assembly validAsm1 = CreateAndLoadAssembly(SonarAnalyzerAssemblyName, "105.0.1", SourceCodeWithStaticProperty);
                AssertSuppressionPropertyIsSet(validAsm1);

                Assembly withoutPropertyAsm = CreateAndLoadAssembly("withoutProperty", "105.0", "class Class1{}");

                Assembly validAsm2 = CreateAndLoadAssembly(SonarAnalyzerAssemblyName, "105.0.2", SourceCodeWithStaticProperty);
                AssertSuppressionPropertyIsSet(validAsm2);
            };

            // Injector has been disposed so should not set the value
            Assembly validAsm3 = CreateAndLoadAssembly(SonarAnalyzerAssemblyName, "105.0.3", SourceCodeWithStaticProperty);
            AssertSuppressionPropertyIsNotSet(validAsm3);
        }

        #region Assertion methods

        private static void AssertSuppressionPropertyIsNotSet(Assembly asm)
        {
            object propertyValue = GetSuppressionPropertyValue(asm);
            propertyValue.Should().BeNull();
        }

        private static void AssertSuppressionPropertyIsSet(Assembly asm)
        {
            object propertyValue = GetSuppressionPropertyValue(asm);
            propertyValue.Should().NotBeNull();
        }

        private static void AssertSuppressionPropertyDoesNotExist(Assembly asm)
        {
            PropertyInfo propertyInfo = GetStaticProperty(asm);
            propertyInfo.Should().BeNull("not expecting the static property to exist in this test assembly");
        }

        private static object GetSuppressionPropertyValue(Assembly asm)
        {
            PropertyInfo propertyInfo = GetStaticProperty(asm);
            propertyInfo.Should().NotBeNull();

            return propertyInfo.GetValue(null);
        }

        private static PropertyInfo GetStaticProperty(Assembly asm)
        {
            Type contextType = asm.GetTypes().SingleOrDefault(t => t.Name == "SonarAnalysisContext");
            PropertyInfo propertyInfo = contextType?.GetProperty("ShouldDiagnosticBeReported", BindingFlags.Static | BindingFlags.Public);
            return propertyInfo;
        }

        #endregion

        #region Assembly creation

        private Assembly CreateAndLoadAssembly(string assemblyName, string assemblyVersion, string source)
        {
            Compilation comp = CreateCompilation(assemblyName, assemblyVersion, source);

            using (var stream = new MemoryStream())
            {
                EmitResult result = comp.Emit(stream);
                if (!result.Success)
                {
                    this.TestContext.WriteLine("Test setup error: compilation of test assembly failed. Errors:");
                    foreach (Diagnostic diag in result.Diagnostics)
                    {
                        this.TestContext.WriteLine(diag.ToString());
                    }

                    throw new Exception("Test setup error: failed to compile the assembly. See the output window for details");
                }

                Assembly asm = Assembly.Load(stream.ToArray());
                return asm;
            }
        }

        private static Compilation CreateCompilation(string assemblyName, string assemblyVersion, string source)
        {
            MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
            MetadataReference DiagnosticReference = MetadataReference.CreateFromFile(typeof(Diagnostic).Assembly.Location);

            source = $"[assembly: System.Reflection.AssemblyVersion(\"{assemblyVersion}\")]" + source;

            var comp = CSharpCompilation.Create(assemblyName,
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
                references: new[] { CorlibReference, SystemCoreReference, DiagnosticReference },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return comp;
        }

        #endregion
    }
}