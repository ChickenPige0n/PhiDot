using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Godot;
using static Phidot.ChartStructure.Easings;


namespace Phidot.ChartStructure
{

    public class BPMListItem
    {
        [JsonPropertyName("bpm")]
        public float BPM { get; set; }
        [JsonPropertyName("startTime")]
        public Time StartTime { get; set; }
    }
    public class META
    {
        public int RPEVersion { get; set; }

        [JsonPropertyName("background")]
        public string Background { get; set; }
        [JsonPropertyName("charter")]
        public string Charter { get; set; }
        [JsonPropertyName("composer")]
        public string Composer { get; set; }
        [JsonPropertyName("id")]
        public string ID { get; set; }
        [JsonPropertyName("level")]
        public string Difficulty { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("offset")]
        public int Offset { get; set; }
        [JsonPropertyName("song")]
        public string MusicFileName { get; set; }
    }
    public class AlphaControlItem
    {
        public AlphaControlItem(float xx)
        {
            Alpha = 1.0f;
            Easing = 1;
            X = xx;
        }
        [JsonPropertyName("alpha")]
        public float Alpha { get; set; }
        [JsonPropertyName("easing")]
        public int Easing { get; set; }
        [JsonPropertyName("x")]
        public float X { get; set; }
    }
    public class RPEEvent
    {
        public RPEEvent()
        {
            LinkGroup = 0;
            EasingLeft = 0;
            EasingRight = 1;
            Bezier = 0;
            BezierPoints = new BezierPoints() { 0.0f, 0.0f, 0.0f, 0.0f };
        }

        public double GetCurVal(double t)
        {
            if (t > StartTime && t < EndTime)
            {
                var easeResult = easeFuncs[EasingType]((t - StartTime) / (EndTime - StartTime));
                return (Start * (1 - easeResult)) + (End * easeResult);
            }
            else if (Math.Abs(t - StartTime) <= 0.01 || Math.Abs(t - EndTime) <= 0.01)
            {
                return Math.Abs(t - StartTime) <= 0.01 ? Start : End;
            }
            else
            {
                return End;
            }
        }

        [JsonPropertyName("bezier")]
        public int Bezier { get; set; }
        [JsonPropertyName("bezierPoints")]
        public BezierPoints BezierPoints { get; set; }
        [JsonPropertyName("easingLeft")]
        public float EasingLeft { get; set; }
        [JsonPropertyName("easingRight")]
        public float EasingRight { get; set; }
        [JsonPropertyName("easingType")]
        public int EasingType { get; set; }
        [JsonPropertyName("end")]
        public float End { get; set; }
        [JsonPropertyName("endTime")]
        public Time EndTime { get; set; }
        [JsonPropertyName("linkgroup")]
        public int LinkGroup { get; set; }
        [JsonPropertyName("start")]
        public float Start { get; set; }
        [JsonPropertyName("startTime")]
        public Time StartTime { get; set; }
    }



    /// <summary>
    /// 四次方贝塞尔缓动类型
    /// </summary>
    public class BezierPoints : List<float>
    {

    }


    /// <summary>
    /// RPE带分数时间
    /// </summary>
    public class Time : List<int>, IComparable
    {
        public int CompareTo(object b)
        {
            return (int)(this - (Time)b);
        }
        public double AsDouble()
        {
            return this[2] == 0 ? 0 : (double)this[0] + ((double)this[1] / (double)this[2]);
        }

        public static implicit operator double(Time time)
        {
            return time[2] == 0 ? 0 : (double)time[0] + ((double)time[1] / (double)time[2]);
        }

        /// <summary>
        /// 将小数拍数转为带分数
        /// </summary>
        /// <param name="value">拍数的小数</param>
        public static explicit operator Time(double value)
        {

            int wholePart = (int)value;
            double fractionPart = value - wholePart;
            int numerator = (int)Math.Round(fractionPart * 1000000);
            int denominator = 1000000;

            // 约分分子和分母
            int gcd = GetGcd(numerator, denominator);
            numerator /= gcd;
            denominator /= gcd;


            return new Time() { wholePart, numerator, denominator };
        }
        private static int GetGcd(int a, int b)
        {
            return b == 0 ? a : GetGcd(b, a % b);
        }

        [JsonIgnore]
        public double RealTime;
    }


    public class RPESpeedEvent
    {
        [JsonPropertyName("end")]
        public float End { get; set; }
        [JsonPropertyName("endTime")]
        public Time EndTime { get; set; }

        [JsonPropertyName("linkgroup")]
        public int LinkGroup { get; set; }
        [JsonPropertyName("start")]
        public float Start { get; set; }
        [JsonPropertyName("startTime")]
        public Time StartTime { get; set; }

        [JsonIgnore]
        public double floorPosition = 0;

    }

    public class EventList : List<RPEEvent>
    {
        public EventList() { }
        public EventList(bool WithBase = false)
        {
            if (WithBase)
            {
                Add(new RPEEvent()
                {
                    EasingType = 1,
                    StartTime = new Time() { 0, 0, 1 },
                    EndTime = new Time() { 1, 0, 1 },
                    Start = 0,
                    End = 0
                });
            }
        }
        [JsonIgnore]
        public int CurAt { get; set; } = -1;
        /// <summary>
        /// 计算拍数对应的事件值
        /// </summary>
        /// <param name="time">当前拍数</param>
        /// <returns></returns>
        public double GetValByTime(double time)
        {

            foreach (RPEEvent e in this)
            {
                var i = this.IndexOf(e);
                double nextEnd = i == this.Count - 1 ? 99999.0d : this[i + 1].StartTime;

                if (time >= e.StartTime && time <= nextEnd)
                {
                    return e.GetCurVal(time);
                }
            }
            return 0;
        }
    }


    public class SpeedEventList : List<RPESpeedEvent>
    {

        public SpeedEventList() { }
        public SpeedEventList(bool WithBase = false)
        {
            if (WithBase)
            {
                Add(new RPESpeedEvent()
                {
                    StartTime = new Time() { 0, 0, 1 },
                    EndTime = new Time() { 1, 0, 1 },
                    Start = 0,
                    End = 0
                });
            }
        }
        public void CalcFloorPosition()
        {
            foreach (RPESpeedEvent lastEvent in this)
            {
                int i = this.IndexOf(lastEvent);
                if (i == Count - 1) break;
                var curEvent = this[i + 1];

                double lastStartTime = lastEvent.StartTime.RealTime;
                double lastEndTime = lastEvent.EndTime.RealTime;

                double curStartTime = curEvent.StartTime.RealTime;


                curEvent.floorPosition +=
                lastEvent.floorPosition + (lastEvent.End + lastEvent.Start) * (lastEndTime - lastStartTime) / 2 +
                lastEvent.End * (curStartTime - lastEndTime) / 1;

            }
        }

        /// <summary>
        /// 获取当前时间的速度积分
        /// </summary>
        /// <param name="bPMList"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetCurTimeSu(double time)
        {
            double floorPosition = 0.0d;
            foreach (RPESpeedEvent speedEvent in this)
            {
                double StartTime = speedEvent.StartTime.RealTime;
                double EndTime = speedEvent.EndTime.RealTime;

                int i = IndexOf(speedEvent);
                if (time == speedEvent.StartTime.RealTime)
                {
                    floorPosition += speedEvent.floorPosition;
                    break;
                }
                else if (time <= speedEvent.EndTime.RealTime)
                {
                    floorPosition += speedEvent.floorPosition +
                    (speedEvent.Start + (speedEvent.End - speedEvent.Start) *
                    (time - StartTime) / (EndTime - StartTime) +
                    speedEvent.Start) * (time - StartTime) / 2;
                    break;
                }
                else if (Count - 1 == i || time <= this[i + 1].StartTime.RealTime)
                {
                    floorPosition += speedEvent.floorPosition + (speedEvent.End + speedEvent.Start) * (EndTime - StartTime) / 2 +
                    speedEvent.End * (time - EndTime) / 1;
                    break;
                }
            }

            return floorPosition;
        }

    }


    public class EventLayer
    {
        [JsonIgnore]
        private EventList _alphaEvents;
        [JsonIgnore]
        private EventList _moveXEvents;
        [JsonIgnore]
        private EventList _moveYEvents;
        [JsonIgnore]
        private EventList _rotateEvents;
        [JsonIgnore]
        private SpeedEventList _speedEvents;


        [JsonPropertyName("alphaEvents")]
        public EventList AlphaEvents
        {
            get { return _alphaEvents; }
            set { _alphaEvents = value; }
        }
        [JsonPropertyName("moveXEvents")]
        public EventList MoveXEvents
        {
            get { return _moveXEvents; }
            set { _moveXEvents = value; }
        }

        [JsonPropertyName("moveYEvents")]
        public EventList MoveYEvents
        {
            get { return _moveYEvents; }
            set { _moveYEvents = value; }
        }

        [JsonPropertyName("rotateEvents")]
        public EventList RotateEvents
        {
            get { return _rotateEvents; }
            set { _rotateEvents = value; }
        }

        [JsonPropertyName("speedEvents")]
        public SpeedEventList SpeedEvents
        {
            get { return _speedEvents; }
            set { _speedEvents = value; }
        }
    }
    public class EventLayerList : List<EventLayer>
    {
        /// <summary>
		/// 获取时间范围内速度积分
		/// </summary>
		/// <param name="startTime"></param>
		/// <param name="time"></param>
		/// <param name="factor"></param>
		/// <returns>单位：屏幕高度</returns>
		public float GetCurSu(double time)
        {
            double result = 0;
            foreach (var Layer in this)
            {
                result += Layer.SpeedEvents.GetCurTimeSu(time);
            }
            return (float)result;
        }
    }


    public class Extended
    {
        public Extended()
        {
            RPEEvent defaultInc = new RPEEvent
            {
                Start = 0.0f,
                End = 0.0f,
                EasingType = 1,
                StartTime = new Time { 0, 0, 1 },
                EndTime = new Time { 1, 0, 1 }
            };
            inclineEvents = new EventList
            {
                defaultInc
            };
        }
        public EventList inclineEvents { get; set; }
    }


    public class NoteEndTimeComparer : Comparer<RPENote>
    {
        public override int Compare(RPENote x, RPENote y)
        {
            if (x.EndTime.RealTime > y.EndTime.RealTime) return 1;
            else if (x.EndTime.RealTime < y.EndTime.RealTime) return -1;
            else return 0;
        }
    }
    public class NoteStartTimeComparer : Comparer<RPENote>
    {
        public override int Compare(RPENote x, RPENote y)
        {
            if (x.StartTime.RealTime > y.StartTime.RealTime) return 1;
            else if (x.StartTime.RealTime < y.StartTime.RealTime) return -1;
            else return 0;
        }
    }


    public enum NoteType : int
    {
        Tap = 1,
        Hold = 2,
        Flick = 3,
        Drag = 4,
    }
    public class RPENote
    {
        [JsonPropertyName("above")]
        public int Above { get; set; }
        [JsonPropertyName("alpha")]
        public int Alpha { get; set; }
        [JsonPropertyName("endTime")]
        public Time EndTime { get; set; }
        [JsonPropertyName("isFake")]
        public int IsFake { get; set; }
        [JsonPropertyName("positionX")]
        public float PositionX { get; set; }
        [JsonPropertyName("size")]
        public float Size { get; set; }
        [JsonPropertyName("speed")]
        public float Speed { get; set; }
        [JsonPropertyName("startTime")]
        public Time StartTime { get; set; }
        [JsonPropertyName("type")]
        public NoteType Type { get; set; }
        [JsonPropertyName("visibleTime")]
        public float VisibleTime { get; set; }
        [JsonPropertyName("yOffset")]
        public float YOffset { get; set; }

        [JsonIgnore]
        public double FloorPosition;
        [JsonIgnore]
        public double CeliPosition;
        [JsonIgnore]
        public bool IsHighLight;
    }
    public enum JudgeState{
        NotJudged,
        Holding,
        Judged,
        Dead
    }

    public class PosControlItem
    {
        public PosControlItem(float xx)
        {
            Pos = 1.0f;
            Easing = 1;
            X = xx;
        }
        [JsonPropertyName("easing")]
        public int Easing { get; set; }
        [JsonPropertyName("pos")]
        public float Pos { get; set; }
        [JsonPropertyName("x")]
        public float X { get; set; }
    }
    public class SizeControlItem
    {
        public SizeControlItem(float xx)
        {
            Size = 1.0f;
            Easing = 1;
            X = xx;
        }
        [JsonPropertyName("easing")]
        public int Easing { get; set; }
        [JsonPropertyName("size")]
        public float Size { get; set; }
        [JsonPropertyName("x")]
        public float X { get; set; }
    }
    public class SkewControlItem
    {
        public SkewControlItem(float xx)
        {
            Skew = 0.0f;
            Easing = 1;
            X = xx;
        }
        [JsonPropertyName("easing")]
        public int Easing { get; set; }
        [JsonPropertyName("skew")]
        public float Skew { get; set; }
        [JsonPropertyName("x")]
        public float X { get; set; }
    }
    public class YControlItem
    {
        public YControlItem(float xx)
        {
            Y = 1.0f;
            Easing = 1;
            X = xx;
        }
        [JsonPropertyName("easing")]
        public int Easing { get; set; }
        [JsonPropertyName("y")]
        public float Y { get; set; }
        [JsonPropertyName("x")]
        public float X { get; set; }
    }
    public class JudgeLineJson
    {
        public int @Group { get; set; }
        public string Name { get; set; }
        public string Texture { get; set; }

        [JsonPropertyName("alphaControl")]
        public List<AlphaControlItem> AlphaControl { get; set; }
        [JsonPropertyName("bpmfactor")]
        public float BPMFactor { get; set; }
        [JsonPropertyName("eventLayers")]
        public EventLayerList EventLayers { get; set; }
        [JsonPropertyName("extended")]
        public Extended Extended { get; set; }
        [JsonPropertyName("father")]
        public int Father { get; set; }
        [JsonPropertyName("isCover")]
        public int IsCover { get; set; }
        [JsonPropertyName("notes")]
        public List<RPENote> Notes { get; set; }
        [JsonPropertyName("numOfNotes")]
        public int NumOfNotes { get; set; }
        [JsonPropertyName("posControl")]
        public List<PosControlItem> PosControl { get; set; }
        [JsonPropertyName("sizeControl")]
        public List<SizeControlItem> SizeControl { get; set; }
        [JsonPropertyName("skewControl")]
        public List<SkewControlItem> SkewControl { get; set; }
        [JsonPropertyName("yControl")]
        public List<YControlItem> YControl { get; set; }
        [JsonPropertyName("zOrder")]
        public int ZOrder { get; set; }
    }

    public static class IEnumerableExtended
    {
        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }
    }
    public enum JudgeType
    {
        Perfect,
        Good,
        Bad,
        Miss
    }


    public class JudgeManager
    {
        public int MaxCombo = 0;
        public int PerfectCount = 0;
        public int GoodCount = 0;
        public int MissCount = 0;
        public int NoteSum = 0;
        public JudgeType judgeStatus = JudgeType.Perfect;

        public void Judge(JudgeType type)
        {
            switch (type)
            {
                case JudgeType.Perfect:
                    PerfectCount += 1;
                    MaxCombo += 1;
                    break;
                case JudgeType.Good:
                    GoodCount += 1;
                    MaxCombo += 1;
                    if (judgeStatus == JudgeType.Perfect) judgeStatus = JudgeType.Good;
                    break;
                case JudgeType.Miss:
                    MissCount += 1;
                    MaxCombo = 0;
                    judgeStatus = JudgeType.Miss;
                    break;
            }
        }

        public double GetAcc()
        {
            return (0.65d * GoodCount + PerfectCount) / NoteSum;
        }

        public int CalcScore()
        {
            return (int)
                    (
                    1000000 *
                    ((0.1d * MaxCombo / NoteSum) +
                    0.9d * GetAcc())
                    );
        }
        public void RevertJudge()
        {
            MaxCombo = 0;
            PerfectCount = 0;
            GoodCount = 0;
            MissCount = 0;
            judgeStatus = JudgeType.Perfect;
        }
    }


    public class ChartRPE
    {
        public static Vector2 RPEPos2PixelPos(Vector2 RPEPos, Vector2 StagePixelSize)
        {
            RPEPos.X *= StagePixelSize.X / 1350.0f;
            RPEPos.Y *= -StagePixelSize.Y / 900.0f;
            return RPEPos;
        }


        public List<BPMListItem> BPMList;

        public META META { get; set; }
        [JsonPropertyName("judgeLineGroup")]
        public List<string> JudgeLineGroup { get; set; }
        [JsonPropertyName("judgeLineList")]
        public List<JudgeLineJson> JudgeLineList { get; set; }


        [JsonIgnore]
        public JudgeManager JudgeData = new();

        /// <summary>
        /// 预计算一些数值
        /// </summary>
        public void PreCalculation()
        {

            var allNotes = new List<RPENote>();
            foreach (var line in JudgeLineList)
            {
                JudgeData.NoteSum += line.NumOfNotes;
                // RealTime Calculate.
                foreach (var note in line.Notes.OrEmptyIfNull())
                {
                    note.StartTime.RealTime = BeatTime2RealTime(note.StartTime);
                    note.EndTime.RealTime = BeatTime2RealTime(note.EndTime);
                    allNotes.Add(note);
                }
                foreach (var layer in line.EventLayers.OrEmptyIfNull())
                {
                    layer.SpeedEvents ??= new SpeedEventList(true);
                    layer.MoveXEvents ??= new EventList(WithBase: true);
                    layer.MoveYEvents ??= new EventList(WithBase: true);
                    layer.MoveYEvents ??= new EventList(WithBase: true);
                    layer.AlphaEvents ??= new EventList(WithBase: true);
                    layer.RotateEvents ??= new EventList(WithBase: true);

                    foreach (var e in layer.SpeedEvents.OrEmptyIfNull())
                    {
                        e.StartTime.RealTime = BeatTime2RealTime(e.StartTime);
                        e.EndTime.RealTime = BeatTime2RealTime(e.EndTime);
                    }
                    layer.SpeedEvents.CalcFloorPosition();
                }

                foreach (var note in line.Notes.OrEmptyIfNull())
                {
                    foreach (var layer in line.EventLayers)
                    {
                        note.FloorPosition += layer.SpeedEvents.GetCurTimeSu(note.StartTime.RealTime);
                        note.CeliPosition += layer.SpeedEvents.GetCurTimeSu(note.EndTime.RealTime);
                    }
                }
            }
            allNotes.Sort(new NoteStartTimeComparer());
            foreach (var curNote in allNotes)
            {

                var i = allNotes.IndexOf(curNote);
                if
                (
                    (i < allNotes.Count - 1 && Math.Abs(allNotes[i + 1].StartTime - curNote.StartTime) <= 0.01)
                    ||
                    (i > 0 && Math.Abs(allNotes[i - 1].StartTime - curNote.StartTime) <= 0.01)
                )
                {
                    curNote.IsHighLight = true;
                }
            }
        }

        /// <summary>
        /// 将拍数转换为秒数
        /// </summary>
        public double BeatTime2RealTime(double beatTime)
        {
            var bPMList = this.BPMList;
            List<double> bpmSeconds = new List<double>();
            foreach (BPMListItem BPMInfo in bPMList)
            {
                int i = bPMList.IndexOf(BPMInfo);
                if (i < bPMList.Count - 1)
                {
                    double dBeat = bPMList[i + 1].StartTime - BPMInfo.StartTime;
                    bpmSeconds.Add(dBeat * 60 / BPMInfo.BPM);
                }
                else
                {
                    bpmSeconds.Add(999999.0);
                }
            }

            double SecondSum = 0.0d;
            foreach (BPMListItem BPMInfo in bPMList)
            {
                int i = bPMList.IndexOf(BPMInfo);
                if (i == bPMList.Count - 1 || bPMList[i + 1].StartTime >= beatTime)
                {
                    SecondSum += (beatTime - BPMInfo.StartTime) * 60 / BPMInfo.BPM;
                }
                else
                {
                    SecondSum += bpmSeconds[i];
                    break;
                }
            }
            return SecondSum;

        }

        /// <summary>
        /// 将秒数转换为拍数
        /// </summary>
        public double RealTime2BeatTime(double realTime)
        {
            List<double> bpmSeconds = new List<double>();
            foreach (BPMListItem BPMInfo in BPMList)
            {
                int i = BPMList.IndexOf(BPMInfo);
                if (i < BPMList.Count - 1)
                {
                    double dBeat = BPMList[i + 1].StartTime - BPMInfo.StartTime;
                    bpmSeconds.Add(dBeat * 60 / BPMInfo.BPM);
                }
                else
                {
                    bpmSeconds.Add(999999.0);
                }
            }



            double second = 0.0;
            double last = 0.0;
            foreach (double t in bpmSeconds)
            {
                second += t;
                if (t >= realTime)
                {
                    double timeInBPMRange = realTime - last;
                    var curBPMInfo = BPMList[bpmSeconds.IndexOf(t)];
                    return (timeInBPMRange * curBPMInfo.BPM / 60) + curBPMInfo.StartTime;
                }
                last = second;
            }
            return 0;
        }
    }
}