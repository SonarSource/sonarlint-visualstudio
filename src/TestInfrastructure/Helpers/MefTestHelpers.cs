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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public static class MefTestHelpers
    {
        public static Export CreateExport<T>() where T: class
        {
            return CreateExport<T>(Mock.Of<T>());
        }

        public static Export CreateExport<T>(object exportInstance, string contractName = null)
        {
            // Add the required export ID so that MEF knows which contract it exports.
            var metadata = new Dictionary<string, object>()
            {
                {  "ExportTypeIdentity", AttributedModelServices.GetTypeIdentity(typeof(T)) }
            };

            contractName ??= AttributedModelServices.GetContractName(typeof(T));
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
            params Export[] requiredExports)
            where TImportType : class
            where TTypeToCheck : class
        {
            SingleObjectImporter<TImportType> importer = new SingleObjectImporter<TImportType>();

            CheckTypeCanBeImported<TTypeToCheck, TImportType>(importer, requiredExports);

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
        /// <param name="requiredExports">Any exports required by TTypeToCheck. Optional.</param>
        private static void CheckTypeCanBeImported<TTypeToCheck, TImportType>(
            object importer,
            IEnumerable<Export> requiredExports)
            where TImportType : class
            where TTypeToCheck : class
        {
            if (importer == null)
            {
                throw new ArgumentNullException(nameof(importer));
            }

            if (requiredExports == null)
            {
                requiredExports = Enumerable.Empty<Export>();
            }

            // Create a type catalog that only contains the type to be checked to
            // make sure we don't pick up any other types
            TypeCatalog catalog = new TypeCatalog(typeof(TTypeToCheck));

            for (int i = 0; i < requiredExports.Count(); i++)
            {
                // Try importing when not all of the required exports are available -> exception
                Exceptions.Expect<Exception>(
                    null,
                    () => TryCompose(importer, catalog, requiredExports.Take(i)),
                    checkDerived: true);
            }

            // Finally try importing when all of the required exports are available -> success
            TryCompose(importer, catalog, requiredExports);
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
