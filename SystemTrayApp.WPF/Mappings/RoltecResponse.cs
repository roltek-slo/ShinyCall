namespace ShinyCall.Mappings
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;



  
    public partial class RoltecResponse
    {
        [JsonProperty("jsonapi")]
        public Jsonapi Jsonapi { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }
    }

    public partial class Data
    {
        [JsonProperty("attributes")]
        public Attributes Attributes { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }
    public partial class Attributes
    {
        [JsonProperty("popupHeight")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long PopupHeight { get; set; }

        [JsonProperty("clientName")]
        public string ClientName { get; set; }

        [JsonProperty("popupDuration")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long PopupDuration { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("popupWidth")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long PopupWidth { get; set; }
    }

    public partial class Jsonapi
    {
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
