using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration.Telemetry;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ShortIsoDateTimeOffsetConverterTests
    {
        [TestMethod]
        public void Convert_ForAllCultures_ReturnsTheExpectedValue()
        {
            // Arrange
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            var testSubject = new ShortIsoDateTimeOffsetConverter();
            var jsonWriterMock = new Mock<JsonWriter>();
            // add ticks to the date to be sure that zeros are not automatically stripped out
            var dateTimeOffset = new DateTimeOffset(2017, 12, 23, 8, 25, 35, 456, TimeSpan.FromHours(1)).AddTicks(123);
            var results = new List<Tuple<string, CultureInfo>>();
            jsonWriterMock.Setup(x => x.WriteValue(It.IsAny<string>()))
                .Callback<string>(s => results.Add(new Tuple<string, CultureInfo>(s, Thread.CurrentThread.CurrentCulture)));
            var cultureCount = 0;

            // Act
            try
            {
                var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                cultureCount = allCultures.Length;

                foreach (var culture in allCultures)
                {
                    Thread.CurrentThread.CurrentCulture = culture;
                    testSubject.WriteJson(jsonWriterMock.Object, dateTimeOffset, null);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }

            // This variable is used to display properly the culture and the value when there is an error
            var nonMatchingDates = results.Where(x => x.Item1 != "2017-12-23T08:25:35.456+01:00")
                .Select(x => $"{x.Item2.EnglishName} => {x.Item1}")
                .ToList();

            // Assert
            results.Should().HaveCount(cultureCount);
            nonMatchingDates.Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_AlwaysReturnsAFixedSizeString()
        {
            // Arrange
            var testSubject = new ShortIsoDateTimeOffsetConverter();
            var jsonWriterMock = new Mock<JsonWriter>();
            var resultLengths = new List<int>();
            jsonWriterMock.Setup(x => x.WriteValue(It.IsAny<string>()))
                .Callback<string>(s => resultLengths.Add(s.Length));

            // Act
            testSubject.WriteJson(jsonWriterMock.Object, new DateTimeOffset(), null);
            testSubject.WriteJson(jsonWriterMock.Object, new DateTimeOffset(DateTime.Now), null);
            testSubject.WriteJson(jsonWriterMock.Object, new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero), null);
            testSubject.WriteJson(jsonWriterMock.Object, new DateTimeOffset(1, TimeSpan.Zero), null);
            testSubject.WriteJson(jsonWriterMock.Object, new DateTimeOffset(1, 1, 1, 1, 1, 1, TimeSpan.Zero), null);
            testSubject.WriteJson(jsonWriterMock.Object, new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, TimeSpan.Zero), null);
            testSubject.WriteJson(jsonWriterMock.Object, new DateTimeOffset(1, 1, 1, 1, 1, 1, 1, new JapaneseCalendar(), TimeSpan.FromHours(1)), null);

            // Assert
            resultLengths.Should().HaveCount(7);
            resultLengths.Should().AllBeEquivalentTo(29);
        }
    }
}
