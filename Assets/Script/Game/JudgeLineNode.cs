using Godot;
using Phidot.ChartStructure;
using System.Collections.Generic;
using Phidot.Game;
using System;
using YamlDotNet.Core.Tokens;
using System.Diagnostics;
using System.Linq;


namespace Phidot.Game
{
	public partial class JudgeLineNode : Sprite2D
	{




		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{

		}


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

		public const double BAD_RANGE = 0.22;
		public float NoteJudgeSize = 100;
		public (NoteNode note, float dx, double dt)? HandleTouch(Vector2 pos, double realTime)
		{
			Idex.Visible = true;
			pos = ((pos * GetCanvasTransform()) - this.GlobalPosition).Rotated(-Rotation);

			(NoteNode note, float dx, double dt)? closest = null;

			foreach (var instance in noteInstances)
			{

				var dt = Math.Abs(realTime - instance.NoteInfo.StartTime.RealTime);
				var dx = Math.Abs(instance.Position.X - pos.X);
				if (!instance.Touched && dt < BAD_RANGE && dx < NoteJudgeSize
					&& (!closest.HasValue || dx < closest.Value.dx))
				{
					closest = (instance, dx, dt);
				}
			}

			return closest;
		}

		private double chartTime;
		public void CalcTime(double realTime)
		{
			var a = Chart.RealTime2BeatTime(realTime);
			var dChartTime = a - chartTime;
			chartTime = a;

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
			foreach (NoteNode instance in noteInstances)
			{
				var note = instance.NoteInfo;

				if (chartTime > note.EndTime)
				{
					instance.Visible = false;
					continue;
				}
				var EventLayerList = Chart.JudgeLineList[LineIndex].EventLayers;
				var newY = StageSize.Y / 7.5f * note.Speed * (float)(EventLayerList.GetCurSu(realTime) - note.FloorPosition);
				newY = note.Above == 1 ? newY : -newY;
				instance.Position = new Vector2(instance.Position.X, chartTime >= note.StartTime ? 0 : newY);
				if (note.Type == NoteType.Hold)
				{
					if (chartTime >= note.StartTime && chartTime <= note.EndTime)
					{
						instance.HoldTimer += dChartTime;
						if (instance.HoldTimer > 0.5f)
						{
							instance.EmitSignal(NoteNode.SignalName.OnJudjed, GlobalPosition, (int)JudgeType.Perfect, (int)NoteType.Hold, false);
							instance.HoldTimer = 0;
						}
					}

					var end = StageSize.Y / 7.5f * note.Speed * (float)(EventLayerList.GetCurSu(realTime) - note.CeliPosition);
					end = note.Above == 1 ? end : -end;

					if (chartTime >= note.StartTime) instance.Head.Visible = false;

					instance.Body.Scale = new Vector2(instance.Body.Scale.X, (instance.Position.Y - end) / instance.Body.Texture.GetSize().Y);
					instance.Tail.Position = new Vector2(0, end - instance.Position.Y);
				}
			}

		}
		public Vector2 CalcScale()
		{
			float newScale = 2.5f * StageSize.DistanceTo(Vector2.Zero) / this.TextureLine.Texture.GetWidth();
			TextureLine.Scale = Vector2.One * newScale;

			// TODO:
			// Support mutable note size.
			var noteScale = StageSize.X * 175 / (1350 * CurPack.TapTexture.GetSize().X);
			foreach (var note in noteInstances)
			{
				var flipFactor = note.NoteInfo.Above == 1 ? 1 : -1;
				note.Head.Scale = Vector2.One * noteScale;
				if (note.NoteInfo.Type == NoteType.Hold)
				{
					note.Body.Scale = Vector2.One * flipFactor * noteScale;
					note.Tail.Scale = Vector2.One * flipFactor * noteScale;
				}
			}

			return new Vector2(newScale, newScale);
		}

		public void Init(ChartRPE chart, int lineIndex)
		{
			Chart = chart;
			LineIndex = lineIndex;


			var ResPackManager = GetNode<ResPackManager>("/root/ResPackManager");
			CurPack = ResPackManager.CurPack;

			WindowSize = DisplayServer.WindowGetSize();
			StageSize = new Vector2I((int)((double)WindowSize.Y * AspectRatio), (int)WindowSize.Y);
			CalcScale();

			TextureLine.Modulate = new(0xecebb0e7);
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
						instance.ZIndex = 1;
						instance.Head.Texture = instance.NoteInfo.IsHighLight ? CurPack.TapHLTexture : CurPack.TapTexture;
						break;
					case NoteType.Hold:
						instance.ZIndex = 0;
						var (head, body, tail) = instance.NoteInfo.IsHighLight ? CurPack.HoldHLTextures : CurPack.HoldTextures;
						instance.Head.Texture = head;
						instance.Body.Texture = body;
						instance.Tail.Texture = tail;
						instance.Head.Offset = new Vector2(0, head.GetSize().Y / 2);
						instance.Body.Offset = new Vector2(0, -body.GetSize().Y / 2);
						instance.Tail.Offset = new Vector2(0, -tail.GetSize().Y / 2);
						break;
					case NoteType.Flick:
						instance.ZIndex = 3;
						instance.Head.Texture = instance.NoteInfo.IsHighLight ? CurPack.FlickHLTexture : CurPack.FlickTexture;
						break;
					case NoteType.Drag:
						instance.ZIndex = 2;
						instance.Head.Texture = instance.NoteInfo.IsHighLight ? CurPack.DragHLTexture : CurPack.DragTexture;
						break;
					default:
						break;
				}
				var posX = noteInfo.PositionX * (StageSize.X / 1350.0f);
				instance.Position = new Vector2(posX, 0);

				AddChild(instance);
			}

			CalcTime(0);
			foreach (var note in noteInstances)
			{
				note.Visible = true;
			}
		}

	}
}