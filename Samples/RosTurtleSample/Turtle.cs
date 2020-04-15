// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1118 // Parameter must not span multiple lines

namespace TurtleROSSample
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Ros;

    /// <summary>
    /// ROS turtle bridge.
    /// </summary>
    public class Turtle
    {
        private const string NodeName = "/turtle_sample";
        private const string CmdVelTopic = "/turtle1/cmd_vel";
        private const string PoseTopic = "/turtle1/pose";

        private readonly string rosSlave;
        private readonly string rosMaster;

        private RosNode.Node node;
        private RosPublisher.IPublisher cmdVelPublisher;
        private RosSubscriber.ISubscriber poseSubscriber;

        private RosMessage.MessageDef poseMessageDef = RosMessage.CreateMessageDef(
            "turtlesim/Pose",
            "863b248d5016ca62ea2e895ae5265cf9",
            new[]
            {
                Tuple.Create("x", RosMessage.RosFieldDef.Float32Def),
                Tuple.Create("y", RosMessage.RosFieldDef.Float32Def),
                Tuple.Create("theta", RosMessage.RosFieldDef.Float32Def),
                Tuple.Create("linear_velocity", RosMessage.RosFieldDef.Float32Def),
                Tuple.Create("angular_velocity", RosMessage.RosFieldDef.Float32Def),
            });

        /// <summary>
        /// Initializes a new instance of the <see cref="Turtle"/> class.
        /// </summary>
        /// <param name="rosSlave">ROS slave address.</param>
        /// <param name="rosMaster">ROS master address.</param>
        public Turtle(string rosSlave, string rosMaster)
        {
            this.rosSlave = rosSlave;
            this.rosMaster = rosMaster;
        }

        /// <summary>
        /// Pose changed event handler.
        /// </summary>
        public event EventHandler<(float, float, float)> PoseChanged;

        /// <summary>
        /// Connect to ROS.
        /// </summary>
        public void Connect()
        {
            this.node = new RosNode.Node(NodeName, this.rosSlave, this.rosMaster);
            this.cmdVelPublisher = this.node.CreatePublisher(RosMessageTypes.Geometry.Twist.Def, CmdVelTopic, false);
            this.poseSubscriber = this.node.Subscribe(this.poseMessageDef, PoseTopic, this.PoseUpdate);
        }

        /// <summary>
        /// Disconnect from ROS.
        /// </summary>
        public void Disconnect()
        {
            this.node.UnregisterPublisher(CmdVelTopic);
            this.node.UnregisterSubscriber(PoseTopic);
        }

        /// <summary>
        /// Velocity action.
        /// </summary>
        /// <param name="linear">Linear speed.</param>
        /// <param name="angular">Angular speed.</param>
        public void Velocity(float linear, float angular)
        {
            this.cmdVelPublisher.Publish(
                RosMessageTypes.Geometry.Twist.ToMessage(
                    new RosMessageTypes.Geometry.Twist.Kind(
                        new RosMessageTypes.Geometry.Vector3.Kind(linear, 0, 0),
                        new RosMessageTypes.Geometry.Vector3.Kind(0, 0, angular))));
        }

        private void PoseUpdate(IEnumerable<Tuple<string, RosMessage.RosFieldVal>> position)
        {
            if (this.PoseChanged != null)
            {
                dynamic pos = RosMessage.GetDynamicFieldVals(position);
                this.PoseChanged(this, (pos.x, pos.y, pos.theta));
            }
        }
    }
}

#pragma warning restore SA1118 // Parameter must not span multiple lines
