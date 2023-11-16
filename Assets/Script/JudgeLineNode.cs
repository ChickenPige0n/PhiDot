using Godot;
using Phigodot.ChartStructure;
using System;
using System.Collections.Generic;
using Phigodot.Game;
using System.Runtime.InteropServices;

public partial class JudgeLineNode : Sprite2D
{
	// TODO:
	// Properties Calculate by time -finished!
	// Note Generate                -half done!
	// Note YPos Calculate
	private int LineIndex;
	private ChartRPE Chart;

	public ResPack CurPack;

	[Export] public Label Idex;

	[Export] public PackedScene NoteScene;

	[Export] public Sprite2D TextureLine;
	public List<NoteNode> noteInstances = new List<NoteNode>();

	public Vector2 StageSize;
	public Vector2 WindowSize;
	public double AspectRatio = 1.666667d;

	public void CalcTime(double realTime)
	{
		var chartTime = Chart.RealTime2BeatTime(realTime);

		var EventLayers = Chart.JudgeLineList[LineIndex].EventLayers;

		double xPos = 0;
		double yPos = 0;
		double alpha = 0;
		double rotate = 0;

		foreach (EventLayer layer in EventLayers)
		{
			xPos += layer.MoveXEvents.GetValByTime(chartTime);
			yPos += layer.MoveYEvents.GetValByTime(chartTime);
			alpha += layer.AlphaEvents.GetValByTime(chartTime);
			rotate += layer.RotateEvents.GetValByTime(chartTime);
		}

		Position = ChartRPE.RPEPos2PixelPos(new Vector2((float)xPos, (float)yPos), StageSize) + (StageSize / 2);
		RotationDegrees = (float)rotate;
		var m = SelfModulate;
	 	TextureLine.SelfModulate = new Color(m.R, m.G, m.B, (float)alpha / 255.0f);

		var noteList = Chart.JudgeLineList[LineIndex].Notes;
		foreach (RPENote note in noteList.OrEmptyIfNull())
		{
			int i = noteList.IndexOf(note);
			if (chartTime >= note.EndTime)
			{
				if (!noteInstances[i].Judged)
				{
					noteInstances[i].Judged = true;
				}
			}
			var newY = StageSize.Y / 7.5f * note.Speed * (float)(GetCurSu(realTime) - note.FloorPosition);
			newY = note.Above == 1 ? newY : -newY;
			noteInstances[i].Position = new Vector2(noteInstances[i].Position.X, newY);
		}
	}
	public void CalcScale()
	{
		float newScale = 0;
		newScale = 2.5f * StageSize.DistanceTo(Vector2.Zero) / this.TextureLine.Texture.GetWidth();
		this.TextureLine.Scale = new Vector2(newScale,newScale);

		// TODO:
		// Support mutable note size.
		newScale = 175.0f * (CurPack.TapTexture.GetSize().X * 1350.0f/StageSize.X);
		
	}

	public void Init(ChartRPE chart, int lineIndex)
	{
		this.Chart = chart;
		this.LineIndex = lineIndex;


		var ResPackManager = GetNode<ResPackManager>("/root/ResPackManager");
		CurPack = ResPackManager.CurPack;

		WindowSize = DisplayServer.WindowGetSize();
		StageSize = new Vector2I((int)((double)WindowSize.Y * AspectRatio), (int)WindowSize.Y);
		CalcScale();
		// Note Init
		Idex.Text = LineIndex.ToString();
		var noteList = chart.JudgeLineList[lineIndex].Notes;
		foreach (RPENote noteInfo in noteList.OrEmptyIfNull())
		{
			var instance = NoteScene.Instantiate<NoteNode>();
			instance.NoteInfo = noteInfo;
			noteInstances.Add(instance);
			switch (noteInfo.Type)
			{
				case NoteType.Tap:
					instance.HeadSprite.Texture = CurPack.TapTexture;
					break;
				case NoteType.Hold:
					instance.HeadSprite.Texture = CurPack.HoldTextures.head;
					instance.BodySprite.Texture = CurPack.HoldTextures.body;
					instance.TailSprite.Texture = CurPack.HoldTextures.tail;//hold
					break;
				case NoteType.Flick:
					instance.HeadSprite.Texture = CurPack.FlickTexture;
					break;
				case NoteType.Drag:
					instance.HeadSprite.Texture = CurPack.DragTexture;
					break;
				default:
					break;
			}
			var posX = noteInfo.PositionX * (StageSize.X / 1350.0f);
			float posY = (float)noteInfo.FloorPosition;
			if (instance.NoteInfo.Above != 1) { posY = -posY; }
			instance.Position = new Vector2(posX, posY);
			AddChild(instance);
		}
	}

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
		foreach (var Layer in Chart.JudgeLineList[LineIndex].EventLayers)
		{
			result += Layer.SpeedEvents.GetCurTimeSu(time);
		}
		return (float)result;
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}
}
