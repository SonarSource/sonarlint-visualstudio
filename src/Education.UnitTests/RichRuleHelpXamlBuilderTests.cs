using System.Linq;
using System.Text;
using System.Windows.Controls;
using FluentAssertions;
using System.Windows.Documents;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.Rules;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Education.UnitTests
{
    [TestClass]
    public class RichRuleHelpXamlBuilderTests
    {
        [TestMethod]
        public void MefCtor_CheckExports()
        {
            MefTestHelpers.CheckTypeCanBeImported<RichRuleHelpXamlBuilder, IRichRuleHelpXamlBuilder>(
                MefTestHelpers.CreateExport<IRuleInfoTranslator>(),
                MefTestHelpers.CreateExport<IXamlGeneratorHelperFactory>(),
                MefTestHelpers.CreateExport<IStaticXamlStorage>(),
                MefTestHelpers.CreateExport<IXamlWriterFactory>());
        }

        [TestMethod]
        public void Create_GeneratesCorrectStructure()
        {
            var selectedIssueContext = "abrakadabra";
            var callSequence = new MockSequence();
            var ruleInfoTranslatorMock = new Mock<IRuleInfoTranslator>(MockBehavior.Strict);
            var xamlGeneratorHelperFactoryMock = new Mock<IXamlGeneratorHelperFactory>(MockBehavior.Strict);
            var xamlGeneratorHelperMock = new Mock<IXamlGeneratorHelper>(MockBehavior.Strict);
            var xamlWriterFactoryMock = new Mock<IXamlWriterFactory>(MockBehavior.Strict);
            var ruleInfo = Mock.Of<IRuleInfo>();
            XmlWriter writer = null;
            ruleInfoTranslatorMock
                .InSequence(callSequence)
                .Setup(x => x.GetRuleDescriptionSections(ruleInfo, selectedIssueContext))
                .Returns(Enumerable
                    .Range(0, 3)
                    .Select(x =>
                    {
                        var mock = new Mock<IRichRuleDescriptionSection>(MockBehavior.Strict);
                        mock
                            .SetupGet(y => y.Title)
                            .Returns(x.ToString());
                        mock
                            .Setup(y => y.GetVisualizationTreeNode(It.IsAny<IStaticXamlStorage>()))
                            .Returns(new ContentSection($"<Paragraph>{x}</Paragraph>"));
                        return mock.Object;
                    }));
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
            xamlGeneratorHelperMock
                .InSequence(callSequence)
                .Setup(x => x.EndDocument())
                .Callback(() =>
                {
                    writer.WriteFullEndElement();
                    writer.Close();
                });

            var testSubject = new RichRuleHelpXamlBuilder(ruleInfoTranslatorMock.Object, xamlGeneratorHelperFactoryMock.Object, Mock.Of<IStaticXamlStorage>(), xamlWriterFactoryMock.Object);

            var flowDocument = testSubject.Create(ruleInfo, selectedIssueContext);

            var blockUiContainer = flowDocument.Blocks.Single().Should().BeOfType<BlockUIContainer>().Subject;
            var tabControl = blockUiContainer.Child.Should().BeOfType<TabControl>().Subject;
            tabControl.SelectedIndex.Should().Be(0);
            tabControl.Items.Should().HaveCount(3);
            for (var index = 0; index < tabControl.Items.Count; index++)
            {
                ((TabItem)tabControl.Items[index])
                    .Content
                    .Should().BeOfType<FlowDocumentScrollViewer>()
                    .Which
                    .Document
                    .Blocks
                    .Single()
                    .Should().BeOfType<Paragraph>()
                    .Which
                    .Inlines
                    .Single()
                    .Should().BeOfType<Run>()
                    .Which
                    .Text.Should().BeEquivalentTo(index.ToString());
            }
        }
    }
}
