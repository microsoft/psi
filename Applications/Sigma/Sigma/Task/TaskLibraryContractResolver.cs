// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Reflection;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Contract resolver that performs custom json serialization for various relevant types in <see cref="TaskLibrary{TTask}"/>.
    /// </summary>
    public class TaskLibraryContractResolver : DefaultContractResolver
    {
        private readonly CoordinateSystemConverter coordinateSystemConverter = new ();

        /// <inheritdoc />
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (property.PropertyType == typeof(CoordinateSystem))
            {
                property.Converter = this.coordinateSystemConverter;
            }

            return property;
        }

        private class CoordinateSystemConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(CoordinateSystem);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var vector = JArray.Load(reader);
                var array = new double[16];
                for (int i = 0; i < 16; i++)
                {
                    array[i] = (double)vector[i];
                }

                return new CoordinateSystem(Matrix<double>.Build.Dense(4, 4, array));
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var coordinateSystem = (CoordinateSystem)value;
                writer.WriteStartArray();
                foreach (var v in coordinateSystem.Values)
                {
                    writer.WriteValue(v);
                }

                writer.WriteEndArray();
            }
        }
    }
}
