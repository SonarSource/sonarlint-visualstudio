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

using System.IO;
using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Models;

[TestClass]
public class FileUriTests
{
    [TestMethod]
    public void Ctor_FromFilePath_FormsCorrectUri()
    {
        var fromUri = new FileUri("file:///c:/file/path");
        var fromFile = new FileUri(@"C:\file\path");

        fromFile.Should().Be(fromUri);
    }
    
    [TestMethod]
    public void Ctor_FromUncPath_FormsCorrectUri()
    {
        var fromUri = new FileUri("file://server/file/path");
        var fromFile = new FileUri(@"\\server\file\path");

        fromFile.Should().Be(fromUri);
    }
    
    [TestMethod]
    public void Ctor_FromFilePathWithSpaces_FormsCorrectUri()
    {
        var reference = new FileUri("file:///c:/file/path%20with%20spaces");
        var testSubject = new FileUri(@"C:\file\path with spaces");

        testSubject.Should().Be(reference);
    }    
    
    [TestMethod]
    public void Ctor_FromFilePathBackticks_FormsCorrectUri()
    {
        var reference = new FileUri("file:///C:/filewithbacktick%601");
        var testSubject = new FileUri(@"C:\filewithbacktick`1");

        testSubject.Should().Be(reference);
    }
    
    [DataTestMethod]
    [DataRow(@"C:\file1.cs", @"C:\file1.cs", true)]
    [DataRow(@"C:\file1.cs", @"C:\file2.cs", false)]
    [DataRow(@"C:\my folder\file1.cs", @"C:\my folder\file1.cs", true)]
    [DataRow(@"C:\my folder\file1.cs", @"C:\my folder\file2.cs", false)]
    [DataRow(@"C:\my folder\file1.cs", @"C:\my folder\file2.cs", false)]
    [DataRow(@"\\server\my folder\file1.cs", @"\\server\my folder\file1.cs", true)]
    [DataRow(@"\\server\my folder\file1.cs", @"C:\my folder\file1.cs", false)]
    public void EqualsAndHashCode_BasedOnFilePath(string filePath1, string filePath2, bool isEqual)
    {
        var uri1 = new FileUri(filePath1);
        var uri2 = new FileUri(filePath2);

        uri1.Equals(uri2).Should().Be(isEqual);
        uri2.Equals(uri1).Should().Be(isEqual);

        (uri1 == uri2).Should().Be(isEqual);
        (uri1 != uri2).Should().Be(!isEqual);
        (uri2 == uri1).Should().Be(isEqual);
        (uri2 != uri1).Should().Be(!isEqual);

        if (isEqual)
        {
            uri1.GetHashCode().Should().Be(uri2.GetHashCode());
        }
    }

    [TestMethod]
    public void ToString_ReturnsFileSchemaUri()
    {
        new FileUri(@"C:\file").ToString().Should().StartWith(Uri.UriSchemeFile);
    }
    
    [TestMethod]
    public void ToString_ReversesSlashes()
    {
        new FileUri(@"C:\file\path\on\disk").ToString().Should().NotContain(Path.DirectorySeparatorChar.ToString());
    }
    
    [TestMethod]
    public void ToString_PercentEncodesSpaces()
    {
        new FileUri(@"C:\file with  4 spaces").ToString().Should().Be("file:///C:/file%20with%20%204%20spaces");
    }
    
    [TestMethod]
    public void ToString_PercentEncodesBackticks()
    {
        new FileUri(@"C:\filewithbacktick`1").ToString().Should().Be("file:///C:/filewithbacktick%601");
    }

    [TestMethod]
    [DataRow("[", "%5B")]
    [DataRow("]", "%5D")]
    [DataRow("#", "%2523")]
    [DataRow("@", "%40")]
    public void ToString_PercentEncodesReservedRfc3986Characters(string reservedChar, string expectedEncoding)
    {
        var actualString = @$"C:\filewithRfc3986ReservedChar{reservedChar}.cs";
        var expectedString = @$"file:///C:/filewithRfc3986ReservedChar{expectedEncoding}.cs";

        new FileUri(actualString).ToString().Should().Be(expectedString);
    }

    [TestMethod]
    public void LocalPath_ReturnsCorrectPath()
    {
        var filePath = @"C:\file\path\with some spaces\and with some backticks`1`2`3";
        new FileUri(filePath).LocalPath.Should().Be(filePath);
    }

    [TestMethod]
    public void SerializeDeserializeToEqualObject()
    {
        var fileUri = new FileUri(@"C:\file with  4 spaces and a back`tick");

        var serializeObject = JsonConvert.SerializeObject(fileUri);
        var deserialized = JsonConvert.DeserializeObject<FileUri>(serializeObject);

        deserialized.Should().Be(fileUri);
    }
    
    [TestMethod]
    public void Serialize_UsesToString()
    {
        var fileUri = new FileUri(@"C:\file with  4 spaces and a back`tick");

        var serializeObject = JsonConvert.SerializeObject(fileUri);

        serializeObject.Should().Be(@"""file:///C:/file%20with%20%204%20spaces%20and%20a%20back%60tick""");
    }
    
    [TestMethod]
    public void Deserialize_ProducesCorrectUri()
    {
        var serialized = @"""file:///C:/file%20with%20%204%20spaces%20and%20a%20back%60tick""";

        var fileUri = JsonConvert.DeserializeObject<FileUri>(serialized);

        fileUri.ToString().Should().Be("file:///C:/file%20with%20%204%20spaces%20and%20a%20back%60tick");
        fileUri.LocalPath.Should().Be(@"C:\file with  4 spaces and a back`tick");
    }

    [TestMethod]
    public void Deserialize_ReservedRfc3986Characters_ProducesCorrectUri()
    {
        var serialized = @"""file:///C:/file%5B%5Dand%2523and%40""";

        var fileUri = JsonConvert.DeserializeObject<FileUri>(serialized);

        fileUri.ToString().Should().Be("file:///C:/file%5B%5Dand%2523and%40");
        fileUri.LocalPath.Should().Be(@"C:\file[]and#and@");
    }
}
