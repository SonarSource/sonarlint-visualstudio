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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIdeHotspots;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIdeHotspots
{
    [TestClass]
    public class IssueOpenInIdeConverterTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<HotspotOpenInIdeConverter, IHotspotOpenInIdeConverter>(
                MefTestHelpers.CreateExport<IHotspotDetailsDtoToHotspotConverter>(),
                MefTestHelpers.CreateExport<IAnalysisIssueVisualizationConverter>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<HotspotOpenInIdeConverter>();
        }

        [DataTestMethod]
        [DataRow(typeof(NullReferenceException), false)]
        [DataRow(typeof(ArgumentException), false)]
        [DataRow(typeof(DivideByZeroException), true)]
        public void TryConvertIssue_DtoConverterThrows_ExceptionHandledDependingOnSeverity(Type exceptionType,
            bool isCriticalException)
        {
            var exception = Activator.CreateInstance(exceptionType) as Exception;
            var testSubject = CreateTestSubject(out var dtoToIssueConverter, out _, out var logger);
            dtoToIssueConverter.Convert(default, default).ThrowsForAnyArgs(exception);

            Func<bool> act = () => testSubject.TryConvert(default, default, out _);

            if (isCriticalException)
            {
                act.Should().Throw<Exception>();
            }
            else
            {
                act.Should().NotThrow();
                act().Should().BeFalse();
                logger.AssertPartialOutputStringExists("[Open in IDE] Unable to convert hotspot data:");
            }
        }

        [DataTestMethod]
        [DataRow(typeof(NullReferenceException), false)]
        [DataRow(typeof(ArgumentException), false)]
        [DataRow(typeof(DivideByZeroException), true)]
        public void TryConvertIssue_VisualisationConverterThrows_ExceptionHandledDependingOnSeverity(Type exceptionType,
            bool isCriticalException)
        {
            var exception = Activator.CreateInstance(exceptionType) as Exception;
            var testSubject =
                CreateTestSubject(out var dtoToIssueConverter, out var visualizationConverter, out var logger);
            var (rootPath, dto, analysisIssueBase) = SetUpDtoConverter(dtoToIssueConverter);
            visualizationConverter.Convert(analysisIssueBase).Throws(exception);

            Func<bool> act = () => testSubject.TryConvert(dto, rootPath, out _);

            if (isCriticalException)
            {
                act.Should().Throw<Exception>();
            }
            else
            {
                act.Should().NotThrow();
                act().Should().BeFalse();
                logger.AssertPartialOutputStringExists("[Open in IDE] Unable to convert hotspot data:");
            }
        }

        [TestMethod]
        public void TryConvertIssue_VisualisationConverterThrows_ExceptionHandledDependingOnSeverity()
        {
            var testSubject =
                CreateTestSubject(out var dtoToIssueConverter, out var visualizationConverter, out var logger);
            var (rootPath, dto, analysisIssueBase) = SetUpDtoConverter(dtoToIssueConverter);
            var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
            visualizationConverter.Convert(analysisIssueBase).Returns(analysisIssueVisualization);

            testSubject.TryConvert(dto, rootPath, out var visualization).Should().BeTrue();

            visualization.Should().BeSameAs(analysisIssueVisualization);
        }

        private static (string rootPath, HotspotDetailsDto dto, IAnalysisIssueBase analysisIssueBase) SetUpDtoConverter(
            IHotspotDetailsDtoToHotspotConverter dtoToHotspotConverter)
        {
            const string rootPath = "rootpath";
            var dto = new HotspotDetailsDto(default, default, default, default, default, default, default, default, default);
            var analysisIssueBase = Substitute.For<IHotspot>();
            dtoToHotspotConverter.Convert(dto, rootPath).Returns(analysisIssueBase);
            return (rootPath, dto, analysisIssueBase);
        }

        private HotspotOpenInIdeConverter CreateTestSubject(out IHotspotDetailsDtoToHotspotConverter dtoToHotspotConverter,
            out IAnalysisIssueVisualizationConverter hotspotToVisualizationConverter, out TestLogger logger)
        {
            dtoToHotspotConverter = Substitute.For<IHotspotDetailsDtoToHotspotConverter>();
            hotspotToVisualizationConverter = Substitute.For<IAnalysisIssueVisualizationConverter>();
            logger = new();
            return new(dtoToHotspotConverter, hotspotToVisualizationConverter, logger);
        }
    }
}
