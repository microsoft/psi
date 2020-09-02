// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Serialization
{
    using System;
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Contract resolver that mitigates issues when resolving MathNet.Spatial.Euclidean.Vector3D properties.
    /// </summary>
    public class Instant3DVisualizationObjectContractResolver : DefaultContractResolver
    {
        private Vector3dConverter vector3dConverter = new Vector3dConverter();

        /// <inheritdoc />
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            // MathNet.Spatial.Euclidean.Vector3D has a computed property (Orthogonal) that throws sometimes
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (property.PropertyType == typeof(MathNet.Spatial.Euclidean.Vector3D))
            {
                property.Converter = this.vector3dConverter;
            }

            return property;
        }

        private class Vector3dConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(MathNet.Spatial.Euclidean.Vector3D);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject vector = JObject.Load(reader);
                return new MathNet.Spatial.Euclidean.Vector3D((double)vector["x"], (double)vector["y"], (double)vector["z"]);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                MathNet.Spatial.Euclidean.Vector3D vector = (MathNet.Spatial.Euclidean.Vector3D)value;
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(vector.X);
                writer.WritePropertyName("y");
                writer.WriteValue(vector.Y);
                writer.WritePropertyName("z");
                writer.WriteValue(vector.Z);
                writer.WriteEndObject();
            }
        }
    }
}
