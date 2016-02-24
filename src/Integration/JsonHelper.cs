//-----------------------------------------------------------------------
// <copyright file="JsonHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace SonarLint.VisualStudio.Integration
{
    internal static class JsonHelper
    {
        public static T Deserialize<T>(string json)
        {
            using (var reader = new StringReader(json))
            {
                using (var textReader = new JsonTextReader(reader))
                {
                    return JsonSerializer.CreateDefault().Deserialize<T>(textReader);
                }
            }
        }

        public static string Serialize(object item)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                using (var textWriter = new JsonTextWriter(writer))
                {
                    var serializer = JsonSerializer.CreateDefault();
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(textWriter, item);
                }
            }

            return sb.ToString();
        }
    }
}
