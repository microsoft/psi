// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.MixedReality.OpenXR;
    using Microsoft.Psi.MixedReality.WinRT;
    using Microsoft.Psi.Serialization;
    using Microsoft.Psi.Spatial.Euclidean;
    using StereoKitHand = Microsoft.Psi.MixedReality.StereoKit.Hand;

    /// <summary>
    /// Represents the user state.
    /// </summary>
    [Serializer(typeof(UserState.CustomSerializer))]
    public class UserState
    {
        /// <summary>
        /// Gets or sets the head pose.
        /// </summary>
        public CoordinateSystem Head { get; set; }

        /// <summary>
        /// Gets or sets the head velocity.
        /// </summary>
        public CoordinateSystemVelocity3D HeadVelocity3D { get; set; }

        /// <summary>
        /// Gets or sets the eyes.
        /// </summary>
        public Eyes Eyes { get; set; }

        /// <summary>
        /// Gets or sets the left hand.
        /// </summary>
        public Hand HandLeft { get; set; }

        /// <summary>
        /// Gets or sets the right hand.
        /// </summary>
        public Hand HandRight { get; set; }

        /// <summary>
        /// Provides custom read- backcompat serialization for <see cref="UserState"/> objects.
        /// </summary>
        public class CustomSerializer : BackCompatClassSerializer<UserState>
        {
            // When introducing a custom serializer, the LatestSchemaVersion
            // is set to be one above the auto-generated schema version (given by
            // RuntimeInfo.LatestSerializationSystemVersion, which was 2 at the time)
            private const int LatestSchemaVersion = 3;
            private SerializationHandler<CoordinateSystem> headHander;
            private SerializationHandler<CoordinateSystemVelocity3D> headVelocity3DHandler;
            private SerializationHandler<Ray3D> eyesHandler;
            private SerializationHandler<StereoKitHand> handLeftHandler;
            private SerializationHandler<StereoKitHand> handRightHandler;

            /// <summary>
            /// Initializes a new instance of the <see cref="CustomSerializer"/> class.
            /// </summary>
            public CustomSerializer()
                : base(LatestSchemaVersion)
            {
            }

            /// <inheritdoc/>
            public override void InitializeBackCompatSerializationHandlers(int schemaVersion, KnownSerializers serializers, TypeSchema targetSchema)
            {
                if (schemaVersion <= 2)
                {
                    this.headHander = serializers.GetHandler<CoordinateSystem>();
                    this.headVelocity3DHandler = serializers.GetHandler<CoordinateSystemVelocity3D>();
                    this.eyesHandler = serializers.GetHandler<Ray3D>();
                    this.handLeftHandler = serializers.GetHandler<StereoKitHand>();
                    this.handRightHandler = serializers.GetHandler<StereoKitHand>();
                }
                else
                {
                    throw new NotSupportedException($"{nameof(UserState.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }

            /// <inheritdoc/>
            public override void BackCompatDeserialize(int schemaVersion, BufferReader reader, ref UserState target, SerializationContext context)
            {
                if (schemaVersion <= 2)
                {
                    var head = default(CoordinateSystem);
                    var headVelocity3D = default(CoordinateSystemVelocity3D);
                    var eyes = default(Ray3D);
                    var handLeft = default(StereoKitHand);
                    var handRight = default(StereoKitHand);

                    this.headHander.Deserialize(reader, ref head, context);
                    this.headVelocity3DHandler.Deserialize(reader, ref headVelocity3D, context);
                    this.eyesHandler.Deserialize(reader, ref eyes, context);
                    this.handLeftHandler.Deserialize(reader, ref handLeft, context);
                    this.handRightHandler.Deserialize(reader, ref handRight, context);

                    target = new UserState()
                    {
                        Head = head,
                        HeadVelocity3D = headVelocity3D,
                        Eyes = new Eyes(eyes, true),
                        HandLeft = handLeft.ToOpenXRHand(),
                        HandRight = handRight.ToOpenXRHand(),
                    };
                }
                else
                {
                    throw new NotSupportedException($"{nameof(Eyes.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }
        }
    }
}
