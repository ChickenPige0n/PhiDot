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

	[Export] public Texture2D TapTexture;
	[Export] public Texture2D FlickTexture;
	[Export] public Texture2D DragTexture;

	[Export] public Label Idex;

	[Export] public PackedScene NoteScene;
	public List<NoteNode> noteInstances = new List<NoteNode>();



	public Vector2I StageSize;
	public double AspectRatio = 1.666667d;

	public double ChartTime
	{
		set
		{
			var EventLayers = Chart.JudgeLineList[LineIndex].EventLayers;

			double xPos = 0;
			double yPos = 0;
			double alpha = 0;
			double rotate = 0;

			foreach (EventLayer layer in EventLayers)
			{
				xPos += layer.MoveXEvents.GetValByTime(value);
				yPos += layer.MoveYEvents.GetValByTime(value);
				alpha += layer.AlphaEvents.GetValByTime(value);
				rotate += layer.RotateEvents.GetValByTime(value);
			}

			Position = ChartRPE.RPEPos2PixelPos(new Vector2((float)xPos, -(float)yPos), StageSize) + (StageSize / 2);
			RotationDegrees = (float)rotate + 180;
			var m = SelfModulate;
			SelfModulate = new Color(m.R, m.G, m.B, (float)alpha / 255.0f);

			var realTime = Chart.BeatTime2RealTime(value);
			var noteList = Chart.JudgeLineList[LineIndex].Notes;
			foreach (RPENote note in noteList.OrEmptyIfNull())
			{
				int i = noteList.IndexOf(note);
				var newY = note.Speed * 15f * (float)(note.FloorPosition - GetCurSu(realTime));
				newY = note.Above == 1 ? newY : -newY;
				noteInstances[i].Position = new Vector2(noteInstances[i].Position.X, newY);
			}

		}
	}

	public void InitChart(ChartRPE chart, int lineIndex)
	{
		this.Chart = chart;
		this.LineIndex = lineIndex;

		Vector2I windowSize = DisplayServer.WindowGetSize();
		StageSize = new Vector2I((int)((double)windowSize.Y * AspectRatio), (int)windowSize.Y);

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
				case 1:
					instance.Texture = TapTexture;
					break;
				case 2:
					instance.Texture = TapTexture;//hold
					break;
				case 3:
					instance.Texture = FlickTexture;
					break;
				case 4:
					instance.Texture = DragTexture;
					break;
				default:
					break;
			}
			var x = ((float)StageSize.X) / 1350.0f;
			var posX = noteInfo.PositionX * x;
			float posY = (float)noteInfo.FloorPosition;
			GD.Print(x);
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
	public float GetCurSu(double time, float factor = 1.0f)
	{
		double result = 0;
		foreach (var Layer in Chart.JudgeLineList[LineIndex].EventLayers)
		{
			result += Layer.SpeedEvents.GetCurTimeSu(time);
		}
		return (float)result * factor;
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Vector2I windowSize = DisplayServer.WindowGetSize();
		StageSize = new Vector2I((int)((double)windowSize.Y * AspectRatio), (int)windowSize.Y);
	}
}
