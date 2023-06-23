// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable

namespace PipelineRejeuxDonnees
{
    using System;
    using System.Numerics;
    using Microsoft.Psi;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;
    using Microsoft.Psi.Components;

    [System.Serializable]
    public class DatainRT
    {
        public List<PositionData> pos1 = new List<PositionData>();
        public List<PositionData> pos2 = new List<PositionData>();
        public List<RotationData> rot1 = new List<RotationData>();
        public List<RotationData> rot2 = new List<RotationData>();
        public List<EventData> other1 = new List<EventData>();
        public List<EventData> gaze1 = new List<EventData>();
        public List<EventData> ungaze1 = new List<EventData>();
        public List<EventData> hover1 = new List<EventData>();
        public List<EventData> unhover1 = new List<EventData>();
        public List<EventData> select1 = new List<EventData>();
        public List<EventData> unselect1 = new List<EventData>();
        public List<EventData> activateFindable1 = new List<EventData>();
        public List<EventData> activatePositionning1 = new List<EventData>();
        public List<EventData> gazeAvatar1In = new List<EventData>();
        public List<EventData> gazeAvatar1Out = new List<EventData>();
        public List<EventData> validationPuzzle1 = new List<EventData>();

        public List<EventData> other2 = new List<EventData>();
        public List<EventData> gaze2 = new List<EventData>();
        public List<EventData> ungaze2 = new List<EventData>();
        public List<EventData> hover2 = new List<EventData>();
        public List<EventData> unhover2 = new List<EventData>();
        public List<EventData> select2 = new List<EventData>();
        public List<EventData> unselect2 = new List<EventData>();
        public List<EventData> activateFindable2 = new List<EventData>();
        public List<EventData> activatePositionning2 = new List<EventData>();
        public List<EventData> gazeAvatar2In = new List<EventData>();
        public List<EventData> gazeAvatar2Out = new List<EventData>();
        public List<EventData> validationPuzzle2 = new List<EventData>();

        public EventData gazeEvent2;
        public EventData ungazeEvent2;


        public List<EventData> allgaze1 = new List<EventData>();
        public List<EventData> allungaze1 = new List<EventData>();
        public List<EventData> allhover1 = new List<EventData>();
        public List<EventData> allunhover1 = new List<EventData>();
        public List<EventData> allselect1 = new List<EventData>();
        public List<EventData> allunselect1 = new List<EventData>();
        public List<EventData> allactivate1 = new List<EventData>();
        public List<EventData> allgazeAvatar1 = new List<EventData>();


        public List<EventData> allgaze2 = new List<EventData>();
        public List<EventData> allungaze2 = new List<EventData>();
        public List<EventData> allhover2 = new List<EventData>();
        public List<EventData> allunhover2 = new List<EventData>();
        public List<EventData> allselect2 = new List<EventData>();
        public List<EventData> allunselect2 = new List<EventData>();
        public List<EventData> allactivate2 = new List<EventData>();
        public List<EventData> allgazeAvatar2 = new List<EventData>();

        public List<EventData> puzzleInteraction1 = new List<EventData>();
        public List<EventData> puzzleInteraction2 = new List<EventData>();

        //public List<TimeData> gazeAvatar1Time = new List<TimeData>();
        //public List<TimeData> gazeAvatar2Time = new List<TimeData>();
        public List<TimeData>[] gazeAvatarTime = new List<TimeData>[numParticipants];
        public List<TimeData>[] gazeAvatarTime250 = new List<TimeData>[numParticipants];
        public List<TimeData>[] gazeAvatarEvents150 = new List<TimeData>[numParticipants];
        public List<TimeData>[] gazeAvatarEvents250 = new List<TimeData>[numParticipants];
        public int[] mutualGazeCount = new int[numParticipants];
        public int[] mutualGazeCount250 = new int[numParticipants];
        public List<TimeData>[] gazeTime = new List<TimeData>[numParticipants];
        public List<TimeData>[] gazeTime250 = new List<TimeData>[numParticipants];

        public List<CollabEventData> collabhoverdata = new List<CollabEventData>();
        public List<CollabEventData> collabgazedata = new List<CollabEventData>();
        public List<TimeData> collabhovertime = new List<TimeData>();
        public List<TimeData> collabgazetime = new List<TimeData>();
        public List<JVAData> jvaEvent1 = new List<JVAData>();
        public List<JVAData> jvaEvent2 = new List<JVAData>();
        public List<JVAData> jvaEvents = new List<JVAData>();
        public List<JVAData> jvaEvents250 = new List<JVAData>();
        public int minFixationTime150 = 200;
        public int minFixationTime250 = 250;
        int jva1;
        int jva2;

        public List<DateTime> startSpeakingP1 = new List<DateTime>();
        public List<DateTime> endSpeakingP1 = new List<DateTime>();
        public List<DateTime> startSpeakingP2 = new List<DateTime>();
        public List<DateTime> endSpeakingP2 = new List<DateTime>();
        public List<TimeData> speakingTimeP1 = new List<TimeData>();
        public List<TimeData> speakingTimeP2 = new List<TimeData>();
        public List<double> speakingTimeRepartitionOverTime = new List<double> { 0, 0 };
        //public List<ChangeSpeakingTimeRepartitionData> changeSpeakingTimeRepartition = new List<ChangeSpeakingTimeRepartitionData>();
        public List<SpeakingRepartitionData> speakingTimeRepartition = new List<SpeakingRepartitionData>();
        public bool _isOneHigherRepartition = false;
        public bool _isTwoHigherRepartition = false;
        public bool _isFirstTalk = false;
        public double totalspeakingTime1;
        public double totalspeakingTime2;
        public List<VadData> vad1 = new List<VadData>();
        public List<VadData> vad2 = new List<VadData>();

        static public int numParticipants = 2;
        public DateTime[] turnEndTimes = new DateTime[numParticipants];
        public int[] turnNumInterventions = new int[numParticipants];
        public int[] turnWithOverlapCount = new int[numParticipants];
        public int[] turnWithoutOverlapCount = new int[numParticipants];
        public int[] overlapCount = new int[numParticipants];
        public int[] jvaCount = new int[numParticipants];
        public int[] jvaCount250 = new int[numParticipants];
        public List<TurnTakingOverlapData> turnTakingOverlap = new List<TurnTakingOverlapData>();
        public List<JVAWindow> jvaPerWindow = new List<JVAWindow>();
        DateTime[] participantSpeechStartTimes = new DateTime[numParticipants];
        DateTime[] participantSpeechEndTimes = new DateTime[numParticipants];
        public List<SttTimeData> lastparticipantSpeechStartTimes = new List<SttTimeData>();
        public List<SttTimeData> lastparticipantSpeechEndTimes = new List<SttTimeData>();
        public int lastSpeaker = 3;

        public bool IsGazeAvatar1 = false;
        public bool IsSpeakingP1 = false;
        public bool IsGazeAvatar2 = false;
        public bool IsSpeakingP2 = false;
        public double gazeAttentionnal1;
        public double gazeAttentionnal2;
        DateTime IsGaze1;
        DateTime IsGaze2;
        private TimeSpan removeMsVad = new TimeSpan(0, 0, 0, 0, 300);

        public List<string> datacsvsave = new List<string>();
        public List<string> datacsvindicators = new List<string>();

        static TakenAndPositionnedPieceData Tbloc = new TakenAndPositionnedPieceData(DateTime.MinValue, DateTime.MinValue, "TBloc", false, false);
        static TakenAndPositionnedPieceData Lbloc = new TakenAndPositionnedPieceData(DateTime.MinValue, DateTime.MinValue, "LBloc", false, false);
        static TakenAndPositionnedPieceData Sbloc = new TakenAndPositionnedPieceData(DateTime.MinValue, DateTime.MinValue, "SBloc", false, false);
        static TakenAndPositionnedPieceData Zbloc = new TakenAndPositionnedPieceData(DateTime.MinValue, DateTime.MinValue, "ZBloc", false, false);
        static TakenAndPositionnedPieceData Linebloc = new TakenAndPositionnedPieceData(DateTime.MinValue, DateTime.MinValue, "LineBloc", false, false);
        static TakenAndPositionnedPieceData Cubebloc = new TakenAndPositionnedPieceData(DateTime.MinValue, DateTime.MinValue, "CubeBloc", false, false);
        static TakenAndPositionnedPieceData Jbloc = new TakenAndPositionnedPieceData(DateTime.MinValue, DateTime.MinValue, "JBloc", false, false);
        public List<TakenAndPositionnedPieceData> positionnedPiece = new List<TakenAndPositionnedPieceData>() { Tbloc, Lbloc, Sbloc, Zbloc, Linebloc, Cubebloc, Jbloc };

    }

    [System.Serializable]
    public class JVAData
    {
        public DateTime startTimejvainitiator;
        public DateTime endTimejvainitiator;
        public DateTime startTimejvaresponder;
        public DateTime endTimejvaresponder;
        public TimeSpan durationTime;
        public string objectID;
        public int initiator;
        public int responder;

        public JVAData(DateTime stinit, DateTime etinit, DateTime stresp, DateTime etresp, TimeSpan duration, string id, int init, int resp)
        {
            startTimejvainitiator = stinit;
            endTimejvainitiator = etinit;
            startTimejvaresponder = stresp;
            endTimejvaresponder = etresp;
            durationTime = duration;
            objectID = id;
            initiator = init;
            responder = resp;
        }

        public string GetToString()
        { return "JVA: " + this.objectID; }
    }
    [System.Serializable]
    public class JVAWindow
    {
        DateTime originatingTime;
        public int jva1;
        public int jva2;
        public int jva1_250;
        public int jva2_250;

        public JVAWindow (DateTime time, int j1, int j2, int j1250, int j2250)
        {
            originatingTime = time;
            jva1 = j1;
            jva2 = j2;
            jva1_250 = j1250;
            jva2_250 = j2250;
        }
    }

    [System.Serializable]
    public class SttTimeData
    {
        public List<DateTime> time = new List<DateTime>();
        
    }
    [System.Serializable]
    public class VadData
    {
        public bool isVADActivity;
        public DateTime time;
        
        public VadData(bool v, DateTime t)
        {
            isVADActivity = v;
            time = t;
        }
    }

    [System.Serializable]
    public class TurnTakingOverlapData
    {
        public int TTwithoutoverlap1;
        public int TTwithoutoverlap2;
        public int TTwithoverlap1;
        public int TTwithoverlap2;
        public int overlap1;
        public int overlap2;
        public DateTime originatingTime;

        public TurnTakingOverlapData(int TTout1, int TTout2, int TTov1, int TTov2, int ov1, int ov2, DateTime ot)
        {
            TTwithoutoverlap1 = TTout1;
            TTwithoutoverlap2 = TTout2;
            TTwithoverlap1 = TTov1;
            TTwithoverlap2 = TTov2;
            overlap1 = ov1;
            overlap2 = ov2;
            originatingTime = ot;
        }
    }

    [System.Serializable]
    public class SpeakingRepartitionData
    {
        public double totaltime1;
        public double totaltime2;
        public double repartition1;
        public double repartition2;
        public int higherspeaker;
        public int lowerspeaker;
        public DateTime originatingTime;
        public double ratio1;
        public double ratio2;
        public double ratioSilence;
        public double pSilence1;
        public double pSilence2;


        public SpeakingRepartitionData(double t1, double t2, double r1, double r2, double rtot1, double rtot2, double rsilence,double ps1, double ps2, int hs, int ls, DateTime ot)
        {
            totaltime1 = t1;
            totaltime2 = t2;
            repartition1 = r1;
            repartition2 = r2;
            ratio1 = rtot1;
            ratio2 = rtot2;
            ratioSilence = rsilence;
            higherspeaker = hs;
            lowerspeaker = ls;
            originatingTime = ot;
            pSilence1 = ps1;
            pSilence2 = ps2;
        }
    }
    
    [System.Serializable]
    public class TakenAndPositionnedPieceData
    {
        public DateTime originatingTimeTaken;
        public DateTime originatingTimePositionned;
        string objectID;
        public bool taken;
        public bool positionned;

        public TakenAndPositionnedPieceData(DateTime ott, DateTime otp, string id, bool _isTaken, bool _isPositionned)
        {
            originatingTimeTaken = ott;
            originatingTimeTaken = otp;
            objectID = id;
            taken = _isTaken;
            positionned = _isPositionned;

        }
    }
    [System.Serializable]
    public class EventData
    {
        public DateTime originatingTime;
        public string type;
        public float time;
        public bool buttonValidation;
        public bool gaze;
        public string actionOnPieces;
        public int userID;
        public string objectID;
        public string blockState;
        public int step;
        public int userIDGazed;
        public int userIDGazer;
        public int interruptionNum;
        public bool interruptionSuccess;

        public EventData(string value)
        {
            var parts = value.Split(';');
            type = parts[0];
            time = float.Parse(parts[1]);
            buttonValidation = bool.Parse(parts[2]);
            gaze = bool.Parse(parts[3]);
            actionOnPieces = parts[4];
            userID = int.Parse(parts[5]);
            objectID = parts[6];
            blockState = parts[7];
            step = int.Parse(parts[8]);
            userIDGazed = int.Parse(parts[9]);
            userIDGazer = int.Parse(parts[10]);
            interruptionNum = int.Parse(parts[11]);
            interruptionSuccess = bool.Parse(parts[12]);
        }
    }
    public class CollabEventData
    {
        public DateTime originatingTime;
        public float time;
        public int firstUser;
        public int secondUser;
        public string collabAction;
        public bool isBegin;
        public bool requireSpecificCue;
        public string objectIfexist;

        public CollabEventData(string value)
        {
            var parts = value.Split(';');
            time = float.Parse(parts[0]);
            secondUser = int.Parse(parts[1]);
            collabAction = parts[2];
            isBegin = bool.Parse(parts[3]);
            requireSpecificCue = bool.Parse(parts[4]);
            objectIfexist = parts[5];
        }
    }
    [System.Serializable]
    public class PositionData
    {
        public DateTime originatingTime;
        public float deltatime;
        public int userID;
        public string headPos;
        public string lHandPos;
        public string rHandPos;
        public Vector3 headPosv;
        public Vector3 lHandPosv;
        public Vector3 rHandPosv;

        public PositionData(string value)
        {
            var parts = value.Split(';');
            deltatime = float.Parse(parts[0]);
            userID = int.Parse(parts[1]);
            headPos = parts[2];
            lHandPos = parts[3];
            rHandPos = parts[4];
        }

        public float X
        {
            get { return this.headPosv.X; }
        }
        public float Y
        {
            get { return this.headPosv.Y; }
        }

        public void ToVectHeadPos()
        {
            var partsHeadPos = headPos.Split('_');
            float xHeadpos = float.Parse(partsHeadPos[0]);
            float yHeadpos = float.Parse(partsHeadPos[1]);
            float zHeadpos = float.Parse(partsHeadPos[2]);
            headPosv = new Vector3(xHeadpos, yHeadpos, zHeadpos);
        }
        public void ToVectLeftHandPos()
        {
            var partsLhandPos = lHandPos.Split('_');
            float xLHandpos = float.Parse(partsLhandPos[0]);
            float yLHandpos = float.Parse(partsLhandPos[1]);
            float zLHandpos = float.Parse(partsLhandPos[2]);
            lHandPosv = new Vector3(xLHandpos, yLHandpos, zLHandpos);
        }
        public void ToVectRightHandPos()
        {
            var partsRhandPos = lHandPos.Split('_');
            float xRHandpos = float.Parse(partsRhandPos[0]);
            float yRHandpos = float.Parse(partsRhandPos[1]);
            float zRHandpos = float.Parse(partsRhandPos[2]);
            rHandPosv = new Vector3(xRHandpos, yRHandpos, zRHandpos);
        }
    }

    [System.Serializable]
    public class RotationData
    {
        public DateTime originatingTime;
        public float deltatime;
        public int userID;
        public string headRot;
        public string lHandRot;
        public string rHandRot;
        public Vector3 headRotv;
        public Vector3 lHandRotv;
        public Vector3 rHandRotv;

        public RotationData(string value)
        {
            var parts = value.Split(';');
            deltatime = float.Parse(parts[0]);
            userID = int.Parse(parts[1]);
            headRot = parts[2];
            lHandRot = parts[3];
            rHandRot = parts[4];
        }

        public float X
        {
            get { return this.headRotv.X; }
        }
        public float Y
        {
            get { return this.headRotv.Y; }
        }
        public float Z
        {
            get { return this.headRotv.Z; }
        }

        public void ToVectHeadRot()
        {
            var partsHeadRot = headRot.Split('_');
            float xHeadrot = float.Parse(partsHeadRot[0]);
            float yHeadrot = float.Parse(partsHeadRot[1]);
            float zHeadrot = float.Parse(partsHeadRot[2]);
            headRotv = new Vector3(xHeadrot, yHeadrot, zHeadrot);
        }
        public void ToVectLeftHandRot()
        {
            var partsLhandRot = lHandRot.Split('_');
            float xLHandrot = float.Parse(partsLhandRot[0]);
            float yLHandrot = float.Parse(partsLhandRot[1]);
            float zLHandrot = float.Parse(partsLhandRot[2]);
            lHandRotv = new Vector3(xLHandrot, yLHandrot, zLHandrot);
        }
        public void ToVectRightHandRot()
        {
            var partsRhandRot = rHandRot.Split('_');
            float xRHandrot = float.Parse(partsRhandRot[0]);
            float yRHandrot = float.Parse(partsRhandRot[1]);
            float zRHandrot = float.Parse(partsRhandRot[2]);
            rHandRotv = new Vector3(xRHandrot, yRHandrot, zRHandrot);
        }
    }
    [System.Serializable]
    public class TimeData
    {
        public DateTime startOriginatingTime;
        public DateTime endOriginatingTime;
        public double durationTime;
        public string text;
       
        public TimeData(DateTime startot, DateTime endot, double time, string txt)
        {
            startOriginatingTime = startot;
            endOriginatingTime = endot;
            durationTime = time;
            text = txt;
        }
    }
   
    class DataType
    {
    }
}