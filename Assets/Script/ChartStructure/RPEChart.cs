using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Godot;


public class BpmListItem
{
    [JsonPropertyName("bpm")] public float Bpm { get; set; }
    [JsonPropertyName("startTime")] public Time StartTime { get; set; }
}

public class Meta
{
    public int RpeVersion { get; set; }

    [JsonPropertyName("background")] public string Background { get; set; }
    [JsonPropertyName("charter")] public string Charter { get; set; }
    [JsonPropertyName("composer")] public string Composer { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("level")] public string Difficulty { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("offset")] public int Offset { get; set; }
    [JsonPropertyName("song")] public string MusicFileName { get; set; }
}

public class AlphaControlItem
{
    public AlphaControlItem(float xx)
    {
        Alpha = 1.0f;
        Easing = 1;
        X = xx;
    }

    [JsonPropertyName("alpha")] public float Alpha { get; set; }
    [JsonPropertyName("easing")] public int Easing { get; set; }
    [JsonPropertyName("x")] public float X { get; set; }
}

public class RpeEvent
{
    public short RelativeDist(double time)
    {
        if (StartTime <= time && time <= EndTime) return 0;
        if (EndTime < time) return 1;
        if (time < StartTime) return -1;
        return -1;
    }

    public double Eval(double t)
    {
        if (t > StartTime && t < EndTime)
        {
            var easeResult = Easings.EaseFunctions[EasingType]((t - StartTime) / (EndTime - StartTime));
            return (Start * (1 - easeResult)) + (End * easeResult);
        }

        if (Math.Abs(t - StartTime) <= 0.01 || Math.Abs(t - EndTime) <= 0.01)
        {
            return Math.Abs(t - StartTime) <= 0.01 ? Start : End;
        }

        return End;
    }

    [JsonPropertyName("bezier")] public int Bezier { get; set; } = 0;

    [JsonPropertyName("bezierPoints")]
    public BezierPoints BezierPoints { get; set; } = new() { 0.0f, 0.0f, 0.0f, 0.0f };

    [JsonPropertyName("easingLeft")] public float EasingLeft { get; set; } = 0;

    [JsonPropertyName("easingRight")] public float EasingRight { get; set; } = 1;

    [JsonPropertyName("easingType")] public int EasingType { get; set; }
    [JsonPropertyName("end")] public float End { get; set; }
    [JsonPropertyName("endTime")] public Time EndTime { get; set; }
    [JsonPropertyName("linkgroup")] public int LinkGroup { get; set; } = 0;

    [JsonPropertyName("start")] public float Start { get; set; }
    [JsonPropertyName("startTime")] public Time StartTime { get; set; }
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
        return this[2] == 0 ? 0 : this[0] + (this[1] / (double)this[2]);
    }

    public static implicit operator double(Time time)
    {
        return time[2] == 0 ? 0 : time[0] + (time[1] / (double)time[2]);
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


        return new Time { wholePart, numerator, denominator };
    }

    private static int GetGcd(int a, int b)
    {
        return b == 0 ? a : GetGcd(b, a % b);
    }

    [JsonIgnore] public double RealTime;
}


public class RpeSpeedEvent
{
    [JsonPropertyName("end")] public float End { get; set; }
    [JsonPropertyName("endTime")] public Time EndTime { get; set; }

    [JsonPropertyName("linkgroup")] public int LinkGroup { get; set; }
    [JsonPropertyName("start")] public float Start { get; set; }
    [JsonPropertyName("startTime")] public Time StartTime { get; set; }

    [JsonIgnore] public double FloorPosition;
}

public class EventList : List<RpeEvent>
{
    public EventList()
    {
    }

    public EventList(bool withBase = false)
    {
        if (withBase)
        {
            Add(new RpeEvent
            {
                EasingType = 1,
                StartTime = new Time { 0, 0, 1 },
                EndTime = new Time { 1, 0, 1 },
                Start = 0,
                End = 0
            });
        }
    }

    [JsonIgnore] public RpeEvent CurrentEvent { get; set; } = null;

    /// <summary>
    /// 计算拍数对应的事件值
    /// </summary>
    /// <param name="time">当前拍数</param>
    /// <returns></returns>
    public double Eval(double time)
    {
        var left = 0;
        var right = 0;
        
        if (Count == 0) return 0;
        CurrentEvent ??= this[0];
        var i = IndexOf(CurrentEvent);
        var result = CurrentEvent.RelativeDist(time);
        if (result == 0) return CurrentEvent.Eval(time);
        if (result > 0)
        {
            if (i >= Count - 1 || this[i + 1].StartTime > time)
            {
                return CurrentEvent.Eval(time);
            }

            if (i + 2 < Count && this[i + 2].StartTime > time)
            {
                CurrentEvent = this[i + 1];
                return CurrentEvent.Eval(time);
            }
    
            left = i;
            right = Count - 1;
        }
        else
        {
            left = 0;
            right = i;
        }

        

        while (left <= right)
        {
            var mid = (left + right) / 2;
            var midEvent = this[mid];

            if (time >= midEvent.StartTime && (mid >= Count - 1 || time < this[mid + 1].StartTime))
            {
                CurrentEvent = midEvent;
                return midEvent.Eval(time);
            }
            if (time < midEvent.StartTime)
            {
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }

        return CurrentEvent.Eval(time);
    }

}


public class SpeedEventList : List<RpeSpeedEvent>
{
    public SpeedEventList()
    {
    }

    public SpeedEventList(bool withBase = false)
    {
        if (withBase)
        {
            Add(new RpeSpeedEvent
            {
                StartTime = new Time { 0, 0, 1 },
                EndTime = new Time { 1, 0, 1 },
                Start = 0,
                End = 0
            });
        }
    }

    public void CalcFloorPosition()
    {
        foreach (RpeSpeedEvent lastEvent in this)
        {
            int i = IndexOf(lastEvent);
            if (i == Count - 1) break;
            var curEvent = this[i + 1];

            double lastStartTime = lastEvent.StartTime.RealTime;
            double lastEndTime = lastEvent.EndTime.RealTime;

            double curStartTime = curEvent.StartTime.RealTime;


            curEvent.FloorPosition +=
                lastEvent.FloorPosition + (lastEvent.End + lastEvent.Start) * (lastEndTime - lastStartTime) / 2 +
                lastEvent.End * (curStartTime - lastEndTime) / 1;
        }
    }

    /// <summary>
    /// 获取当前时间的速度积分
    /// </summary>
    /// <param name="time"></param>
    /// <returns>从谱面开始到当前时间的总路程</returns>
    public double GetCurTimeSu(double time)
    {
        var floorPosition = 0.0d;
        foreach (RpeSpeedEvent speedEvent in this)
        {
            var startTime = speedEvent.StartTime.RealTime;
            var endTime = speedEvent.EndTime.RealTime;

            var i = IndexOf(speedEvent);
            if (Math.Abs(time - speedEvent.StartTime.RealTime) < 1e-5)
            {
                floorPosition += speedEvent.FloorPosition;
                break;
            }

            if (time <= speedEvent.EndTime.RealTime)
            {
                floorPosition += speedEvent.FloorPosition +
                                 (speedEvent.Start + (speedEvent.End - speedEvent.Start) *
                                  (time - startTime) / (endTime - startTime) +
                                  speedEvent.Start) * (time - startTime) / 2;
                break;
            }

            if (Count - 1 != i && !(time <= this[i + 1].StartTime.RealTime)) continue;
            floorPosition += speedEvent.FloorPosition +
                             (speedEvent.End + speedEvent.Start) * (endTime - startTime) / 2 +
                             speedEvent.End * (time - endTime) / 1;
            break;
        }

        return floorPosition;
    }
}


public class EventLayer
{
    [JsonPropertyName("alphaEvents")]
    [field: JsonIgnore]
    public EventList AlphaEvents { get; set; }

    [JsonPropertyName("moveXEvents")]
    [field: JsonIgnore]
    public EventList MoveXEvents { get; set; }

    [JsonPropertyName("moveYEvents")]
    [field: JsonIgnore]
    public EventList MoveYEvents { get; set; }

    [JsonPropertyName("rotateEvents")]
    [field: JsonIgnore]
    public EventList RotateEvents { get; set; }

    [JsonPropertyName("speedEvents")]
    [field: JsonIgnore]
    public SpeedEventList SpeedEvents { get; set; }
}

public class EventLayerList : List<EventLayer>
{
    /// <summary>
    /// 获取时间范围内速度积分
    /// </summary>
    /// <param name="time"></param>
    /// <returns>单位：屏幕高度</returns>
    public double GetCurSu(double time)
    {
        return this.Sum(layer => layer.SpeedEvents.GetCurTimeSu(time));
    }
}


public class Extended
{
    public Extended()
    {
        var defaultInc = new RpeEvent
        {
            Start = 0.0f,
            End = 0.0f,
            EasingType = 1,
            StartTime = new Time { 0, 0, 1 },
            EndTime = new Time { 1, 0, 1 }
        };
        InclineEvents = new EventList
        {
            defaultInc
        };
    }

    public EventList InclineEvents { get; set; }
}


public class NoteEndTimeComparer : Comparer<RpeNote>
{
    public override int Compare(RpeNote x, RpeNote y)
    {
        if (x!.EndTime.RealTime > y!.EndTime.RealTime) return 1;
        if (x.EndTime.RealTime < y.EndTime.RealTime) return -1;
        return 0;
    }
}

public class NoteStartTimeComparer : Comparer<RpeNote>
{
    public override int Compare(RpeNote x, RpeNote y)
    {
        if (x!.StartTime.RealTime > y!.StartTime.RealTime) return 1;
        if (x.StartTime.RealTime < y.StartTime.RealTime) return -1;
        return 0;
    }
}


public enum NoteType
{
    Tap = 1,
    Hold = 2,
    Flick = 3,
    Drag = 4,
}

public class RpeNote
{
    [JsonPropertyName("above")] public int Above { get; set; }
    [JsonPropertyName("alpha")] public int Alpha { get; set; }
    [JsonPropertyName("endTime")] public Time EndTime { get; set; }
    [JsonPropertyName("isFake")] public int IsFake { get; set; }
    [JsonPropertyName("positionX")] public float PositionX { get; set; }
    [JsonPropertyName("size")] public float Size { get; set; }
    [JsonPropertyName("speed")] public float Speed { get; set; }
    [JsonPropertyName("startTime")] public Time StartTime { get; set; }
    [JsonPropertyName("type")] public NoteType Type { get; set; }
    [JsonPropertyName("visibleTime")] public float VisibleTime { get; set; }
    [JsonPropertyName("yOffset")] public float YOffset { get; set; }

    [JsonIgnore] public double FloorPosition;
    [JsonIgnore] public double TopPosition;
    [JsonIgnore] public bool IsHighLight;
}

public enum JudgeState
{
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

    [JsonPropertyName("easing")] public int Easing { get; set; }
    [JsonPropertyName("pos")] public float Pos { get; set; }
    [JsonPropertyName("x")] public float X { get; set; }
}

public class SizeControlItem
{
    public SizeControlItem(float xx)
    {
        Size = 1.0f;
        Easing = 1;
        X = xx;
    }

    [JsonPropertyName("easing")] public int Easing { get; set; }
    [JsonPropertyName("size")] public float Size { get; set; }
    [JsonPropertyName("x")] public float X { get; set; }
}

public class SkewControlItem
{
    public SkewControlItem(float xx)
    {
        Skew = 0.0f;
        Easing = 1;
        X = xx;
    }

    [JsonPropertyName("easing")] public int Easing { get; set; }
    [JsonPropertyName("skew")] public float Skew { get; set; }
    [JsonPropertyName("x")] public float X { get; set; }
}

public class YControlItem
{
    public YControlItem(float xx)
    {
        Y = 1.0f;
        Easing = 1;
        X = xx;
    }

    [JsonPropertyName("easing")] public int Easing { get; set; }
    [JsonPropertyName("y")] public float Y { get; set; }
    [JsonPropertyName("x")] public float X { get; set; }
}

public class JudgeLineJson
{
    public int Group { get; set; }
    public string Name { get; set; }
    public string Texture { get; set; }

    [JsonPropertyName("alphaControl")] public List<AlphaControlItem> AlphaControl { get; set; }
    [JsonPropertyName("bpmfactor")] public float BpmFactor { get; set; }
    [JsonPropertyName("eventLayers")] public EventLayerList EventLayers { get; set; }
    [JsonPropertyName("extended")] public Extended Extended { get; set; }
    [JsonPropertyName("father")] public int Father { get; set; }
    [JsonPropertyName("isCover")] public int IsCover { get; set; }
    [JsonPropertyName("notes")] public List<RpeNote> Notes { get; set; }
    [JsonPropertyName("numOfNotes")] public int NumOfNotes { get; set; }
    [JsonPropertyName("posControl")] public List<PosControlItem> PosControl { get; set; }
    [JsonPropertyName("sizeControl")] public List<SizeControlItem> SizeControl { get; set; }
    [JsonPropertyName("skewControl")] public List<SkewControlItem> SkewControl { get; set; }
    [JsonPropertyName("yControl")] public List<YControlItem> YControl { get; set; }
    [JsonPropertyName("zOrder")] public int ZOrder { get; set; }
}

public static class EnumerableExtended
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
    public int MaxCombo;
    public int PerfectCount;
    public int GoodCount;
    public int MissCount;
    public int NoteSum;
    public JudgeType ChartJudgeType = JudgeType.Perfect;

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
                if (ChartJudgeType == JudgeType.Perfect) ChartJudgeType = JudgeType.Good;
                break;
            case JudgeType.Miss:
                MissCount += 1;
                MaxCombo = 0;
                ChartJudgeType = JudgeType.Miss;
                break;
            case JudgeType.Bad:
                MissCount += 1;
                MaxCombo = 0;
                ChartJudgeType = JudgeType.Miss;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
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
        ChartJudgeType = JudgeType.Perfect;
    }
}


public class ChartRpe
{
    public static Vector2 RpePos2PixelPos(Vector2 rpePos, Vector2 stagePixelSize)
    {
        rpePos.X *= stagePixelSize.X / 1350.0f;
        rpePos.Y *= -stagePixelSize.Y / 900.0f;
        return rpePos;
    }


    public List<BpmListItem> BpmList;

    public Meta Meta { get; set; }
    [JsonPropertyName("judgeLineGroup")] public List<string> JudgeLineGroup { get; set; }
    [JsonPropertyName("judgeLineList")] public List<JudgeLineJson> JudgeLineList { get; set; }


    [JsonIgnore] public JudgeManager JudgeData = new();

    public ChartRpe(List<BpmListItem> bpmList)
    {
        BpmList = bpmList;
    }

    /// <summary>
    /// 预计算一些数值
    /// </summary>
    public void PreCalculation()
    {
        var allNotes = new List<RpeNote>();
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
                layer.SpeedEvents  ??= new SpeedEventList(true);
                layer.MoveXEvents  ??= new EventList(withBase: true);
                layer.MoveYEvents  ??= new EventList(withBase: true);
                layer.MoveYEvents  ??= new EventList(withBase: true);
                layer.AlphaEvents  ??= new EventList(withBase: true);
                layer.RotateEvents ??= new EventList(withBase: true);

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
                    note.TopPosition += layer.SpeedEvents.GetCurTimeSu(note.EndTime.RealTime);
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
    private double BeatTime2RealTime(double beatTime)
    {
        var bPmList = BpmList;
        var bpmSeconds = new List<double>();
        foreach (BpmListItem bpmInfo in bPmList)
        {
            int i = bPmList.IndexOf(bpmInfo);
            if (i < bPmList.Count - 1)
            {
                double dBeat = bPmList[i + 1].StartTime - bpmInfo.StartTime;
                bpmSeconds.Add(dBeat * 60 / bpmInfo.Bpm);
            }
            else
            {
                bpmSeconds.Add(999999.0);
            }
        }

        var secondSum = 0.0d;
        foreach (BpmListItem bpmInfo in bPmList)
        {
            var i = bPmList.IndexOf(bpmInfo);
            if (i == bPmList.Count - 1 || bPmList[i + 1].StartTime >= beatTime)
            {
                secondSum += (beatTime - bpmInfo.StartTime) * 60 / bpmInfo.Bpm;
            }
            else
            {
                secondSum += bpmSeconds[i];
                break;
            }
        }

        return secondSum;
    }

    /// <summary>
    /// 将秒数转换为拍数
    /// </summary>
    public double RealTime2BeatTime(double realTime)
    {
        var bpmSeconds = new List<double>();
        foreach (var bpmInfo in BpmList)
        {
            var i = BpmList.IndexOf(bpmInfo);
            if (i < BpmList.Count - 1)
            {
                var dBeat = BpmList[i + 1].StartTime - bpmInfo.StartTime;
                bpmSeconds.Add(dBeat * 60 / bpmInfo.Bpm);
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
                double timeInBpmRange = realTime - last;
                var curBpmInfo = BpmList[bpmSeconds.IndexOf(t)];
                return (timeInBpmRange * curBpmInfo.Bpm / 60) + curBpmInfo.StartTime;
            }

            last = second;
        }

        return 0;
    }
}