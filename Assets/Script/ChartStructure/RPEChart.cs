using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;
using static Phigodot.ChartStructure.Easings;

//generated automatically

namespace Phigodot.ChartStructure
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
    public class Time : List<int>
    {
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
        public int EasingType { get; set; }
        [JsonPropertyName("end")]
        public float End { get; set; }
        [JsonPropertyName("endTime")]
        public Time EndTime { get; set; }


        // Include extended time after event.
        [JsonIgnore]
        public double RealEndTime {get;set;}

        [JsonPropertyName("linkgroup")]
        public int LinkGroup { get; set; }
        [JsonPropertyName("start")]
        public float Start { get; set; }
        [JsonPropertyName("startTime")]
        public Time StartTime { get; set; }

        [JsonIgnore]
        public double floorPosition = 0;

        /// <summary>
        /// 计算速度事件从startTime到endTime的积分
        /// </summary>
        /// <param name="startTime">开始时间,单位为秒。</param>
        /// <param name="endTime">结束时间,单位为秒,不能大于下一个事件开始时间</param>
        /// <returns></returns>
        public double Integral(double startTime, double endTime)
        {
            if (endTime <= StartTime.RealTime||startTime >= RealEndTime) return 0;// Beyond range.

            double result = 0.0d;
            double inEventEndTime = Math.Min(EndTime.RealTime, endTime);
            double inEventStartTime = Math.Max(StartTime.RealTime, startTime);
            double avgVal = ValAt((inEventStartTime + inEventEndTime) / 2);
            result += avgVal * (inEventEndTime - inEventStartTime);

            if (endTime >= EndTime.RealTime) result += End * (Math.Min(endTime, RealEndTime) - Math.Max(startTime, this.EndTime.RealTime));
            return result;
        }

        public double ValAt(double time)
        {
            double ratio = (time - StartTime.RealTime) / (EndTime.RealTime - StartTime.RealTime);
            return (End * ratio) + (Start * (1 - ratio));
        }

    }

    public class EventList : List<RPEEvent>
    {
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
                double nextEnd = i == this.Count - 1 ? 99999.0d:this[i + 1].StartTime;

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
        /// <summary>
        /// 获取一段时间内速度积分
        /// </summary>
        /// <param name="startTime">开始时间(单位为秒)</param>
        /// <param name="endTime">结束时间(单位为秒)</param>
        public double Integral(double startTime, double endTime)
        {
            double result = 0.0d;
            foreach (var e in this)
            {
                result += e.Integral(startTime, endTime);
            }
            return result;
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

        //<summary>
        //1:moveX 2:moveY 3:rotation 4:alpha 5:speed
        //</summary>




    }


    public class Extended
    {
        public Extended()
        {
            RPEEvent defaultInc = new RPEEvent();
            defaultInc.Start = 0.0f;
            defaultInc.End = 0.0f;
            defaultInc.EasingType = 1;
            defaultInc.StartTime = new Time { 0, 0, 1 };
            defaultInc.EndTime = new Time { 1, 0, 1 };
            inclineEvents = new EventList
            {
                defaultInc
            };
        }
        public EventList inclineEvents { get; set; }
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
        public int Type { get; set; }
        [JsonPropertyName("visibleTime")]
        public float VisibleTime { get; set; }
        [JsonPropertyName("yOffset")]
        public float YOffset { get; set; }
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
        public List<EventLayer> EventLayers { get; set; }
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

    public class ChartRPE
    {
        public static Godot.Vector2 RPEPos2PixelPos(Godot.Vector2 RPEPos, Godot.Vector2 StagePixelSize)
        {
            RPEPos.X *= StagePixelSize.X / 1350.0f;
            RPEPos.Y *= StagePixelSize.Y / 900.0f;
            return RPEPos;
        }


        public List<BPMListItem> BPMList;

        public META META { get; set; }
        [JsonPropertyName("judgeLineGroup")]
        public List<string> JudgeLineGroup { get; set; }
        [JsonPropertyName("judgeLineList")]
        public List<JudgeLineJson> JudgeLineList { get; set; }

        /// <summary>
        /// 预计算一些数值
        /// </summary>
        public void PreCalculation()
        {
            foreach (var line in JudgeLineList)
            {

                // RealTime Calculate.
                foreach (var note in line.Notes.OrEmptyIfNull())
                {
                    note.StartTime.RealTime = BeatTime2RealTime(note.StartTime);
                    note.EndTime.RealTime = BeatTime2RealTime(note.EndTime);
                }


                foreach (var layer in line.EventLayers)
                {

                    // Not necessary.

                    // foreach(var e in layer.moveXEvents){
                    //     e.startTime.RealTime = BeatTime2RealTime(BPMList,e.startTime);
                    //     e.endTime.RealTime   = BeatTime2RealTime(BPMList,e.endTime);
                    // }
                    // foreach(var e in layer.moveYEvents){
                    //     e.startTime.RealTime = BeatTime2RealTime(BPMList,e.startTime);
                    //     e.endTime.RealTime   = BeatTime2RealTime(BPMList,e.endTime);
                    // }
                    // foreach(var e in layer.rotateEvents){
                    //     e.startTime.RealTime = BeatTime2RealTime(BPMList,e.startTime);
                    //     e.endTime.RealTime   = BeatTime2RealTime(BPMList,e.endTime);
                    // }
                    // foreach(var e in layer.alphaEvents){
                    //     e.startTime.RealTime = BeatTime2RealTime(BPMList,e.startTime);
                    //     e.endTime.RealTime   = BeatTime2RealTime(BPMList,e.endTime);
                    // }


                    foreach (var e in layer.SpeedEvents)
                    {
                        e.StartTime.RealTime = BeatTime2RealTime(e.StartTime);
                        e.EndTime.RealTime = BeatTime2RealTime(e.EndTime);
                    }
                    foreach (var e in layer.SpeedEvents){
                        
                        // Real end time calculate.
                        var list = layer.SpeedEvents;
                        int i = list.IndexOf(e);
                        if (i == list.Count - 1) continue; // Skip last event.
                        e.RealEndTime = list[i + 1].StartTime.RealTime; // Collapse situration NOT considered.
                    }
                    layer.SpeedEvents[layer.SpeedEvents.Count - 1].RealEndTime = 99999.0d; // May have better solution.
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