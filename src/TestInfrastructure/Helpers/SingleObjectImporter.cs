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

using System.ComponentModel.Composition;
using FluentAssertions;

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

        public void AssertImportIsNotNull()
        {
            this.Import.Should().NotBeNull();
        }

        public void AssertImportIsInstanceOf<TExpected>()
        {
            this.AssertImportIsNotNull();
            this.Import.Should().BeAssignableTo<TExpected>();
        }

        #endregion Assertions
    }
}