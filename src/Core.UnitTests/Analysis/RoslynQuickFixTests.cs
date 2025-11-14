/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Core.UnitTests.Analysis;

[TestClass]
public class RoslynQuickFixTests
{
    public static object[][] IdAndStorageValueTestData =>
    [
        [new Guid("64146AA8-AE93-4ABE-BD81-FCBE27FC4D3E"), "|<SQVS_ROSLYN_QUICKFIX>|64146aa8-ae93-4abe-bd81-fcbe27fc4d3e"],
        [new Guid("6125AA53-4A10-4A07-84CD-9C4C758B26A3"), "|<SQVS_ROSLYN_QUICKFIX>|6125aa53-4a10-4a07-84cd-9c4c758b26a3"],
        [new Guid("145A5840-0E94-41D1-8431-4BFA07F952C6"), "|<SQVS_ROSLYN_QUICKFIX>|145a5840-0e94-41d1-8431-4bfa07f952c6"]
    ];

    [TestMethod]
    [DynamicData(nameof(IdAndStorageValueTestData))]
    public void StorageValue_ContainsCorrectPrefix(Guid id, string expectedStorageValue)
    {
        var testSubject = new RoslynQuickFix(id);

        testSubject.Id.Should().Be(id);
        testSubject.GetStorageValue().Should().Be(expectedStorageValue);
    }

    [TestMethod]
    [DynamicData(nameof(IdAndStorageValueTestData))]
    public void TryParse_ValidInput_ReturnsTrue(Guid expectedId, string storageValue)
    {
        var result = RoslynQuickFix.TryParse(storageValue, out var quickFix);

        result.Should().BeTrue();
        quickFix.Should().NotBeNull();
        quickFix.Id.Should().Be(expectedId);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow("invalid")]
    [DataRow("|<SQVS_ROSLYN_QUICKFIX>|")]
    [DataRow("|<SQVS_ROSLYN_QUICKFIX>|not-a-guid")]
    [DataRow("|<WRONGPREFIX>|BCF7C738-EEF5-4F41-A5FF-3D8DDC00540B")]
    [DataRow("82D00A1A-3019-4E09-9895-BE42657DFB34")]
    public void TryParse_InvalidInput_ReturnsFalse(string message)
    {
        var result = RoslynQuickFix.TryParse(message, out _);

        result.Should().BeFalse();
    }

    [TestMethod]
    public void TryParse_RoundTrip_PreservesOriginalId()
    {
        var originalId = Guid.NewGuid();
        var originalQuickFix = new RoslynQuickFix(originalId);
        var storageValue = originalQuickFix.GetStorageValue();

        var success = RoslynQuickFix.TryParse(storageValue, out var parsedQuickFix);

        success.Should().BeTrue();
        parsedQuickFix.Id.Should().Be(originalId);
        parsedQuickFix.GetStorageValue().Should().Be(storageValue);
    }
}
