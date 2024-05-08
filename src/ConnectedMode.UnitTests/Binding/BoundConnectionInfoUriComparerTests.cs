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

using SonarLint.VisualStudio.ConnectedMode.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class BoundConnectionInfoUriComparerTests
    {
        BoundConnectionInfoUriComparer comparer;

        [TestInitialize]
        public void Init()
        {
            comparer = new BoundConnectionInfoUriComparer();
        }

        [TestMethod]
        public void Equals_BothNull_ReturnsTrue()
        {
            BoundConnectionInfo bindingInfo1 = null;
            BoundConnectionInfo bindingInfo2 = null;

            comparer.Equals(bindingInfo1, bindingInfo2).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_OneNull_ReturnsFalse()
        {
            BoundConnectionInfo bindingInfo1 = new BoundConnectionInfo();
            BoundConnectionInfo bindingInfo2 = null;

            comparer.Equals(bindingInfo1, bindingInfo2).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_SameUri_ReturnsTrue()
        {
            BoundConnectionInfo bindingInfo1 = new BoundConnectionInfo { ServerUri = new Uri("https://www.google.com") };
            BoundConnectionInfo bindingInfo2 = new BoundConnectionInfo { ServerUri = new Uri("https://www.google.com") };

            comparer.Equals(bindingInfo1, bindingInfo2).Should().BeTrue();
        }

        [TestMethod]
        public void GetHashCode_SameAsUri()
        {
            BoundConnectionInfo bindingInfo = new BoundConnectionInfo { ServerUri = new Uri("https://www.google.com") };

            comparer.GetHashCode(bindingInfo).Should().Be(bindingInfo.ServerUri.GetHashCode());
        }
    }
}
