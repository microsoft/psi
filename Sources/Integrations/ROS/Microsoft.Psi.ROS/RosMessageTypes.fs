// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Ros

module RosMessageTypes =

    open System
    open RosMessage

    module Standard =

        module Header =
            let Def = { Type   = "std_msgs/Header"
                        MD5    = "2176decaecbce78abc3b96ef049fabed"
                        Fields = ["seq",      UInt32Def
                                  "stamp",    TimeDef
                                  "frame_id", StringDef] }
            type Kind = { Seq:     uint32
                          Stamp:   DateTime
                          FrameID: string }
            let FromMessage m = m |> Seq.toList |> function ["seq",      UInt32Val seq
                                                             "stamp",    TimeVal (sec, nsec)
                                                             "frame_id", StringVal frameId] -> { Seq     = seq
                                                                                                 Stamp   = toDateTime sec nsec
                                                                                                 FrameID = frameId } | _ -> malformed ()
            let ToMessage { Seq     = seq
                            Stamp   = stamp
                            FrameID = frameId } = ["seq",      UInt32Val seq
                                                   "stamp",    TimeVal (fromDateTime stamp)
                                                   "frame_id", StringVal frameId] |> Seq.ofList

        module String =
            let Def = { Type   = "std_msgs/String"
                        MD5    = "992ce8a1687cec8c8bd883ec73ca41d1"
                        Fields = ["data", StringDef] }
            type Kind = string
            let FromMessage m = m |> Seq.toList |> function ["data", StringVal str] -> str | _ -> malformed ()
            let ToMessage str = ["data", StringVal str] |> Seq.ofList

        module Bool =
            let Def = { Type   = "std_msgs/Bool"
                        MD5    = "8b94c1b53db61fb6aed406028ad6332a"
                        Fields = ["data", BoolDef] }
            type Kind = bool
            let FromMessage m = m |> Seq.toList |> function ["data", BoolVal b] -> b | _ -> malformed ()
            let ToMessage b = ["data", BoolVal b] |> Seq.ofList

        module ColorRGBA =
            let Def = { Type   = "std_msgs/ColorRGBA"
                        MD5    = "a29a96539573343b1310c73607334b00"
                        Fields = ["r", Float32Def
                                  "g", Float32Def
                                  "b", Float32Def
                                  "a", Float32Def] }
            type Kind = { R: single
                          G: single
                          B: single
                          A: single }
            let FromMessage m = m |> Seq.toList |> function ["r", Float32Val r
                                                             "g", Float32Val g
                                                             "b", Float32Val b
                                                             "a", Float32Val a] -> { R = r
                                                                                     G = g
                                                                                     B = b
                                                                                     A = a } | _ -> malformed ()
            let ToMessage { R = r
                            G = g
                            B = b
                            A = a } = ["r", Float32Val r
                                       "g", Float32Val g
                                       "b", Float32Val b
                                       "a", Float32Val a] |> Seq.ofList

    module ActionLib =

        module GoalID =
            let Def = { Type   = "actionlib_msgs/GoalID"
                        MD5    = "302881f31927c1df708a2dbab0e80ee8"
                        Fields = ["stamp", TimeDef
                                  "id",    StringDef] }
            type Kind = { Stamp: DateTime
                          ID:    string }
            let FromMessage m = m |> Seq.toList |> function ["stamp", TimeVal (sec, nsec)
                                                             "id",    StringVal id] -> { Stamp = toDateTime sec nsec
                                                                                         ID =    id } | _ -> malformed ()
            let ToMessage { Stamp = stamp
                            ID    = id } = ["stamp", TimeVal (fromDateTime stamp)
                                            "id",    StringVal id] |> Seq.ofList

    module Sensor =

        module Range =
            let Def = { Type   = "sensor_msgs/Range"
                        MD5    = "c005c34273dc426c67a020a87bc24148"
                        Fields = ["header",         StructDef Standard.Header.Def.Fields
                                  "radiation_type", UInt8Def
                                  "field_of_view",  Float32Def
                                  "min_range",      Float32Def
                                  "max_range",      Float32Def
                                  "range",          Float32Def] }
            type Kind = { Header:        Standard.Header.Kind
                          Range:         single
                          RadiationType: uint8
                          FieldOfView:   single
                          MinRange:      single
                          MaxRange:      single }
            let FromMessage m = m |> Seq.toList |> function ["header",         StructVal header
                                                             "radiation_type", UInt8Val rad
                                                             "field_of_view",  Float32Val fov
                                                             "min_range",      Float32Val min
                                                             "max_range",      Float32Val max
                                                             "range",          Float32Val range] -> { Header        = Standard.Header.FromMessage header
                                                                                                      Range         = range
                                                                                                      RadiationType = rad
                                                                                                      FieldOfView   = fov
                                                                                                      MinRange      = min
                                                                                                      MaxRange      = max } | _ -> malformed ()
            let ToMessage { Header        = header
                            Range         = range
                            RadiationType = rad
                            FieldOfView   = fov
                            MinRange      = min
                            MaxRange      = max } = ["header",         StructVal (Standard.Header.ToMessage header |> Seq.toList)
                                                     "radiation_type", UInt8Val rad
                                                     "field_of_view",  Float32Val fov
                                                     "min_range",      Float32Val min
                                                     "max_range",      Float32Val max
                                                     "range",          Float32Val range] |> Seq.ofList

        module JointState =
            let Def = { Type   = "sensor_msgs/JointState"
                        MD5    = "3066dcd76a6cfaef579bd0f34173e9fd"
                        Fields = ["header",   StructDef Standard.Header.Def.Fields
                                  "name",     VariableArrayDef StringDef
                                  "position", VariableArrayDef Float64Def
                                  "velocity", VariableArrayDef Float64Def
                                  "effort",   VariableArrayDef Float64Def] }
            type Kind = { Header:   Standard.Header.Kind
                          Name:     string seq
                          Position: float seq
                          Velocity: float seq
                          Effort:   float seq }
            let FromMessage m = m |> Seq.toList |> function ["header",   StructVal header
                                                             "name",     VariableArrayVal name
                                                             "position", VariableArrayVal position
                                                             "velocity", VariableArrayVal velocity
                                                             "effort",   VariableArrayVal effort] -> { Header   = Standard.Header.FromMessage header
                                                                                                       Name     = List.map (function StringVal str -> str | _ -> malformed ()) name
                                                                                                       Position = List.map (function Float64Val pos -> pos | _ -> malformed ()) position
                                                                                                       Velocity = List.map (function Float64Val vel -> vel | _ -> malformed ()) velocity
                                                                                                       Effort   = List.map (function Float64Val eff -> eff | _ -> malformed ()) effort } | _ -> malformed ()
            let ToMessage { Header   = header
                            Name     = name
                            Position = position
                            Velocity = velocity
                            Effort   = effort } = ["header",   StructVal (Standard.Header.ToMessage header |> Seq.toList)
                                                   "name",     VariableArrayVal (Seq.map StringVal name |> Seq.toList)
                                                   "position", VariableArrayVal (Seq.map Float64Val position |> Seq.toList)
                                                   "velocity", VariableArrayVal (Seq.map Float64Val velocity |> Seq.toList)
                                                   "effort",   VariableArrayVal (Seq.map Float64Val effort |> Seq.toList)] |> Seq.ofList

        module Image =
            let Def = { Type   = "sensor_msgs/Image"
                        MD5    = "060021388200f6f0f447d0fcd9c64743"
                        Fields = ["header",       StructDef Standard.Header.Def.Fields
                                  "height",       UInt32Def
                                  "width",        UInt32Def
                                  "encoding",     StringDef
                                  "is_bigendian", BoolDef
                                  "step",         UInt32Def
                                  "data",         VariableArrayDef UInt8Def] }
            type Kind = { Header:      Standard.Header.Kind
                          Height:      int
                          Width:       int
                          Encoding:    string
                          IsBigEndian: bool
                          Step:        int
                          Data:        byte array }
            let FromMessage m = m |> Seq.toList |> function ["header",       StructVal header
                                                             "height",       UInt32Val height
                                                             "width",        UInt32Val width
                                                             "encoding",     StringVal encoding
                                                             "is_bigendian", BoolVal isBigEndian
                                                             "step",         UInt32Val step
                                                             "data",         VariableArrayVal data] -> { Header = Standard.Header.FromMessage header
                                                                                                         Height = int height
                                                                                                         Width = int width
                                                                                                         Encoding = encoding
                                                                                                         IsBigEndian = isBigEndian
                                                                                                         Step = int step
                                                                                                         Data = Seq.map (function UInt8Val b -> b | _ -> malformed ()) data |> Array.ofSeq } | _ -> malformed ()
            let ToMessage { Header      = header
                            Height      = height
                            Width       = width
                            Encoding    = encoding
                            IsBigEndian = isBigEndian
                            Step        = step
                            Data        = data } = ["header", StructVal (Standard.Header.ToMessage header |> Seq.toList)
                                                    "height", UInt32Val (uint32 height)
                                                    "width", UInt32Val (uint32 width)
                                                    "encoding", StringVal encoding
                                                    "is_bigendian", BoolVal isBigEndian
                                                    "step", UInt32Val (uint32 step)
                                                    "data", VariableArrayVal (Seq.map UInt8Val data |> List.ofSeq)] |> Seq.ofList

    module Geometry =

        module Point =
            let Def = { Type   = "geometry_msgs/Point"
                        MD5    = "4a842b65f413084dc2b10fb484ea7f17"
                        Fields = ["x", Float64Def
                                  "y", Float64Def
                                  "z", Float64Def] }
            type Kind = { X: float
                          Y: float
                          Z: float }
            let FromMessage m = m |> Seq.toList |> function ["x", Float64Val x
                                                             "y", Float64Val y
                                                             "z", Float64Val z] -> { X = x
                                                                                     Y = y
                                                                                     Z = z } | _ -> malformed ()
            let ToMessage { X = x
                            Y = y
                            Z = z } = ["x", Float64Val x
                                       "y", Float64Val y
                                       "z", Float64Val z] |> Seq.ofList

        module Quaternion =
            let Def = { Type   = "geometry_msgs/Quaternion"
                        MD5    = "a779879fadf0160734f906b8c19c7004"
                        Fields = ["x", Float64Def
                                  "y", Float64Def
                                  "z", Float64Def
                                  "w", Float64Def] }
            type Kind = { X: float
                          Y: float
                          Z: float
                          W: float }
            let FromMessage m = m |> Seq.toList |> function ["x", Float64Val x
                                                             "y", Float64Val y
                                                             "z", Float64Val z
                                                             "w", Float64Val w] -> { X = x
                                                                                     Y = y
                                                                                     Z = z
                                                                                     W = w } | _ -> malformed ()
            let ToMessage { X = x
                            Y = y
                            Z = z
                            W = w } = ["x", Float64Val x
                                       "y", Float64Val y
                                       "z", Float64Val z
                                       "w", Float64Val w] |> Seq.ofList

        module Pose =
            let Def = { Type    = "geometry_msgs/Pose"
                        MD5     = "e45d45a5a1ce597b249e23fb30fc871f"
                        Fields  = ["position",    StructDef Point.Def.Fields
                                   "orientation", StructDef Quaternion.Def.Fields] }
            type Kind = { Position:    Point.Kind
                          Orientation: Quaternion.Kind }
            let FromMessage m = m |> Seq.toList |> function ["position",    StructVal position
                                                             "orientation", StructVal orientation] -> { Position    = Point.FromMessage position
                                                                                                        Orientation = Quaternion.FromMessage orientation } | _ -> malformed ()
            let ToMessage { Position    = position
                            Orientation = orientation } = ["position",    StructVal (Point.ToMessage position |> Seq.toList)
                                                           "orientation", StructVal (Quaternion.ToMessage orientation |> Seq.toList)] |> Seq.ofList

        module PoseWithCovariance =
            let Def = { Type   = "geometry_msgs/PoseWithCovariance"
                        MD5    = "c23e848cf1b7533a8d7c259073a97e6f"
                        Fields = ["pose",       StructDef Pose.Def.Fields
                                  "covariance", FixedArrayDef (36, Float64Def)] }
            type Kind = { Pose:       Pose.Kind
                          Covariance: float array }
            let FromMessage m = m |> Seq.toList |> function ["pose",       StructVal pose
                                                             "covariance", FixedArrayVal covariance] -> { Pose       = Pose.FromMessage pose
                                                                                                          Covariance = List.map (function Float64Val f -> f | _ -> malformed ()) covariance |> List.toArray } | _ -> malformed ()
            let ToMessage { Pose       = pose
                            Covariance = covariance } = ["pose",       StructVal (Pose.ToMessage pose |> Seq.toList)
                                                         "covariance", FixedArrayVal (Seq.map Float64Val covariance |> Seq.toList)] |> Seq.ofList

        module PoseWithCovarianceStamped =
            let Def = { Type = "geometry_msgs/PoseWithCobarianceStamped"
                        MD5 = "953b798c0f514ff060a53a3498ce6246"
                        Fields = ["header", StructDef Standard.Header.Def.Fields
                                  "pose", StructDef PoseWithCovariance.Def.Fields] }
            type Kind = { Header: Standard.Header.Kind
                          Pose:   PoseWithCovariance.Kind }
            let FromMessage m = m |> Seq.toList |> function ["header", StructVal header
                                                             "pose",   StructVal pose] -> { Header = Standard.Header.FromMessage header
                                                                                            Pose   = PoseWithCovariance.FromMessage pose } | _ -> malformed ()
            let ToMessage { Header = header
                            Pose   = pose } = ["header", StructVal (Standard.Header.ToMessage header |> Seq.toList)
                                               "pose",   StructVal (PoseWithCovariance.ToMessage pose |> Seq.toList)] |> Seq.ofList

        module Pose2D =
            let Def = { Type   = "geometry_msgs/Pose2D"
                        MD5    = "938fa65709584ad8e77d238529be13b8"
                        Fields = ["x",     Float64Def
                                  "y",     Float64Def
                                  "theta", Float64Def] }
            type Kind = { X:     float
                          Y:     float
                          Theta: float }
            let FromMessage m = m |> Seq.toList |> function ["x",     Float64Val x
                                                             "y",     Float64Val y
                                                             "theta", Float64Val theta] -> { X     = x
                                                                                             Y     = y
                                                                                             Theta = theta } | _ -> malformed ()
            let ToMessage { X = x
                            Y = y
                            Theta = theta } = ["x",     Float64Val x
                                               "y",     Float64Val y
                                               "theta", Float64Val theta] |> Seq.ofList

        module Vector3 =
            let Def = { Type   = "geometry_msgs/Vector3"
                        MD5    = "4a842b65f413084dc2b10fb484ea7f17"
                        Fields = ["x", Float64Def
                                  "y", Float64Def
                                  "z", Float64Def] }
            type Kind = { X: float
                          Y: float
                          Z: float }
            let FromMessage m = m |> Seq.toList |> function ["x", Float64Val x
                                                             "y", Float64Val y
                                                             "z", Float64Val z] -> { X = x
                                                                                     Y = y
                                                                                     Z = z } | _ -> malformed ()
            let ToMessage { X = x
                            Y = y
                            Z = z } = ["x", Float64Val x
                                       "y", Float64Val y
                                       "z", Float64Val z] |> Seq.ofList
        module Twist =
            let Def = { Type = "geometry_msgs/Twist"
                        MD5 = "9f195f881246fdfa2798d1d3eebca84a"
                        Fields = ["linear", StructDef Vector3.Def.Fields
                                  "angular", StructDef Vector3.Def.Fields] }
            type Kind = { Linear:  Vector3.Kind
                          Angular: Vector3.Kind }
            let FromMessage m = m |> Seq.toList |> function ["linear", StructVal linear
                                                             "angular", StructVal angular] -> { Linear  = Vector3.FromMessage linear
                                                                                                Angular = Vector3.FromMessage angular } | _ -> malformed ()
            let ToMessage { Linear  = linear
                            Angular = angular } = ["linear",  StructVal (Vector3.ToMessage linear |> Seq.toList)
                                                   "angular", StructVal (Vector3.ToMessage angular |> Seq.toList)] |> Seq.ofList

    module People =
        
        module PositionMeasurement =
            let Def = { Type    = "people_msgs/PositionMeasurement"
                        MD5     = "54fa938b4ec28728e01575b79eb0ec7c"
                        Fields  = [ "header",           StructDef Standard.Header.Def.Fields
                                    "name",             StringDef
                                    "object_id",        StringDef
                                    "pos",              StructDef Geometry.Point.Def.Fields
                                    "reliability",      Float64Def
                                    "covariance",       FixedArrayDef(9, Float64Def)
                                    "initialization",   Int8Def] }
            
            type Kind = {   Header:         Standard.Header.Kind
                            Name:           string
                            ObjectId:       string
                            Pos:            Geometry.Point.Kind
                            Reliability:    float
                            Covariance:     float array
                            Initialization: int8 }
            
            let FromMessage m = m |> Seq.toList |> function ["header",          StructVal header
                                                             "name",            StringVal name
                                                             "object_id",       StringVal object_id
                                                             "pos",             StructVal pos
                                                             "reliability",     Float64Val reliability
                                                             "covariance",      FixedArrayVal covariance
                                                             "initialization",  Int8Val initialization] -> { Header         = Standard.Header.FromMessage header
                                                                                                             Name           = name
                                                                                                             ObjectId       = object_id
                                                                                                             Pos            = Geometry.Point.FromMessage pos
                                                                                                             Reliability    = reliability
                                                                                                             Covariance     = List.map (function Float64Val f -> f | _ -> malformed ()) covariance |> List.toArray
                                                                                                             Initialization = initialization } | _ -> malformed ()

            let ToMessage { Header          = header
                            Name            = name
                            ObjectId        = object_id
                            Pos             = pos
                            Reliability     = reliability
                            Covariance      = covariance
                            Initialization  = initialization } = ["header",         StructVal (Standard.Header.ToMessage header |> Seq.toList)
                                                                  "name",           StringVal name
                                                                  "object_id",      StringVal object_id
                                                                  "pos",            StructVal (Geometry.Point.ToMessage pos |> Seq.toList)
                                                                  "reliability",    Float64Val reliability
                                                                  "covariance",     FixedArrayVal (Seq.map Float64Val covariance |> Seq.toList)
                                                                  "initialization", Int8Val initialization] |> Seq.ofList

        module PositionMeasurementArray =
            let Def = { Type    = "people_msgs/PositionMeasurementArray"
                        MD5     = "59c860d40aa739ec920eb3ad24ae019e"
                        Fields  = [ "header",       StructDef Standard.Header.Def.Fields
                                    "people",       VariableArrayDef (StructDef PositionMeasurement.Def.Fields)
                                    "cooccurrence", VariableArrayDef Float64Def ] }

            type Kind = {   Header:         Standard.Header.Kind
                            People:         PositionMeasurement.Kind array
                            Cooccurrence:   single array }

            let FromMessage m = m |> Seq.toList |> function ["header",          StructVal header
                                                             "people",          VariableArrayVal people
                                                             "cooccurrence",    VariableArrayVal cooccurrence] -> { Header          = Standard.Header.FromMessage header
                                                                                                                    People          = people |> List.map (function StructVal p -> PositionMeasurement.FromMessage p | _ -> malformed ()) |> List.toArray
                                                                                                                    Cooccurrence    = cooccurrence |> List.map (function Float32Val c -> c | _ -> malformed ()) |> List.toArray } | _ -> malformed ()

            let ToMessage { Header        = header
                            People        = people
                            Cooccurrence  = cooccurrence } = ["header",       StructVal (Standard.Header.ToMessage header |> Seq.toList)
                                                              "people",       VariableArrayVal (Seq.map (PositionMeasurement.ToMessage >> Seq.toList >> StructVal) people |> Seq.toList)
                                                              "cooccurrence", VariableArrayVal (Seq.map Float32Val cooccurrence |> Seq.toList)] |> Seq.ofList

    module NaoQiBridge =

        module AudioBuffer =
            let Def = { Type = "naoqi_bridge_msgs/Audio"
                        MD5 = "50f300aa63f3c1b2f3d3173329165316"
                        Fields = ["header",     StructDef Standard.Header.Def.Fields
                                  "frequency",  UInt16Def
                                  "channelMap", VariableArrayDef UInt8Def
                                  "data",       VariableArrayDef Int16Def] }
            type Kind = { Header:     Standard.Header.Kind
                          Frequency:  uint16
                          ChannelMap: uint8 array
                          Data:       int16 array }
            let FromMessage m = m |> Seq.toList |> function ["header",     StructVal header
                                                             "frequency",  UInt16Val freq
                                                             "channelMap", VariableArrayVal map
                                                             "data",       VariableArrayVal data] -> { Header     = Standard.Header.FromMessage header
                                                                                                       Frequency  = freq
                                                                                                       ChannelMap = List.map (function UInt8Val m -> m | _ -> malformed ()) map |> List.toArray
                                                                                                       Data       = List.map (function Int16Val d -> d | _ -> malformed ()) data |> List.toArray } | _ -> malformed ()
            let ToMessage { Header     = header
                            Frequency  = freq
                            ChannelMap = map
                            Data       = data } = ["header",     StructVal (Standard.Header.ToMessage header |> Seq.toList)
                                                   "frequency",  UInt16Val freq
                                                   "channelMap", VariableArrayVal (Seq.map UInt8Val map |> List.ofSeq)
                                                   "data",       VariableArrayVal (Seq.map Int16Val data |> List.ofSeq)] |> Seq.ofList

        module BodyPoseGoal =
            let Def = { Type   = "naoqi_bridge_msgs/BodyPoseGoal"
                        MD5    = "e6184073e8e665fb2bf0be194fc36541"
                        Fields = ["pose_name", StringDef] }
            type Kind = { PoseName: string }
            let FromMessage m = m |> Seq.toList |> function ["pose_name", StringVal name] -> { PoseName = name } | _ -> malformed ()
            let ToMessage { PoseName = name } = ["pose_name", StringVal name] |> Seq.ofList

        module BodyPoseActionGoal =
            let Def = { Type   = "naoqi_bridge_msgs/BodyPoseActionGoal"
                        MD5    = "0c4ae1487ff4d033a7fa048a0b31509c"
                        Fields = ["header",  StructDef Standard.Header.Def.Fields
                                  "goal_id", StructDef ActionLib.GoalID.Def.Fields
                                  "goal",    StructDef BodyPoseGoal.Def.Fields ]}
            type Kind = { Header: Standard.Header.Kind
                          GoalID: ActionLib.GoalID.Kind
                          Goal:   BodyPoseGoal.Kind }
            let FromMessage m = m |> Seq.toList |> function ["header",  StructVal header
                                                             "goal_id", StructVal id
                                                             "goal",    StructVal goal] -> { Header = Standard.Header.FromMessage header
                                                                                             GoalID = ActionLib.GoalID.FromMessage id
                                                                                             Goal   = BodyPoseGoal.FromMessage goal } | _ -> malformed ()
            let ToMessage { Header = header
                            GoalID = id
                            Goal   = goal } = ["header",  StructVal (Standard.Header.ToMessage header |> Seq.toList)
                                               "goal_id", StructVal (ActionLib.GoalID.ToMessage id |> Seq.toList)
                                               "goal",    StructVal (BodyPoseGoal.ToMessage goal |> Seq.toList)] |> Seq.ofList

        module JointAnglesWithSpeed =
            let Def = { Type = "naoqi_bridge_msgs/JointAnglesWithSpeed"
                        MD5  = "052ca11f74a00ad6745dfff6ebc2b4d8"
                        Fields = ["header",       StructDef Standard.Header.Def.Fields
                                  "joint_names",  VariableArrayDef StringDef
                                  "joint_angles", VariableArrayDef Float32Def
                                  "speed",        Float32Def
                                  "relative",     BoolDef]}
            type Kind = { Header:      Standard.Header.Kind
                          JointNames:  string seq
                          JointAngles: single seq
                          Speed:       single
                          Relative:    bool }
            let FromMessage m = m |> Seq.toList |> function ["header",       StructVal header
                                                             "joint_names",  VariableArrayVal names
                                                             "joint_angles", VariableArrayVal angles
                                                             "speed",        Float32Val speed
                                                             "relative",     BoolVal relative] -> { Header      = Standard.Header.FromMessage header
                                                                                                    JointNames  = List.map (function StringVal n -> n | _ -> malformed ()) names
                                                                                                    JointAngles = List.map (function Float32Val a -> a | _ -> malformed ()) angles
                                                                                                    Speed       = speed
                                                                                                    Relative    = relative } | _ -> malformed ()
            let ToMessage { Header      = header
                            JointNames  = names
                            JointAngles = angles
                            Speed       = speed
                            Relative    = relative } = ["header",       StructVal (Standard.Header.ToMessage header |> Seq.toList)
                                                        "joint_names",  VariableArrayVal (Seq.map StringVal names |> Seq.toList)
                                                        "joint_angles", VariableArrayVal (Seq.map Float32Val angles |> Seq.toList)
                                                        "speed",        Float32Val speed
                                                        "relative",     BoolVal relative] |> Seq.ofList

        module TactileTouch =
            let Def = { Type   = "naoqi_bridge_msgs/TactileTouch"
                        MD5    =  "b75165bf9dfed26d50ad4e3162304225"
                        Fields = ["button", UInt8Def
                                  "state",  UInt8Def] } // 1 buttonFront, 2 buttonMIddle, 3 buttonRear, 0 stateReleased 1 statePressed
            type Kind = { Button: uint8
                          State:  uint8 }
            let FromMessage m = m |> Seq.toList |> function ["button", UInt8Val button
                                                             "state",  UInt8Val state] -> { Button = button
                                                                                            State  = state } | _ -> malformed ()
            let ToMessage { Button = button
                            State  = state } = ["button", UInt8Val button
                                                "state",  UInt8Val state] |> Seq.ofList

        module Bumper =
            let Def = { Type   = "naoqi_bridge_msgs/Bumper"
                        MD5    = "89965a81ab868825f18d59365e28ddaf"
                        Fields = ["bumper", UInt8Def
                                  "state",  UInt8Def] } // 0 right, 1 left, 2 back, 0 stateReleased 1 statePressed
            type Kind = { Bumper: uint8
                          State:  uint8 }
            let FromMessage m = m |> Seq.toList |> function ["bumper", UInt8Val bumper
                                                             "state",  UInt8Val state] -> { Bumper = bumper
                                                                                            State  = state } | _ -> malformed ()
            let ToMessage { Bumper = bumper
                            State  = state } = ["bumper", UInt8Val bumper
                                                "state",  UInt8Val state] |> Seq.ofList

        module FadeRGB =
            let Def = { Type   = "naoqi_bridge_msgs/FadeRGB"
                        MD5    = "0df8c8fbe7f1de5f2168d6117ffced08"
                        Fields = ["led_name", StringDef
                                  "color",    StructDef Standard.ColorRGBA.Def.Fields
                                  "duration", DurationDef] } // for LED names, see http://doc.aldebaran.com/2-1/naoqi/sensors/alleds.html
            type Kind = { LEDName:  string
                          Color:    Standard.ColorRGBA.Kind
                          Duration: TimeSpan }
            let FromMessage m = m |> Seq.toList |> function ["led_name", StringVal name
                                                             "color",    StructVal color
                                                             "duration", DurationVal (sec, nsec)] -> { LEDName  = name
                                                                                                       Color    = Standard.ColorRGBA.FromMessage color
                                                                                                       Duration = toTimeSpan sec nsec } | _ -> malformed ()
            let ToMessage { LEDName  = name
                            Color    = color
                            Duration = duration } = ["led_name", StringVal name
                                                     "color",    StructVal (Standard.ColorRGBA.ToMessage color |> Seq.toList)
                                                     "duration", DurationVal (fromTimeSpan duration)] |> Seq.ofList

        module Blink =
            module Goal =
                let Message = { Type   = "naoqi_bridge_msgs/BlinkGoal"
                                MD5    = "5e5d3c2ba9976dc121a0bb6ef7c66d79"
                                Fields = ["colors",   VariableArrayDef (StructDef Standard.ColorRGBA.Def.Fields)
                                          "bg_color", StructDef Standard.ColorRGBA.Def.Fields] }
                type Kind = { Colors:     Standard.ColorRGBA.Kind seq
                              Background: Standard.ColorRGBA.Kind }
                let FromMessage m = m |> Seq.toList |> function ["colors",   VariableArrayVal colors
                                                                 "bg_color", StructVal background] -> { Colors     = List.map (function StructVal color -> Standard.ColorRGBA.FromMessage color | _ -> malformed ()) colors
                                                                                                        Background = Standard.ColorRGBA.FromMessage background } | _ -> malformed ()
                let ToMessage { Colors = colors
                                Background = background } = ["colors",   VariableArrayVal (Seq.map (Standard.ColorRGBA.ToMessage >> Seq.toList >> StructVal) colors |> Seq.toList)
                                                             "bg_color", StructVal (Standard.ColorRGBA.ToMessage background |> Seq.toList)] |> Seq.ofList
            module ActionGoal =
                let Message = { Type   = "naoqi_brige_msgs/BlinkActionGoal"
                                MD5    = "8fb9f71a23feed1923381dc04a3cab38"
                                Fields = ["header",          StructDef Standard.Header.Def.Fields
                                          "goal_id",         StructDef ActionLib.GoalID.Def.Fields
                                          "goal",            StructDef Goal.Message.Fields
                                          "blink_duration",  DurationDef
                                          "blink_rate_mean", Float32Def
                                          "blink_rate_sd",   Float32Def] }
                type Kind = { Header:        Standard.Header.Kind
                              GoalID:        ActionLib.GoalID.Kind
                              Goal:          Goal.Kind
                              BlinkDuration: TimeSpan
                              BlinkRateMean: single
                              BlinkRateSD:   single }
                let FromMessage m = m |> Seq.toList |> function ["header",          StructVal   header
                                                                 "goal_id",         StructVal   id
                                                                 "goal",            StructVal   goal
                                                                 "blink_duration",  DurationVal (sec, nsec)
                                                                 "blink_rate_mean", Float32Val  mean
                                                                 "blink_rate_sd",   Float32Val  sd] -> { Header        = Standard.Header.FromMessage header
                                                                                                         GoalID        = ActionLib.GoalID.FromMessage id
                                                                                                         Goal          = Goal.FromMessage goal
                                                                                                         BlinkDuration = toTimeSpan sec nsec
                                                                                                         BlinkRateMean = mean
                                                                                                         BlinkRateSD   = sd } | _ -> malformed ()
                let ToMessage { Header        = header
                                GoalID        = id
                                Goal          = goal
                                BlinkDuration = duration
                                BlinkRateMean = mean
                                BlinkRateSD   = sd } = ["header",          StructVal (Standard.Header.ToMessage header |> Seq.toList)
                                                        "goal_id",         StructVal (ActionLib.GoalID.ToMessage id |> Seq.toList)
                                                        "goal",            StructVal (Goal.ToMessage goal |> Seq.toList)
                                                        "blink_duration",  DurationVal (fromTimeSpan duration)
                                                        "blink_rate_mean", Float32Val mean
                                                        "blink_rate_sd",   Float32Val sd] |> Seq.ofList

    module Psi =

        module Cmd =
            let Def = { Type   = "psi/Cmd"
                        MD5    = "671f8e4998eaec79f1c47e339dfd527b"
                        Fields = ["data", StringDef] }
            type Kind = string
            let FromMessage m = m |> Seq.toList |> function ["data", StringVal str] -> str | _ -> malformed ()
            let ToMessage str = ["data", StringVal str] |> Seq.ofList


    module AudioCommon =
        module AudioData =
            let Def = { Type   = "audio_common_msgs/AudioData"
                        MD5    = "f43a8e1b362b75baa741461b46adc7e0";
                        Fields = ["data", VariableArrayDef UInt8Def]}

            type Kind = { Data : uint8 seq }
            let FromMessage m = m |> Seq.toList |> function ["data", VariableArrayVal data] -> {Data = List.map (function UInt8Val n -> n | _ -> malformed ()) data}| _ -> malformed ()          
            let ToMessage {Data = data } = ["data", VariableArrayVal (Seq.map UInt8Val data |> Seq.toList)]

        module AudioInfo =
            let Def = { Type   = "audio_common_msgs/AudioInfo"
                        MD5    = "9413d9b7029680d3b1db6ed0ae535f88";
                        Fields = ["channels", UInt8Def
                                  "sample_rate", UInt32Def
                                  "sample_format", StringDef
                                  "bitrate", UInt32Def
                                  "coding_format", StringDef]}
                     
            type Kind = { Channels:     uint8 
                          SampleRate:   uint32 
                          SampleFormat: string 
                          Bitrate:      uint32 
                          CodingFormat: string }
            let FromMessage m = m |> Seq.toList |> function ["channels",        UInt8Val   channels
                                                             "sample_rate",     UInt32Val  rate
                                                             "sample_format",   StringVal  format
                                                             "bitrate",         UInt32Val  bitrate
                                                             "coding_format",   StringVal  coding] -> { Channels     = channels
                                                                                                        SampleRate   = rate
                                                                                                        SampleFormat = format
                                                                                                        Bitrate      = bitrate
                                                                                                        CodingFormat = coding } |_ -> malformed ()
                                                   
            let ToMessage { Channels     = channels
                            SampleRate   = rate
                            SampleFormat = format
                            Bitrate      = bitrate
                            CodingFormat = coding  } = ["channels",         UInt8Val   channels
                                                        "sample_rate",      UInt32Val  rate
                                                        "sample_format",    StringVal  format
                                                        "bitrate",          UInt32Val  bitrate
                                                        "coding_format",    StringVal  coding] |> Seq.ofList
