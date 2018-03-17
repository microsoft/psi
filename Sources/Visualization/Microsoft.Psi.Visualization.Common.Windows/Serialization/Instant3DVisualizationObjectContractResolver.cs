// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Serialization
{
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Contract resolver that mitigates issues when resolving MathNet.Spatial.Euclidean.Vector3D properties.
    /// </summary>
    public class Instant3DVisualizationObjectContractResolver : DefaultContractResolver
    {
        /// <inheritdoc />
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            // MathNet.Spatial.Euclidean.Vector3D.Orthogonal is a computed property that throws sometimes
            if (property.DeclaringType == typeof(MathNet.Spatial.Euclidean.Vector3D) &&
                property.PropertyName == nameof(MathNet.Spatial.Euclidean.Vector3D.Orthogonal))
            {
                property.ShouldSerialize = instance => false;
                property.ShouldDeserialize = instance => false;
            }

            return property;
        }
    }
}
