using System;
using System.Collections.Generic;
using Godot;
using Phidot.ChartStructure;

namespace Phidot.Game
{
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


		private float _noteJudgeSize = 100;
		public List<ChartManager.FingerData> FingerDataList;


		private double _chartTime;
		public void CalcTime(double realTime)
		{
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
				xPos += layer.MoveXEvents.GetValByTime(_chartTime);
				yPos += layer.MoveYEvents.GetValByTime(_chartTime);
				alpha += layer.AlphaEvents.GetValByTime(_chartTime);
				rotate += layer.RotateEvents.GetValByTime(_chartTime);
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





			foreach (var instance in NoteInstances)
			{
				var note = instance.NoteInfo;

				if (_chartTime > note.EndTime)
				{
					instance.Visible = false;
					if (instance.NoteJudgeType == JudgeType.Perfect && instance.State == JudgeState.Judged && instance.NoteInfo.Type >= NoteType.Flick)
					{
						instance.EmitOnJudged(instance.NoteJudgeType, instance.NoteInfo.Type);
						instance.State = JudgeState.Dead;
					}
					else if (instance.State == JudgeState.NotJudged && realTime - note.StartTime.RealTime >= ChartManager.BadRange)
					{
						instance.NoteJudgeType = JudgeType.Miss;
						instance.EmitOnJudged(instance.NoteJudgeType, instance.NoteInfo.Type);
						instance.State = JudgeState.Judged;
					}
					continue;
				}
				var eventLayerList = _chart.JudgeLineList[_lineIndex].EventLayers;
				var newY = StageSize.Y / 7.5f * note.Speed * (float)(eventLayerList.GetCurSu(realTime) - note.FloorPosition);
				newY = note.Above == 1 ? newY : -newY;
				instance.Position = new Vector2(instance.Position.X, realTime >= note.StartTime.RealTime ? 0 : newY);
				if (note.Type == NoteType.Hold)
				{
					if (realTime >= note.StartTime.RealTime && realTime <= note.EndTime.RealTime && instance.State == JudgeState.Holding)
					{
						instance.UntouchTimer += GetProcessDeltaTime();

						GD.Print($"Type: {instance.UntouchTimer}");

						if (instance.UntouchTimer > HoldMissTime)
						{
							instance.State = JudgeState.Judged;
							instance.NoteJudgeType = JudgeType.Miss;
							instance.Modulate = new Color(1, 1, 1, .5f);
							instance.EmitOnJudged(instance.NoteJudgeType, instance.NoteInfo.Type);
						}
						instance.HoldTimer += dChartTime;
						if (instance.HoldTimer > 0.5f)
						{
							instance.EmitOnJudged(instance.NoteJudgeType, instance.NoteInfo.Type, false);
							instance.HoldTimer = 0;
						}
					}

					var end = StageSize.Y / 7.5f * note.Speed * (float)(eventLayerList.GetCurSu(realTime) - note.CeliPosition);
					end = note.Above == 1 ? end : -end;

					if (_chartTime >= note.StartTime)
					{
						instance.Head.Visible = false;
					}

					instance.Body.Scale = new Vector2(instance.Body.Scale.X, (instance.Position.Y - end) / instance.Body.Texture.GetSize().Y);
					instance.Tail.Position = new Vector2(0, end - instance.Position.Y);
				}


				if (instance.NoteInfo.Type == NoteType.Tap) continue;

				foreach (var fingerData in FingerDataList)
				{
					var pos = ((fingerData.CurPos * GetCanvasTransform()) - GlobalPosition).Rotated(-Rotation);
					var dt = Math.Abs(realTime - instance.NoteInfo.StartTime.RealTime);
					var dx = Math.Abs(instance.Position.X - pos.X);
					if (instance.State == JudgeState.Judged || !(dx < _noteJudgeSize)) continue;
					if (instance.NoteInfo.Type == NoteType.Hold)
					{
						instance.UntouchTimer = 0;
						continue;
					}
					if (dt < ChartManager.GoodRange && (instance.NoteInfo.Type == NoteType.Drag || fingerData.CurVec.Length() >= 180))
					{
						instance.State = JudgeState.Judged;
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
				note.Head.Scale = Vector2.One * noteScale;
				if (note.NoteInfo.Type != NoteType.Hold) continue;
				note.Body.Scale = Vector2.One * flipFactor * noteScale;
				note.Tail.Scale = Vector2.One * flipFactor * noteScale;
			}

			return new Vector2(newScale, newScale);
		}

		public void Init(ChartRpe chart, int lineIndex)
		{
			_chart = chart;
			_lineIndex = lineIndex;


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

			CalcTime(0);
			foreach (var note in NoteInstances)
			{
				note.Visible = true;
			}
		}

	}
}