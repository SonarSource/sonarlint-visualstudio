/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;

namespace SonarLint.VisualStudio.Integration.UnitTests.ErrorList
{
    [TestClass]
    public class RedefinedErrorListConstantsTests
    {
#if VS2022
        [TestMethod]
        public void CheckRedefinedSuppressionStateColumnName()
        {
            // Sanity check that our definition of the column name matches the VS version.

            // We can only run this test for the VS2022 build - the whole point in having our own constant
            // is because it doesn't exist in VS2019.3 (v16.3).
            RedefinedErrorListConstants.SuppressionStateColumnName.Should().Be(StandardTableColumnDefinitions.SuppressionState);
        }
#endif
    }
}
