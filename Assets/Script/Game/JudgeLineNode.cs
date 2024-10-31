using System;
using System.Collections.Generic;
using Godot;

public partial class JudgeLineNode : Sprite2D
{
	private const double HoldMissTime = 0.05;
	public JudgeType ChartJudgeState;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}


	private int _lineIndex;
	private ChartRpe _chart;

	private ResPack _curPack;

	[Export] public Label Idex;

	[Export] public PackedScene NoteScene;

	[Export] public Sprite2D TextureLine;
	public List<NoteNode> NoteInstances = new();

	public Vector2 StageSize;
	public Vector2 WindowSize;
	public double AspectRatio = 1.666667d;


	private bool _isAutoPlay; 
	private float _noteJudgeSize = 100;
	public List<ChartManager.FingerData> FingerDataList;


	private double _chartTime;
	public void CalcTime(double realTime, Vector2 stageSize)
	{
		StageSize = stageSize;
		var a = _chart.RealTime2BeatTime(realTime);
		var dChartTime = a - _chartTime;
		_chartTime = a;

		var eventLayers = _chart.JudgeLineList[_lineIndex].EventLayers;

		double xPos = 0;
		double yPos = 0;
		double alpha = 0;
		double rotate = 0;

		foreach (var layer in eventLayers)
		{
			xPos += layer.MoveXEvents.Eval(_chartTime);
			yPos += layer.MoveYEvents.Eval(_chartTime);
			alpha += layer.AlphaEvents.Eval(_chartTime);
			rotate += layer.RotateEvents.Eval(_chartTime);
		}

		Position = ChartRpe.RpePos2PixelPos(new Vector2((float)xPos, (float)yPos), StageSize) + (StageSize / 2);
		RotationDegrees = (float)rotate;
		var m = ChartJudgeState switch
		{
			JudgeType.Perfect => ChartManager.PerfectColor,
			JudgeType.Good => ChartManager.GoodColor,
			_ => new Color(1, 1, 1)
		};
		TextureLine.Modulate = new Color(m.R, m.G, m.B, (float)alpha / 255.0f);





		foreach (var noteObject in NoteInstances)
		{
			var note = noteObject.NoteInfo;

			if (_isAutoPlay && _chartTime > note.StartTime && noteObject.State == JudgeState.NotJudged)
			{
				noteObject.NoteJudgeType = JudgeType.Perfect;
				noteObject.EmitOnJudged();
			}
			
			if (_chartTime > note.EndTime)
			{
				noteObject.Visible = false;
				if (noteObject.NoteJudgeType == JudgeType.Perfect && noteObject.State == JudgeState.Judged && noteObject.NoteInfo.Type >= NoteType.Flick)
				{
					noteObject.EmitOnJudged();
					noteObject.State = JudgeState.Dead;
				}
				else if (noteObject.State == JudgeState.NotJudged && realTime - note.StartTime.RealTime >= ChartManager.BadRange)
				{
					noteObject.NoteJudgeType = JudgeType.Miss;
					noteObject.EmitOnJudged();
				}
				continue;
			}
			var eventLayerList = _chart.JudgeLineList[_lineIndex].EventLayers;
			var newY = StageSize.Y / 7.5f * note.Speed * (float)(eventLayerList.GetCurSu(realTime) - note.FloorPosition);
            const float eps = 0.01f;
            noteObject.Visible = !(newY > -eps && noteObject.NoteInfo.Type != NoteType.Hold);
			
			newY = note.Above == 1 ? newY : -newY;
			
			noteObject.Position = new Vector2(noteObject.Position.X, realTime >= note.StartTime.RealTime ? 0 : newY);

			
			if (note.Type == NoteType.Hold)
			{
				if (realTime >= note.StartTime.RealTime && realTime <= note.EndTime.RealTime && noteObject.State == JudgeState.Holding)
				{
					noteObject.UntouchTimer += GetProcessDeltaTime();


					if (!_isAutoPlay && noteObject.UntouchTimer > HoldMissTime)
					{
						noteObject.State = JudgeState.Judged;
						noteObject.NoteJudgeType = JudgeType.Miss;
						noteObject.Modulate = new Color(1, 1, 1, .5f);
						noteObject.EmitOnJudged();
					}
					noteObject.HoldTimer += dChartTime;
					if (noteObject.HoldTimer > 0.5f)
					{
						GD.Print($"Emit once");
						noteObject.EmitOnJudged(false);
						noteObject.HoldTimer = 0;
					}
				}

				var end = StageSize.Y / 7.5f * note.Speed * (float)(eventLayerList.GetCurSu(realTime) - note.TopPosition);
				end = note.Above == 1 ? end : -end;

				if (_chartTime >= note.StartTime)
				{
					
					noteObject.Head.Visible = false;
				}

				noteObject.Body.Scale = new Vector2(noteObject.Body.Scale.X, (noteObject.Position.Y - end) / noteObject.Body.Texture.GetSize().Y);
				noteObject.Tail.Position = new Vector2(0, end - noteObject.Position.Y);
			}


			if (noteObject.NoteInfo.Type == NoteType.Tap) continue;

			foreach (var fingerData in FingerDataList)
			{
				var pos = ((fingerData.CurPos * GetCanvasTransform()) - GlobalPosition).Rotated(-Rotation);
				var dt = Math.Abs(realTime - noteObject.NoteInfo.StartTime.RealTime);
				var dx = Math.Abs(noteObject.Position.X - pos.X);
				if (noteObject.State == JudgeState.Judged || !(dx < _noteJudgeSize)) continue;
				if (noteObject.NoteInfo.Type == NoteType.Hold)
				{
					noteObject.UntouchTimer = 0;
					continue;
				}
				if (dt < ChartManager.GoodRange && (noteObject.NoteInfo.Type == NoteType.Drag || fingerData.CurVec.Length() >= 180))
				{
					noteObject.State = JudgeState.Judged;
				}
			}
		}

	}
	public Vector2 CalcScale()
	{
		var newScale = 2.5f * StageSize.DistanceTo(Vector2.Zero) / TextureLine.Texture.GetWidth();
		TextureLine.Scale = Vector2.One * newScale;

		// TODO:
		// Support mutable note size.
		var noteScale = StageSize.X * 175 / (1350 * _curPack.TapTexture.GetSize().X);
		foreach (var note in NoteInstances)
		{
			var flipFactor = note.NoteInfo.Above == 1 ? 1 : -1;
			note.Head.Scale = Vector2.One * flipFactor * noteScale;
			if (note.NoteInfo.Type != NoteType.Hold) continue;
			note.Body.Scale = Vector2.One * flipFactor * noteScale;
			note.Tail.Scale = Vector2.One * flipFactor * noteScale;
		}

		return new Vector2(newScale, newScale);
	}

	public void Init(ChartRpe chart, int lineIndex, bool isAutoPlay)
	{
		_chart = chart;
		_lineIndex = lineIndex;
		_isAutoPlay = isAutoPlay;

		var resPackManager = GetNode<ResPackManager>("/root/ResPackManager");
		_curPack = resPackManager.CurPack;

		WindowSize = DisplayServer.WindowGetSize();
		StageSize = new Vector2I((int)(WindowSize.Y * AspectRatio), (int)WindowSize.Y);
		CalcScale();

		TextureLine.Modulate = new(0xecebb0e7);
		// Note Init
		Idex.Text = _lineIndex.ToString();
		var noteList = chart.JudgeLineList[lineIndex].Notes;
		foreach (RpeNote noteInfo in noteList.OrEmptyIfNull())
		{
			var instance = NoteScene.Instantiate<NoteNode>();
			instance.NoteInfo = noteInfo;
			NoteInstances.Add(instance);
			switch (noteInfo.Type)
			{
				case NoteType.Tap:
					instance.ZIndex = 1;
					instance.Head.Texture = instance.NoteInfo.IsHighLight ? _curPack.TapHlTexture : _curPack.TapTexture;
					break;
				case NoteType.Hold:
					instance.ZIndex = 0;
					var (head, body, tail) = instance.NoteInfo.IsHighLight ? _curPack.HoldHlTextures : _curPack.HoldTextures;
					instance.Head.Texture = head;
					instance.Body.Texture = body;
					instance.Tail.Texture = tail;
					instance.Head.Offset = new Vector2(0, head.GetSize().Y / 2);
					instance.Body.Offset = new Vector2(0, -body.GetSize().Y / 2);
					instance.Tail.Offset = new Vector2(0, -tail.GetSize().Y / 2);
					break;
				case NoteType.Flick:
					instance.ZIndex = 3;
					instance.Head.Texture = instance.NoteInfo.IsHighLight ? _curPack.FlickHlTexture : _curPack.FlickTexture;
					break;
				case NoteType.Drag:
					instance.ZIndex = 2;
					instance.Head.Texture = instance.NoteInfo.IsHighLight ? _curPack.DragHlTexture : _curPack.DragTexture;
					break;
			}
			var posX = noteInfo.PositionX * (StageSize.X / 1350.0f);
			instance.Position = new Vector2(posX, 0);

			AddChild(instance);
		}

		CalcTime(0, StageSize);
		foreach (var note in NoteInstances)
		{
			note.Visible = true;
		}
	}
}
