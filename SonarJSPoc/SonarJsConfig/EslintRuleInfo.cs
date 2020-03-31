using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SonarJsConfig
{
    public class EslintRuleInfo
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
