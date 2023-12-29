/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Protocol;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Protocol;

[TestClass]
public class EitherJsonConverterTests
{
    [TestMethod]
    public void SimpleTest()
    {
        var respLeft = new SomeResponse { dto = Either<V1Dto, V2Dto>.CreateLeft(new V1Dto()) };
        var respRight = new SomeResponse { dto = Either<V1Dto, V2Dto>.CreateRight(new V2Dto()) };

        JsonConvert.DeserializeObject<SomeResponse>(JsonConvert.SerializeObject(respLeft)).Should().BeEquivalentTo(respLeft);
        JsonConvert.DeserializeObject<SomeResponse>(JsonConvert.SerializeObject(respRight)).Should().BeEquivalentTo(respRight);
    }
    
    // todo add more tests
    
    public class SomeResponse
    {
        public string str = "aaaaa";
        [JsonConverter(typeof(EitherJsonConverter<V1Dto, V2Dto>))]
        public Either<V1Dto, V2Dto> dto { get; set; }
    }
    
    public class V1Dto
    {
        public object ACommon { get; set; } = new { lala = 10 };
        public object ACommon2 = new { lala = 10 };
        public object V1Obj { get; set; } = new { dada = 20 };
        public object V1Str = "strstrstr";
        public object XCommon { get; set; } = new { lala = 10 };
    }
    
    public class V2Dto
    {
        public object ACommon { get; set; } = new { lala = 20 };
        public object ACommon2 = new { lala = 20 };
        public object V2Obj { get; set; } = new { dada = 40 };
        public object V2Str = "strstrstr2";
        public object XCommon { get; set; } = new { lala = 20 };
    }
}
