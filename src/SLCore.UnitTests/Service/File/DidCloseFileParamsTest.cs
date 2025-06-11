using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.File;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.File;

[TestClass]
public class DidCloseFileParamsTest
{
    [TestMethod]
    public void Serialize_AsExpected()
    {
        var testSubject = new DidCloseFileParams("some ConfigScope", new FileUri("file:///c:/Users/test/project1/Baz.cs"));

        const string expectedString = """
                                      {
                                        "configurationScopeId": "some ConfigScope",
                                        "fileUri": "file:///c:/Users/test/project1/Baz.cs"
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }
}
