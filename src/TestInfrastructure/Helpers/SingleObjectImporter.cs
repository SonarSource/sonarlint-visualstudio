//-----------------------------------------------------------------------
// <copyright file="SingleObjectImporter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Generic class that MEF imports an arbitrary type.
    /// Used when testing that platforms extensions can be imported as expected.
    /// </summary>
    public class SingleObjectImporter<T> where T : class
    {
        [Import]
        public T Import { get; set; }

        #region Assertions

        public void AssertImportIsNull()
        {
            Assert.IsNull(this.Import, "Expecting the import to be null");
        }

        public void AssertImportIsNotNull()
        {
            Assert.IsNotNull(this.Import, "Expecting the import not to be null");
        }

        public void AssertExpectedImport(T expected)
        {
            this.AssertImportIsNotNull();
            Assert.AreSame(this.Import, expected, "An unexpected instance was imported");
        }

        public void AssertImportIsInstanceOf<TExpected>()
        {
            this.AssertImportIsNotNull();
            Assert.IsInstanceOfType(this.Import, typeof(TExpected), "Import is not of the expected type");
        }

        #endregion
    }
}
