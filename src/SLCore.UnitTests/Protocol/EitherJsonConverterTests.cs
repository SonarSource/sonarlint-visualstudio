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
using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Protocol;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Protocol;

[TestClass]
public class EitherJsonConverterTests
{
    [DataTestMethod]
    [DataRow(typeof(object), false)]
    [DataRow(typeof(Either<ConflictingObject.ConflictingLeft, ConflictingObject.ConflictingRight>), false)]
    [DataRow(typeof(Either<SimpleObject.LeftOption, ConflictingObject.ConflictingRight>), false)]
    [DataRow(typeof(Either<SimpleObject.LeftOption, SimpleObject.RightOption>), true)]
    public void CanConvert_TypeHasToMatch(Type typeToCheck, bool isSupported)
    {
        var testSubject = new EitherJsonConverter<SimpleObject.LeftOption, SimpleObject.RightOption>();

        testSubject.CanConvert(typeToCheck).Should().Be(isSupported);
    }
    
    [TestMethod]
    public void SerializeObject_SerializesEitherAsSingleObject()
    {
        var left = new SimpleObject
        {
            Property = Either<SimpleObject.LeftOption, SimpleObject.RightOption>.CreateLeft(
                new SimpleObject.LeftOption
                    { Left = "lll" })
        };
        var right = new SimpleObject
        {
            Property = Either<SimpleObject.LeftOption, SimpleObject.RightOption>.CreateRight(
                new SimpleObject.RightOption
                    { Right = 10 })
        };

        JsonConvert.SerializeObject(left).Should().BeEquivalentTo("""{"Property":{"Left":"lll"}}""");
        JsonConvert.SerializeObject(right).Should().BeEquivalentTo("""{"Property":{"Right":10}}""");
    }

    [TestMethod]
    public void DeserializeObject_PrimitiveNotAnObject_Throws()
    {
        var str = """
                  {
                    "Property" : "ThisIsExpectedToBeAnObjectButItIsAString"
                  }  
                  """;

        Action act = () => JsonConvert.DeserializeObject<SimpleObject>(str);

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage("Expected Object, found String");
    }

    [TestMethod]
    public void DeserializeObject_CollectionNotAnObject_Throws()
    {
        var str = """
                  {
                    "Property" : [1, 2, 3]
                  }  
                  """;

        Action act = () => JsonConvert.DeserializeObject<SimpleObject>(str);

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage("Expected Object, found Array");
    }

    [TestMethod]
    public void DeserializeObject_UnableToMatch_Throws()
    {
        var str = """
                  {
                    "Property" :
                    {
                      "unknown" : "value"
                    }
                  }
                  """;

        Action act = () => JsonConvert.DeserializeObject<SimpleObject>(str);

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage("Unable to make a definitive choice between Either options");
    }

    [TestMethod]
    public void DeserializeObject_UnresolvableTypes_Throws()
    {
        var str = """
                  {
                    "Property" :
                    {
                      "SameProperty" : "value"
                    }
                  }
                  """;


        Action act = () => JsonConvert.DeserializeObject<ConflictingObject>(str);

        act
            .Should()
            .ThrowExactly<JsonException>()
            .WithInnerExceptionExactly<ArgumentException>()
            .WithMessage(
                "Types SonarLint.VisualStudio.SLCore.UnitTests.Protocol.EitherJsonConverterTests+ConflictingObject+ConflictingLeft and SonarLint.VisualStudio.SLCore.UnitTests.Protocol.EitherJsonConverterTests+ConflictingObject+ConflictingRight have equivalent sets of properties and fields");
    }

    [TestMethod]
    public void SerializeDeserializeObject_ComplexObjects_DeserializesToCorrectEitherVariant()
    {
        var left = new ComplexObject
        {
            dto = Either<ComplexObject.LeftOption, ComplexObject.RightOption>.CreateLeft(new ComplexObject.LeftOption())
        };
        var right = new ComplexObject
        {
            dto = Either<ComplexObject.LeftOption, ComplexObject.RightOption>.CreateRight(
                new ComplexObject.RightOption())
        };

        JsonConvert.DeserializeObject<ComplexObject>(JsonConvert.SerializeObject(left)).Should().BeEquivalentTo(left);
        JsonConvert.DeserializeObject<ComplexObject>(JsonConvert.SerializeObject(right)).Should().BeEquivalentTo(right);
    }

    public class SimpleObject
    {
        [JsonConverter(typeof(EitherJsonConverter<LeftOption, RightOption>))]
        public Either<LeftOption, RightOption> Property { get; set; }

        public class LeftOption
        {
            public string Left { get; set; }
        }

        public class RightOption
        {
            public int Right;
        }
    }

    public class ConflictingObject
    {
        [JsonConverter(typeof(EitherJsonConverter<ConflictingLeft, ConflictingRight>))]
        public Either<ConflictingLeft, ConflictingRight> Property { get; set; }

        public class ConflictingLeft
        {
            public string SameProperty { get; set; }
        }

        public class ConflictingRight
        {
            public string SameProperty { get; set; }
        }
    }


    public class ComplexObject
    {
        public string str = "aaaaa";

        [JsonConverter(typeof(EitherJsonConverter<LeftOption, RightOption>))]
        public Either<LeftOption, RightOption> dto { get; set; }

        public class LeftOption
        {
            public object ACommon { get; set; } = new { lala = 10 };
            public object ACommon2 = new { lala = 10 };
            public object V1Obj { get; set; } = new { dada = 20 };
            public object V1Str = "strstrstr";
            public object XCommon { get; set; } = new { lala = 10 };
        }

        public class RightOption
        {
            public object ACommon { get; set; } = new { lala = 20 };
            public object ACommon2 = new { lala = 20 };
            public object V2Obj { get; set; } = new { dada = 40 };
            public object V2Str = "strstrstr2";
            public object XCommon { get; set; } = new { lala = 20 };
        }
    }
}
