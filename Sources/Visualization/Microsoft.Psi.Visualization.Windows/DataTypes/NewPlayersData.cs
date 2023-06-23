// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable

using PipelineRejeuxDonnees;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms.VisualStyles;

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

    public PositionData Position
    {
        get { return this.position; }
    }

    public RotationData Rotation
    {
        get { return this.rotation; }
    }

    public bool Vad
    {
        get { return this.vad; }
    }

    public JVAData JVA
    {
        get { return this.jvaEvent; }
    }
}

