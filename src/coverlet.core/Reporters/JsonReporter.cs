using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Coverlet.Core.Reporters
{
    public class JsonReporter : IReporter
    {
        public string Format => "json";

        public string Extension => "json";

        public string Report(CoverageResult result)
        {
            return JsonConvert.SerializeObject(result.Modules, Formatting.Indented, new LinesConverter(), new BranchesConverter());
        }

        public CoverageResult Read(string data)
        {
            return new CoverageResult
            {
                Identifier = Guid.NewGuid().ToString(),
                Modules = JsonConvert.DeserializeObject<Modules>(data, new LinesConverter(), new BranchesConverter())
            };
        }

        private class BranchesConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Branches);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var array = JArray.Load(reader);
                var branches = new Branches();

                foreach (var item in array)
                {
                    var obj = (JObject)item;

                    var key = (
                        (int)obj["Key"]["Number"],
                        (int)obj["Key"]["Offset"],
                        (int)obj["Key"]["EndOffset"],
                        (int)obj["Key"]["Path"],
                        (uint)obj["Key"]["Ordinal"]);
                    var value = new HitInfo { Hits = (int)obj["Value"]["Hits"] };

                    branches.Add(key, value);
                }

                return branches;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var branches = (Branches) value;
                var array = new JArray();

                foreach (var kv in branches)
                {
                    dynamic obj = new JObject();

                    obj.Key = new JObject();
                    obj.Key.Number = kv.Key.Number;
                    obj.Key.Offset = kv.Key.Offset;
                    obj.Key.EndOffset = kv.Key.EndOffset;
                    obj.Key.Path = kv.Key.Path;
                    obj.Key.Ordinal = kv.Key.Ordinal;

                    obj.Value = new JObject();
                    obj.Value.Hits = kv.Value.Hits;

                    array.Add(obj);
                }

                array.WriteTo(writer);
            }
        }

        private class LinesConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Lines);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var array = JArray.Load(reader);
                var lines = new Lines();

                foreach (var item in array)
                {
                    var obj = (JObject) item;

                    var key = (int)obj["Key"]["Line"];
                    var value = new HitInfo { Hits = (int)obj["Value"]["Hits"] };

                    lines.Add(key, value);
                }

                return lines;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var lines = (Lines)value;
                var array = new JArray();

                foreach (var kv in lines)
                {
                    dynamic obj = new JObject();

                    obj.Key = new JObject();
                    obj.Key.Line = kv.Key;

                    obj.Value = new JObject();
                    obj.Value.Hits = kv.Value.Hits;

                    array.Add(obj);
                }

                array.WriteTo(writer);
            }
        }
    }
}