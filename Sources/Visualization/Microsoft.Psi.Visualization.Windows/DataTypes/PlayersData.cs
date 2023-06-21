// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Numerics;
using PipelineRejeuxDonnees;

#pragma warning disable

[System.Serializable]
public class PlayersData
{
    public PositionData position1;
    public PositionData position2;
    public RotationData rotation1;
    public RotationData rotation2;
    public bool vad1;
    public bool vad2;
    public JVAData jvaEvent;

    public PlayersData(PositionData pos1, PositionData pos2, RotationData rot1, RotationData rot2, bool v1, bool v2, JVAData jva)
    {
        position1 = pos1;
        position2 = pos2;
        rotation1 = rot1;
        rotation2 = rot2;
        vad1 = v1;
        vad2 = v2;
        jvaEvent = jva;
    }
}
