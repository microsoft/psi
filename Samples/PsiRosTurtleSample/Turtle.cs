// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1118 // Parameter must not span multiple lines

namespace TurtleROSSample
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Ros;

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
                Tuple.Create("angular_velocity", RosMessage.RosFieldDef.Float32Def)
            });

        public Turtle(string rosSlave, string rosMaster)
        {
            this.rosSlave = rosSlave;
            this.rosMaster = rosMaster;
        }

        public event EventHandler<Tuple<float, float, float>> PoseChanged;

        public void Connect()
        {
            this.node = new RosNode.Node(NodeName, this.rosSlave, this.rosMaster);
            this.cmdVelPublisher = this.node.CreatePublisher(RosMessageTypes.Geometry.Twist.Def, CmdVelTopic, false);
            this.poseSubscriber = this.node.Subscribe(this.poseMessageDef, PoseTopic, this.PoseUpdate);
        }

        public void Disconnect()
        {
            this.node.UnregisterPublisher(CmdVelTopic);
            this.node.UnregisterSubscriber(PoseTopic);
        }

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
                var pos = position.Select(f => RosMessage.GetFloat32Val(f.Item2)).ToArray();
                this.PoseChanged(this, Tuple.Create(pos[0], pos[1], pos[2])); // drop velocity info
            }
        }
    }
}

#pragma warning restore SA1118 // Parameter must not span multiple lines
