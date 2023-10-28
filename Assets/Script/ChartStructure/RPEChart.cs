using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Phigodot.ChartStructure.Easings;

//generated automatically

namespace Phigodot.ChartStructure
{

    public class BPMListItem
    {
        public float bpm { get; set; }
        public Time startTime { get; set; }
    }
    public class META
    {
        public int RPEVersion { get; set; }
        public string background { get; set; }
        public string charter { get; set; }
        public string composer { get; set; }
        public string id { get; set; }
        public string level { get; set; }
        public string name { get; set; }
        public int offset { get; set; }
        public string song { get; set; }
    }
    public class AlphaControlItem
    {
        public AlphaControlItem(float xx)
        {
            alpha = 1.0f;
            easing = 1;
            x = xx;
        }
        public float alpha { get; set; }
        public int easing { get; set; }
        public float x { get; set; }
    }
    public class RPEEvent
    {
        public RPEEvent()
        {
            linkgroup = 0;
            easingLeft = 0;
            easingRight = 1;
            bezier = 0;
            bezierPoints = new BezierPoints() { 0.0f, 0.0f, 0.0f, 0.0f };
        }

        public double GetCurVal(double t)
        {
            if (t > startTime && t < endTime)
            {
                var easeResult = easeFuncs[easingType]((t - startTime) / (endTime - startTime));
                return (start * (1-easeResult)) + (end * easeResult);
            }
            else if (Math.Abs(t - startTime)<=0.01 || Math.Abs(t - endTime)<=0.01)
            {
                return Math.Abs(t - startTime) <= 0.01 ? start : end;
            }
            else
            {
                return end;
            }
        }


        public int bezier { get; set; }
        public BezierPoints bezierPoints { get; set; }
        public float easingLeft { get; set; }
        public float easingRight { get; set; }
        public int easingType { get; set; }
        public float end { get; set; }
        public Time endTime { get; set; }
        public int linkgroup { get; set; }
        public float start { get; set; }
        public Time startTime { get; set; }
    }



    //Cubic Bezier
    public class BezierPoints : List<float>
    {

    }

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
    }


    public class SpeedEventsItem
    {
        public float end { get; set; }
        public Time endTime { get; set; }
        public int linkgroup { get; set; }
        public float start { get; set; }
        public Time startTime { get; set; }
    }

    public class EventList : List<RPEEvent>
    {
        public double GetValByTime(double time)
        {
            foreach (RPEEvent e in this)
            {
                var i = this.IndexOf(e);
                if (time >=e.startTime && time <= e.endTime)
                {
                    return e.GetCurVal(time);
                }
                if(i==this.Count-1) break;
                else if(time>=e.endTime&&time<=this[i+1].startTime)
                {
                    return e.GetCurVal(time);
                }
            }
            return 0;
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
        private List<SpeedEventsItem> _speedEvents;

        public EventList alphaEvents
        {
            get { return _alphaEvents; }
            set { _alphaEvents = value; }
        }

        public EventList moveXEvents
        {
            get { return _moveXEvents; }
            set { _moveXEvents = value; }
        }

        public EventList moveYEvents
        {
            get { return _moveYEvents; }
            set { _moveYEvents = value; }
        }

        public EventList rotateEvents
        {
            get { return _rotateEvents; }
            set { _rotateEvents = value; }
        }

        public List<SpeedEventsItem> speedEvents
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
        public Extended() {
            RPEEvent defaultInc = new RPEEvent();
            defaultInc.start = 0.0f;
            defaultInc.end = 0.0f;
            defaultInc.easingType = 1;
            defaultInc.startTime = new Time { 0, 0, 1 };
            defaultInc.endTime = new Time { 1, 0, 1 };
            inclineEvents = new EventList
            {
                defaultInc
            };
        }
        public EventList inclineEvents { get; set; }
    }

    public class NotesItem
    {
        public int above { get; set; }
        public int alpha { get; set; }
        public Time endTime { get; set; }
        public int isFake { get; set; }
        public float positionX { get; set; }
        public float size { get; set; }
        public float speed { get; set; }
        public Time startTime { get; set; }
        public int type { get; set; }
        public float visibleTime { get; set; }
        public float yOffset { get; set; }
    }

    public class PosControlItem
    {
        public PosControlItem(float xx)
        {
            pos = 1.0f;
            easing = 1;
            x = xx;
        }
        public int easing { get; set; }
        public float pos { get; set; }
        public float x { get; set; }
    }
    public class SizeControlItem
    {
        public SizeControlItem(float xx)
        {
            size = 1.0f;
            easing = 1;
            x = xx;
        }
        public int easing { get; set; }
        public float size { get; set; }
        public float x { get; set; }
    }
    public class SkewControlItem
    {
        public SkewControlItem(float xx)
        {
            skew = 0.0f;
            easing = 1;
            x = xx;
        }
        public int easing { get; set; }
        public float skew { get; set; }
        public float x { get; set; }
    }
    public class YControlItem
    {
        public YControlItem(float xx)
        {
            y = 1.0f;
            easing = 1;
            x = xx;
        }
        public int easing { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }
    public class JudgeLineListItem
    {
        public int @Group { get; set; }
        public string Name { get; set; }
        public string Texture { get; set; }
        public List<AlphaControlItem> alphaControl { get; set; }
        public float bpmfactor { get; set; }
        public List<EventLayer> eventLayers { get; set; }
        public Extended extended { get; set; }
        public int father { get; set; }
        public int isCover { get; set; }
        public List<NotesItem> notes { get; set; }
        public int numOfNotes { get; set; }
        public List<PosControlItem> posControl { get; set; }
        public List<SizeControlItem> sizeControl { get; set; }
        public List<SkewControlItem> skewControl { get; set; }
        public List<YControlItem> yControl { get; set; }
        public int zOrder { get; set; }
    }
    public class RPEChart
    {
        public static Godot.Vector2 RPEPos2PixelPos(Godot.Vector2 RPEPos,Godot.Vector2 StagePixelSize)
        {
            RPEPos.X *= StagePixelSize.X/1350.0f;
            RPEPos.Y *= StagePixelSize.Y/900.0f;
            return RPEPos;
        }


        public List<BPMListItem> BPMList;
        
        public META META { get; set; }
        public List<string> judgeLineGroup { get; set; }
        public List<JudgeLineListItem> judgeLineList { get; set; }


        public double RealTime2ChartTime(double realTime)
        {
            List<double> bpmSeconds = new List<double>();
            foreach(BPMListItem BPMInfo in BPMList){
                int i = BPMList.IndexOf(BPMInfo);
                if (i < BPMList.Count - 1){
                    double dChartTime = BPMList[i+1].startTime - BPMInfo.startTime;
                    bpmSeconds.Add(dChartTime*60/BPMInfo.bpm);
                }else{
                    bpmSeconds.Add(999999.0);
                }
            }



            double second = 0.0;
            double last = 0.0;
            foreach(double t in bpmSeconds)
            {
                second += t;
                if(t>=realTime)
                {
                    double timeInBPMRange = realTime - last;
                    var curBPMInfo = BPMList[bpmSeconds.IndexOf(t)];
                    return (timeInBPMRange*curBPMInfo.bpm/60) + curBPMInfo.startTime;
                }
                last = second;
            }
            return 0;
        }
    }
}