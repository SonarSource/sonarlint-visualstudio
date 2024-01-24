/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Moq;

namespace SonarLint.VisualStudio.TestInfrastructure
{
    public static class MefTestHelpers
    {
        [DebuggerStepThrough]
        public static Export CreateExport<T>() where T: class
        {
            return CreateExport<T>(Mock.Of<T>());
        }

        [DebuggerStepThrough]
        public static Export CreateExport<T>(object exportInstance, string contractName = null)
        {
            // Add the required export ID so that MEF knows which contract it exports.
            var metadata = new Dictionary<string, object>()
            {
                {  "ExportTypeIdentity", AttributedModelServices.GetTypeIdentity(typeof(T)) }
            };

            contractName ??= AttributedModelServices.GetContractName(typeof(T));
            var export = new Export(contractName, metadata, () => exportInstance);

            return export;
        }

        /// <summary>
        /// Overload that imports using a SingleObjectImporter and checks that the expected
        /// implementing type was imported.
        /// </summary>
        /// <remarks>This method can be used if the import being tested doesn't specify an explicit contract name.
        /// If it does use an explicit contract name you will need to call the other overload and supply an
        /// importer with a property with the appropriate [Import("MyContractName")] attribute.</remarks>
        public static SingleObjectImporter<TImportType> CheckTypeCanBeImported<TTypeToCheck, TImportType>(
            params Export[] requiredExports)
            where TImportType : class
            where TTypeToCheck : class
        {
            var importer = new SingleObjectImporter<TImportType>();

            CheckTypeCanBeImported<TTypeToCheck>(importer, requiredExports);

            importer.AssertImportIsNotNull();
            importer.AssertImportIsInstanceOf<TTypeToCheck>();
            return importer;
        }
        
        /// <summary>
        /// Tests that the <typeparamref name="TTypeToCheck"/> can be imported using MEF.
        /// If required exports are supplied then the test also checks that the type cannot be
        /// imported without them.
        /// </summary>
        /// <typeparam name="TTypeToCheck">The type to check</typeparam>
        /// <param name="importer">The instance that is importing values</param>
        /// <param name="requiredExports">Any exports required by TTypeToCheck. Optional.</param>
        private static void CheckTypeCanBeImported<TTypeToCheck>(
            object importer,
            Export[] requiredExports)
            where TTypeToCheck : class
        {
            if (importer == null)
            {
                throw new ArgumentNullException(nameof(importer));
            }

            if (requiredExports == null)
            {
                requiredExports = Array.Empty<Export>();
            }

            for (int i = 0; i < requiredExports.Count(); i++)
            {
                // Try importing when not all of the required exports are available -> exception
                var subsetOfRequiredExports = new List<Export>(requiredExports);
                var exportToRemove = subsetOfRequiredExports[i];
                Console.WriteLine($"Removing export: {exportToRemove.Definition.ContractName}");
                subsetOfRequiredExports.RemoveAt(i);

                Action act = () => Compose(importer, typeof(TTypeToCheck), subsetOfRequiredExports.ToArray());
                act.Should().Throw<CompositionException>();
            }

            // Finally try importing when all of the required exports are available -> success
            Compose(importer, typeof(TTypeToCheck), requiredExports);
        }

        /// <summary>
        /// Performs a MEF compose with the supplied objects
        /// </summary>
        /// <param name="importer">The instance that is importing the exported type</param>
        /// <param name="typeToExport">The type to of export being tested</param>
        /// <param name="additionalExports">Any other exports that are required. Can be empty.</param>
        public static void Compose(object importer, Type typeToExport, params Export[] additionalExports)
            => Compose(
                new object[]{ importer },
                new Type[]{ typeToExport },
                additionalExports);

        /// <summary>
        /// Performs a MEF compose with the supplied objects
        /// </summary>
        /// <param name="importers">The instance that is importing the exported type</param>
        /// <param name="typesToExport">The types of export being tested</param>
        /// <param name="additionalExports">Any other exports that are required. Can be empty.</param>
        /// <remarks>Note: this overload is useful for testing types that have multiple exports
        /// i.e. that export multiple types.</remarks>
        public static void Compose(object[] importers, Type[] typesToExport, params Export[] additionalExports)
        {
            // Create a type catalog that only contains the type to be checked to
            // make sure we don't pick up any other types
            var catalog = new TypeCatalog(typesToExport);

            var batch = new CompositionBatch();
            
            foreach(var importer in importers)
            {
                batch.AddPart(importer);
            }

            foreach (Export item in additionalExports)
            {
                batch.AddExport(item);
            }

            DoCompose(catalog, batch);
        }

        /// <summary>
        /// Performs a MEF composition with the supplied objects
        /// </summary>
        /// <param name="exportingAssembly">The assembly containing the types being exported</param>
        /// <param name="importers">The instance(s) that are importing types from the assembly</param>
        public static void Compose(Assembly exportingAssembly, params object[] importers)
        {
            if (importers.Length == 0)
            {
                throw new InvalidOperationException("Test setup error: must specify at least one importer");
            }

            var batch = new CompositionBatch();

            foreach (var importer in importers)
            {
                batch.AddPart(importer);
            }

            var catalog = new AssemblyCatalog(exportingAssembly);

            DoCompose(catalog, batch);
        }

        private static void DoCompose(ComposablePartCatalog catalog, CompositionBatch batch)
        {
            var container = new CompositionContainer(catalog);

            try
            {
                container.Compose(batch);

            }
            finally
            {
                SafeDispose(container);
            }
        }

        private static void SafeDispose(CompositionContainer container)
        {
            // Clean up, ignoring any errors
            try
            {
                Console.WriteLine("Disposing composition container...");
                container.Dispose();
                throw new InvalidCastException("test");

            }
            catch (Exception ex)
            {
                // Disposing the container will dispose the parts it created. That might cause exceptions in our
                // code. Since we're testing whether objects can be imported here we're not interested in
                // exceptions when disposing the objects -> dump to console and ignore
                Console.WriteLine($"Error disposing composition container: {ex}");
            }
        }

        /// <summary>
        /// Check if type has creation policy non shared.
        /// </summary>m
        public static void CheckIsNonSharedMefComponent<T>()
        {
            var creationPolicy = GetCreationPolicyFromAttribute<T>();
            creationPolicy.Should().Be(CreationPolicy.NonShared);
        }

        /// <summary>
        /// Check if type has creation policy shared.
        /// </summary>
        public static void CheckIsSingletonMefComponent<T>()
        {
            var creationPolicy = GetCreationPolicyFromAttribute<T>();
            creationPolicy.Should().Be(CreationPolicy.Shared);
        }

        /// <summary>
        /// Checks that a type supports two exports, and that importing either exported
        /// type returns the same instance
        /// </summary>
        /// <param name="exportingType">The type being tested</param>
        /// <param name="additionalExports">Any other exports that are required. Can be empty.</param>
        public static void CheckMultipleExportsReturnSameInstance<TTypeToCheck, TExport1, TExport2>(params Export[] additionalExports)
            where TExport1 : class // the first expected MEF export
            where TExport2 : class // the second expected MEF export
        {
            var importer1 = new SingleObjectImporter<TExport1>();
            var importer2 = new SingleObjectImporter<TExport2>();
            var importers = new object[] { importer1, importer2 };

            Compose(importers, new[] { typeof(TTypeToCheck) }, additionalExports);
            importer1.Import.Should().NotBeNull();
            importer2.Import.Should().NotBeNull();
            importer1.Import.Should().BeSameAs(importer2.Import);
        }

        private static CreationPolicy GetCreationPolicyFromAttribute<T>()
        {
            var customAttributes = typeof(T).GetCustomAttributes(typeof(PartCreationPolicyAttribute), true);
            customAttributes.Should().ContainSingle();

            var partCreationPolicyAttribute = (PartCreationPolicyAttribute)customAttributes[0];

            return partCreationPolicyAttribute.CreationPolicy;
        }
    }
}
