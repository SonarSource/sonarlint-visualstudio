//-----------------------------------------------------------------------
// <copyright file="MefTestHelpers.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public static class MefTestHelpers
    {
        public static Export CreateExport<T>(object exportInstance)
        {
            return CreateExport<T>(exportInstance, new Dictionary<string, object>());
        }

        public static Export CreateExportWithMetadata<T>(object exportInstance, string metadataKey, string metadataValue)
        {
            Dictionary<string, object> metadata = new Dictionary<string, object>();
            metadata[metadataKey] = metadataValue;

            return CreateExport<T>(exportInstance, metadata);
        }

        public static Export CreateExport<T>(object exportInstance, IDictionary<string, object> metadata)
        {
            // Add the required export ID so that MEF knows which contract it exports.
            metadata.Add("ExportTypeIdentity", AttributedModelServices.GetTypeIdentity(typeof(T)));

            string contractName = AttributedModelServices.GetContractName(typeof(T));
            Export export = new Export(contractName, metadata, () => exportInstance);

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
            IEnumerable<Export> optionalExports,
            IEnumerable<Export> requiredExports)
            where TImportType : class
            where TTypeToCheck : class
        {
            SingleObjectImporter<TImportType> importer = new SingleObjectImporter<TImportType>();

            CheckTypeCanBeImported<TTypeToCheck, TImportType>(importer, optionalExports, requiredExports);

            importer.AssertImportIsNotNull();
            importer.AssertImportIsInstanceOf<TTypeToCheck>();
            return importer;
        }

        /// <summary>
        /// Tests that the <typeparamref name="TTypeToCheck"/> can be imported using MEF as an
        /// instance of <typeparamref name="TImportType"/>.
        /// If required exports are supplied then the test also checks that the type cannot be
        /// imported without them.
        /// </summary>
        /// <typeparam name="TTypeToCheck">The type to check</typeparam>
        /// <typeparam name="TImportType">The import contract type</typeparam>
        /// <param name="importer">The instance that is importing values</param>
        /// <param name="optionalExports">Any optional exports to include in the composition.</param>
        /// <param name="requiredExports">Any exports required by TTypeToCheck. Optional.</param>
        public static void CheckTypeCanBeImported<TTypeToCheck, TImportType>(
            object importer,
            IEnumerable<Export> optionalExports,
            IEnumerable<Export> requiredExports)
            where TImportType : class
            where TTypeToCheck : class
        {
            if (requiredExports == null)
            {
                requiredExports = Enumerable.Empty<Export>();
            }
            if (optionalExports == null)
            {
                optionalExports = Enumerable.Empty<Export>();
            }
            // Create a type catalog that only contains the type to be checked to
            // make sure we don't pick up any other types
            TypeCatalog catalog = new TypeCatalog(typeof(TTypeToCheck));

            for (int i = 0; i < requiredExports.Count(); i++)
            {
                // Try importing when not all of the required exports are available -> exception
                Exceptions.Expect<Exception>(() => TryCompose(importer, catalog, requiredExports.Take(i)));
            }
            for (int i = 0; i < optionalExports.Count(); i++)
            {
                // Try importing when all of the required and not all of the optional exports are available -> success
                TryCompose(importer, catalog, requiredExports.Concat(optionalExports.Take(i)));
            }
            // Finally try importing when all of the required and optional exports are available -> success
            TryCompose(importer, catalog, requiredExports.Concat(optionalExports));
        }

        private static void TryCompose(object importer, TypeCatalog catalog, IEnumerable<Export> exports)
        {
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            foreach (Export item in exports)
            {
                batch.AddExport(item);
            }
            using (CompositionContainer container = new CompositionContainer(catalog))
            {
                container.Compose(batch);
            }
        }
    }
}
