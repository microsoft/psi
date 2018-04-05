// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1118 // Parameter must not span multiple lines

namespace ArmControlROSSample
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Ros;

    public class UArm
    {
        private const string NodeName = "/uarm_metal_sample";
        private const string PumpTopic = "/uarm_metal/pump";
        private const string BeepTopic = "/uarm_metal/beep";
        private const string PositionReadTopic = "/uarm_metal/position_read";
        private const string PositionWriteTopic = "/uarm_metal/position_write";

        private readonly string rosSlave;
        private readonly string rosMaster;

        private RosNode.Node node;
        private RosPublisher.IPublisher pumpPublisher;
        private RosPublisher.IPublisher beepPublisher;
        private RosSubscriber.ISubscriber positionSubscriber;
        private RosPublisher.IPublisher positionPublisher;

        private RosMessage.MessageDef beepMessageDef = RosMessage.CreateMessageDef(
            "uarm_metal/Beep",
            "8c872fbca0d0a5bd8ca8259935da556e",
            new[]
            {
                Tuple.Create("frequency", RosMessage.RosFieldDef.Float32Def),
                Tuple.Create("duration", RosMessage.RosFieldDef.Float32Def)
            });

        private RosMessage.MessageDef positionMessageDef = RosMessage.CreateMessageDef(
            "uarm_metal/Position",
            "cc153912f1453b708d221682bc23d9ac",
            new[]
            {
                Tuple.Create("x", RosMessage.RosFieldDef.Float32Def),
                Tuple.Create("y", RosMessage.RosFieldDef.Float32Def),
                Tuple.Create("z", RosMessage.RosFieldDef.Float32Def)
            });

        private float x;

        private float y;

        private float z;

        public UArm(string rosSlave, string rosMaster)
        {
            this.rosSlave = rosSlave;
            this.rosMaster = rosMaster;
        }

        public event EventHandler<Tuple<float, float, float>> PositionChanged;

        public void Connect()
        {
            this.node = new RosNode.Node(NodeName, this.rosSlave, this.rosMaster);
            this.pumpPublisher = this.node.CreatePublisher(RosMessageTypes.Standard.Bool.Def, PumpTopic, false);
            this.beepPublisher = this.node.CreatePublisher(this.beepMessageDef, BeepTopic, false);
            this.positionSubscriber = this.node.Subscribe(this.positionMessageDef, PositionReadTopic, this.PositionUpdate);
            this.positionPublisher = this.node.CreatePublisher(this.positionMessageDef, PositionWriteTopic, false);
        }

        public void Disconnect()
        {
            this.node.UnregisterPublisher(PumpTopic);
            this.node.UnregisterPublisher(BeepTopic);
            this.node.UnregisterSubscriber(PositionReadTopic);
            this.node.UnregisterPublisher(PositionWriteTopic);
        }

        public void Pump(bool pump)
        {
            this.pumpPublisher.Publish(RosMessageTypes.Standard.Bool.ToMessage(pump));
        }

        public void Beep(float frequency, float duration)
        {
            this.beepPublisher.Publish(this.BeepMessage(frequency, duration));
        }

        public void AbsolutePosition(float x, float y, float z)
        {
            this.positionPublisher.Publish(this.PositionMessage(x, y, z));
        }

        public void RelativePosition(float x, float y, float z)
        {
            this.AbsolutePosition(this.x + x, this.y + y, this.z + z);
        }

        private Tuple<string, RosMessage.RosFieldVal>[] BeepMessage(float frequency, float duration)
        {
            return new[]
            {
                Tuple.Create("frequency", RosMessage.RosFieldVal.NewFloat32Val(frequency)),
                Tuple.Create("duration", RosMessage.RosFieldVal.NewFloat32Val(duration))
            };
        }

        private void PositionUpdate(IEnumerable<Tuple<string, RosMessage.RosFieldVal>> position)
        {
            foreach (var p in position)
            {
                var name = p.Item1;
                var val = RosMessage.GetFloat32Val(p.Item2);
                switch (name)
                {
                    case "x":
                        this.x = val;
                        break;

                    case "y":
                        this.y = val;
                        break;

                    case "z":
                        this.z = val;
                        break;
                }
            }

            this.PositionChanged?.Invoke(this, Tuple.Create(this.x, this.y, this.z));
        }

        private Tuple<string, RosMessage.RosFieldVal>[] PositionMessage(float x, float y, float z)
        {
            return new[]
            {
                Tuple.Create("x", RosMessage.RosFieldVal.NewFloat32Val(x)),
                Tuple.Create("y", RosMessage.RosFieldVal.NewFloat32Val(y)),
                Tuple.Create("z", RosMessage.RosFieldVal.NewFloat32Val(z))
            };
        }
    }
}

#pragma warning restore SA1118 // Parameter must not span multiple lines
