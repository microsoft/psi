// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable

using PipelineRejeuxDonnees;

[System.Serializable]
public class PlayersData
{
    public PositionData position;
    public RotationData rotation;
    public bool vad;
    public JVAData jvaEvent;

    public PlayersData(PositionData pos, RotationData rot, bool v, JVAData jva)
    {
        position = pos;
        rotation = rot;
        vad = v;
        jvaEvent = jva;
    }
}

