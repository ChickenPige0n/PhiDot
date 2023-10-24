using Godot;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;


namespace Phigodot.ChartStructure
{
    // 读谱类
    public class Note
    {
    	public int type { get; set; }
    	public int time { get; set; }
    	public float positionX { get; set; }
    	public float holdTime { get; set; }
    	public float speed { get; set; }
    	public float floorPosition { get; set; }
    }
    public class SpeedEvent
    {
    	public float startTime { get; set; }
    	public float endTime { get; set; }
    	public float value { get; set; }
    }
    public class JudgeLineMoveEvent
    {
    	public float startTime { get; set; }
    	public float endTime { get; set; }
    	public float start { get; set; }
    	public float end { get; set; }
    	public float start2 { get; set; }
    	public float end2 { get; set; }
    }
    public class JudgeLineRotateEvent
    {
    	public float startTime { get; set; }
    	public float endTime { get; set; }
    	public float start { get; set; }
    	public float end { get; set; }
    }
    public class JudgeLineDisappearEvent
    {
    	public float startTime { get; set; }
    	public float endTime { get; set; }
    	public float start { get; set; }
    	public float end { get; set; }
    }
    public class JudgeLineList
    {
    	public float bpm { get; set; }
    	public required List<Note> notesAbove { get; set; }
    	public required List<object> notesBelow { get; set; }
    	public required List<SpeedEvent> speedEvents { get; set; }
    	public required List<JudgeLineMoveEvent> judgeLineMoveEvents { get; set; }
    	public required List<JudgeLineRotateEvent> judgeLineRotateEvents { get; set; }
    }
    public class OfficialChart
    {
    	public int formatVersion { get; set; }
    	public float offset { get; set; }
    	public required List<JudgeLineList> judgeLineList { get; set; }
    }
}