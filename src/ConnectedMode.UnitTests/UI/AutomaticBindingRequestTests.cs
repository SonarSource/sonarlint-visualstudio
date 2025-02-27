/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.ConnectedMode.UI;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI;

[TestClass]
public class AutomaticBindingRequestTests
{
    [TestMethod]
    public void Shared_Instance_NotNull()
    {
        AutomaticBindingRequest.Shared.Current.Should().NotBeNull();
        AutomaticBindingRequest.Shared.Current.TypeName.Should().Be(Resources.AutomaticBindingType_Shared);
    }

    public static object[][] AssistedBindingSubtypes =>
    [
        [true, Resources.AutomaticBindingType_Shared],
        [false, Resources.AutomaticBindingType_Suggested]
    ];
    [DynamicData(nameof(AssistedBindingSubtypes))]
    [DataTestMethod]
    public void Assisted_ReturnsTypeNameDependingOnParameter(bool isShared, string typeName)
    {
        var testSubject = new AutomaticBindingRequest.Assisted("con id", "proj id", isShared);

        testSubject.ServerConnectionId.Should().Be("con id");
        testSubject.ServerProjectKey.Should().Be("proj id");
        testSubject.TypeName.Should().Be(typeName);
        testSubject.IsShared.Should().Be(isShared);
    }
}
