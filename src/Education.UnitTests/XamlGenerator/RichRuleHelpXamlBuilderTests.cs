using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Xml;
using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests.XamlGenerator
{
    [TestClass]
    public class RichRuleHelpXamlBuilderTests
    {
        [TestMethod]
        public void MefCtor_CheckExports()
        {
            MefTestHelpers.CheckTypeCanBeImported<RichRuleHelpXamlBuilder, IRichRuleHelpXamlBuilder>(
                MefTestHelpers.CreateExport<IRichRuleDescriptionProvider>(),
                MefTestHelpers.CreateExport<IRuleHelpXamlTranslatorFactory>(),
                MefTestHelpers.CreateExport<IXamlGeneratorHelperFactory>(),
                MefTestHelpers.CreateExport<IXamlWriterFactory>());
        }

        [TestMethod]
        public void Create_GeneratesCorrectStructure()
        {
            const string placeholderContent = "PLACEHOLDER";
            const string selectedIssueContext = "selectedcontext1";
            var callSequence = new MockSequence();
            var richRuleDescriptionProviderMock = new Mock<IRichRuleDescriptionProvider>(MockBehavior.Strict);
            var richRuleDescriptionMock = new Mock<IRichRuleDescription>(MockBehavior.Strict);
            var visualizationNodeMock = new Mock<IAbstractVisualizationTreeNode>(MockBehavior.Strict);
            var ruleInfoTranslatorFactoryMock = new Mock<IRuleHelpXamlTranslatorFactory>(MockBehavior.Strict);
            var ruleInfoTranslatorMock = new Mock<IRuleHelpXamlTranslator>(MockBehavior.Strict);
            var xamlGeneratorHelperFactoryMock = new Mock<IXamlGeneratorHelperFactory>(MockBehavior.Strict);
            var xamlGeneratorHelperMock = new Mock<IXamlGeneratorHelper>(MockBehavior.Strict);
            var xamlWriterFactoryMock = new Mock<IXamlWriterFactory>(MockBehavior.Strict);
            var ruleInfo = Mock.Of<IRuleInfo>();
            XmlWriter writer = null;
            xamlWriterFactoryMock
                .InSequence(callSequence)
                .Setup(x => x.Create(It.IsAny<StringBuilder>()))
                .Returns((StringBuilder sb) =>
                {
                    writer = new XamlWriterFactory().Create(sb);
                    return writer;
                });
            xamlGeneratorHelperFactoryMock
                .InSequence(callSequence)
                .Setup(x => x.Create(It.IsAny<XmlWriter>()))
                .Returns(xamlGeneratorHelperMock.Object);
            xamlGeneratorHelperMock
                .InSequence(callSequence)
                .Setup(x => x.WriteDocumentHeader(ruleInfo))
                .Callback(() => { writer.WriteStartElement("FlowDocument", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"); });
            richRuleDescriptionProviderMock
                .InSequence(callSequence)
                .Setup(provider => provider.GetRichRuleDescriptionModel(ruleInfo))
                .Returns(richRuleDescriptionMock.Object);
            ruleInfoTranslatorFactoryMock
                .InSequence(callSequence)
                .Setup(x => x.Create())
                .Returns(ruleInfoTranslatorMock.Object);
            richRuleDescriptionMock
                .InSequence(callSequence)
                .Setup(richRule => 
                    richRule.ProduceVisualNode(It.Is<VisualizationParameters>(p =>
                        p.HtmlToXamlTranslator == ruleInfoTranslatorMock.Object && p.RelevantContext == selectedIssueContext)))
                .Returns(visualizationNodeMock.Object);
            visualizationNodeMock
                .InSequence(callSequence)
                .Setup(vis => vis.ProduceXaml(It.IsAny<XmlWriter>()))
                .Callback(() =>
                {
                    writer.WriteStartElement("Paragraph");
                    writer.WriteString(placeholderContent);
                    writer.WriteEndElement();
                });
            xamlGeneratorHelperMock
                .InSequence(callSequence)
                .Setup(x => x.EndDocument())
                .Callback(() =>
                {
                    writer.WriteFullEndElement();
                    writer.Close();
                });

            var testSubject = new RichRuleHelpXamlBuilder(richRuleDescriptionProviderMock.Object,
                ruleInfoTranslatorFactoryMock.Object,
                xamlGeneratorHelperFactoryMock.Object,
                xamlWriterFactoryMock.Object);

            var flowDocument = testSubject.Create(ruleInfo, selectedIssueContext);

            flowDocument
                .Blocks
                .Single()
                .Should()
                .BeOfType<Paragraph>()
                .Which
                .Inlines
                .Single()
                .ContentStart
                .GetTextInRun(LogicalDirection.Forward)
                .Should()
                .BeEquivalentTo(placeholderContent);
        }
    }
}
