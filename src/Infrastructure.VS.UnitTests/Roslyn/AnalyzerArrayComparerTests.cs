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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Roslyn;

[TestClass]
public class AnalyzerArrayComparerTests
{
    [TestMethod]
    public void Equals_BothNulls_True()
    {
        AnalyzerArrayComparer.Instance.Equals(null, null).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_OneNull_False()
    {
        var analyzerArray = ImmutableArray.Create<AnalyzerFileReference>();
        AnalyzerArrayComparer.Instance.Equals(null, analyzerArray).Should().BeFalse();
        AnalyzerArrayComparer.Instance.Equals(analyzerArray, null).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_EmptyArrays_True()
    {
        var analyzerArray1 = ImmutableArray.Create<AnalyzerFileReference>();
        var analyzerArray2 = ImmutableArray.Create<AnalyzerFileReference>();
        AnalyzerArrayComparer.Instance.Equals(analyzerArray1, analyzerArray2).Should().BeTrue();
        AnalyzerArrayComparer.Instance.Equals(analyzerArray2, analyzerArray1).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_DifferentLengths_False()
    {
        var empty = ImmutableArray.Create<AnalyzerFileReference>();
        var single = ImmutableArray.Create(GetAnalyzerFileReference());
        var triple = ImmutableArray.Create(GetAnalyzerFileReference(), GetAnalyzerFileReference(), GetAnalyzerFileReference());
        AnalyzerArrayComparer.Instance.Equals(empty, single).Should().BeFalse();
        AnalyzerArrayComparer.Instance.Equals(single, empty).Should().BeFalse();
        AnalyzerArrayComparer.Instance.Equals(single, triple).Should().BeFalse();
        AnalyzerArrayComparer.Instance.Equals(triple, single).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_SameArray_True()
    {
        var array = ImmutableArray.Create(GetAnalyzerFileReference());
        AnalyzerArrayComparer.Instance.Equals(array, array).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_ArrayWithSameValuesInOrder_True()
    {
        var analyzerFileReference1 = GetAnalyzerFileReference();
        var analyzerFileReference2 = GetAnalyzerFileReference();
        var analyzerFileReference3 = GetAnalyzerFileReference();
        var array1 = ImmutableArray.Create(analyzerFileReference1, analyzerFileReference2, analyzerFileReference3);
        var array2 = ImmutableArray.Create(analyzerFileReference1, analyzerFileReference2, analyzerFileReference3);
        AnalyzerArrayComparer.Instance.Equals(array1, array2).Should().BeTrue();
        AnalyzerArrayComparer.Instance.Equals(array2, array1).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_ArrayWithSameValuesInDifferentOrder_False()
    {
        var analyzerFileReference1 = GetAnalyzerFileReference();
        var analyzerFileReference2 = GetAnalyzerFileReference();
        var analyzerFileReference3 = GetAnalyzerFileReference();
        var array1 = ImmutableArray.Create(analyzerFileReference1, analyzerFileReference3, analyzerFileReference2);
        var array2 = ImmutableArray.Create(analyzerFileReference1, analyzerFileReference2, analyzerFileReference3);
        AnalyzerArrayComparer.Instance.Equals(array1, array2).Should().BeFalse();
        AnalyzerArrayComparer.Instance.Equals(array2, array1).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_ArrayWithEquivalentValues_True()
    {
        var analyzerAssemblyLoader1 = Substitute.For<IAnalyzerAssemblyLoader>();
        var analyzerAssemblyLoader2 = Substitute.For<IAnalyzerAssemblyLoader>();
        var analyzerFileReference11 = GetAnalyzerFileReference(@"C:\analyzer1", analyzerAssemblyLoader1);
        var analyzerFileReference12 = GetAnalyzerFileReference(@"C:\analyzer1", analyzerAssemblyLoader1);
        var analyzerFileReference21 = GetAnalyzerFileReference(@"C:\analyzer2", analyzerAssemblyLoader2);
        var analyzerFileReference22 = GetAnalyzerFileReference(@"C:\analyzer2", analyzerAssemblyLoader2);
        var array1 = ImmutableArray.Create(analyzerFileReference11, analyzerFileReference21);
        var array2 = ImmutableArray.Create(analyzerFileReference12, analyzerFileReference22);
        AnalyzerArrayComparer.Instance.Equals(array1, array2).Should().BeTrue();
        AnalyzerArrayComparer.Instance.Equals(array2, array1).Should().BeTrue();
    }

    [TestMethod]
    public void GetHashCode_DelegatesToObject()
    {
        ImmutableArray<AnalyzerFileReference>? nullArray = null;
        AnalyzerArrayComparer.Instance.GetHashCode(nullArray).Should().Be(nullArray.GetHashCode());
        var analyzerFileReferences = ImmutableArray.Create<AnalyzerFileReference>();
        AnalyzerArrayComparer.Instance.GetHashCode(analyzerFileReferences).Should().Be(analyzerFileReferences.GetHashCode());
    }

    private AnalyzerFileReference GetAnalyzerFileReference(string filePath = @"C:\analyzer", IAnalyzerAssemblyLoader analyzerAssemblyLoader = null)
    {
        return new AnalyzerFileReference(filePath, analyzerAssemblyLoader ?? Substitute.For<IAnalyzerAssemblyLoader>());
    }
}
