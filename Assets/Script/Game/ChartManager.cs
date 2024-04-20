using Godot;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using Phidot.ChartStructure;
using Color = Godot.Color;
using System.Linq;
using System.Diagnostics;
using System;

namespace Phidot.Game
{
	public partial class ChartManager : Control
	{
		public bool isAutoPlay = false;

		private ResPackManager resPackManager;

		public ChartRPE Chart;

		public ChartData chartData;

		public double AspectRatio = 1.666667d;


		#region UIReferences
		[Export] public Label ScoreLabel;
		public int Score = 0;
		[Export] public Label SongNameLabel;
		[Export] public Label DiffLabel;
		[Export] public Label GameModeLabel;
		[Export] public Label ComboLabel;
		public int Combo;
		[Export] public Sprite2D BackGroundImage;

		[Export] public ProgressBar progressBar;

		[Export] public Control PauseUI;

		[Export] public MarginContainer LabelUI;
		[Export] public MarginContainer ComboUI;

		[Export] public TextureButton PauseBtn;
		#endregion

		public Vector2 WindowSize;
		public Vector2 StageSize;


		#region SFX

		[Export] public AudioStreamPlayer Music;
		[Export] public AudioStreamPlayer TapSFX;
		[Export] public AudioStreamPlayer FlickSFX;
		[Export] public AudioStreamPlayer DragSFX;

		#endregion

		[Export] public PackedScene HEScene;
		[Export] public PackedScene JudgeLineScene;



		public List<JudgeLineNode> JudgeLineInstances = new List<JudgeLineNode>();


		public double PlaybackTime;
		public double Time;

		public bool isPlaying = false;

		#region LoadChart
		// Called when the node enters the scene tree for the first time.
		public void LoadFromDir(string dir)
		{
			Vector2 newSize = DisplayServer.WindowGetSize();
			StageSize = new Vector2I((int)((double)newSize.Y * AspectRatio), (int)newSize.Y);


			GameModeLabel.Text = isAutoPlay ? "AUTOPLAY" : "COMBO";
			var absPath = ProjectSettings.GlobalizePath(dir);

			LoadChart(absPath);

			resPackManager = GetNode<ResPackManager>("/root/ResPackManager");
			var curPack = resPackManager.CurPack;
			var HEInstance = HEScene.Instantiate<AnimatedSprite2D>();
			HEInstance.SpriteFrames = curPack.HitEffectFrames;

			var Scene = new PackedScene();
			Scene.Pack(HEInstance);
			HEScene = Scene;

			CalcUISize();
			PlayChart();
		}
		public void LoadChart(string RootDir)
		{
			string infoPath = Path.Combine(RootDir, "info.txt");
			string infoContent = File.ReadAllText(infoPath);
			chartData = ChartData.FromString(RootDir, infoContent);
			// TODO: Read from JSON META
			BackGroundImage.Texture = (Texture2D)GD.Load<Texture>(chartData.ImageSource);
			Music.Stream = (AudioStream)GD.Load(Path.Combine(RootDir, chartData.MusicFileName));
			SongNameLabel.Text = chartData.ChartName;
			DiffLabel.Text = chartData.ChartDiff;

			string jsonText = File.ReadAllText(Path.Combine(RootDir, chartData.ChartFileName));
			Chart = JsonConvert.DeserializeObject<ChartRPE>(jsonText);
			GD.Print(Chart.JudgeLineList.Count);
			// Must done as initialization
			Chart.PreCalculation();
			foreach (var Line in Chart.JudgeLineList)
			{
				int i = Chart.JudgeLineList.IndexOf(Line);
				var LineInstance = JudgeLineScene.Instantiate() as JudgeLineNode;
				LineInstance.fingerDatas = fingerDatas;
				AddChild(LineInstance);
				LineInstance.Init(Chart, i);
				LineInstance.ZIndex = Chart.JudgeLineList[i].ZOrder;
				LineInstance.AspectRatio = this.AspectRatio;
				JudgeLineInstances.Add(LineInstance);

				foreach (NoteNode note in LineInstance.noteInstances)
				{
					note.OnJudged += JudgedEventHandler;
				}
			}
		}
		#endregion

		#region ChartControl

		public void Pause()
		{
			isPlaying = !isPlaying;
			PauseUI.Visible = !PauseUI.Visible;
			if (isPlaying)
			{
				Music.Play();
				Music.Seek((float)Time);
			}
			else
			{
				Music.Stop();
			}
		}


		public void SeekTo(double RealTime)
		{
			RealTime = RealTime < 0 ? 0 : RealTime;

			foreach (var line in JudgeLineInstances)
			{
				foreach (var noteNode in line.noteInstances)
				{
					if (noteNode.State != JudgeState.NotJudged && RealTime <= noteNode.NoteInfo.StartTime.RealTime)
					{
						if (RealTime <= Time) Chart.JudgeData.RevertJudge();
						noteNode.State = JudgeState.NotJudged;
						noteNode.Visible = true;
						noteNode.Head.Visible = true;
						if (noteNode.NoteInfo.Type == NoteType.Hold)
						{
							noteNode.Body.Visible = true;
							noteNode.Tail.Visible = true;
							noteNode.Modulate = new Color(1, 1, 1, 1);
						}
					}
				}
			}

			Time = RealTime;
			Music.Seek((float)RealTime);

		}

		public void Restart()
		{
			PauseUI.Visible = !PauseUI.Visible;
			Chart.JudgeData.RevertJudge();
			ComboLabel.Text = "0";
			Combo = 0;
			ScoreLabel.Text = "0000000";
			Score = 0;
			progressBar.Value = 0;
			SeekTo(0);
			PlayChart();
		}

		public void Exit()
		{
			QueueFree();
		}

		#endregion

		#region StartChart
		public void PlayChart()
		{
			LabelUI.Size = StageSize + Vector2.One * 400;
			LabelUI.Position = Vector2.One * -200;

			GameModeLabel.Visible = false;
			ComboLabel.Visible = false;
			var tween = LabelUI.CreateTween().SetParallel(true);
			foreach (JudgeLineNode line in JudgeLineInstances)
			{
				var Scale = line.CalcScale();
				line.TextureLine.Scale = new Vector2(0, Scale.Y);
				tween.TweenProperty(line.TextureLine, "scale", Scale, 1.5d).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Cubic);
				line.CalcTime(0.0f);
			}
			tween.TweenProperty(LabelUI, "size", StageSize, 1.0d).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quint);
			tween.TweenProperty(LabelUI, "position", Vector2.Zero, 1.0d).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quint);
			tween.TweenCallback(
			Callable.From
			(() =>
			{
				isPlaying = true;
				Music.Play();
			})).SetDelay(1.0f);
			tween.Play();
		}
		#endregion




		public override void _Ready()
		{
			base._Ready();
		}
		#region Tick
		public override void _Process(double delta)
		{
			CalcUISize();

			PlaybackTime = Music.GetPlaybackPosition();
			progressBar.Value = PlaybackTime / Music.Stream.GetLength();

			if (isPlaying)
			{

				Time += delta;

				foreach (JudgeLineNode line in JudgeLineInstances)
				{
					line.CalcTime(Time);
				}

				ComboLabel.Text = Combo.ToString();
				ScoreLabel.Text = Score.ToString("D7");

				if (Combo >= 3)
				{
					GameModeLabel.Visible = true;
					ComboLabel.Visible = true;
				}
				else
				{
					GameModeLabel.Visible = false;
					ComboLabel.Visible = false;
				}

			}


			// Time Control
			if (isAutoPlay)
			{
				if (Input.IsActionJustPressed("seekforward"))
				{
					SeekTo(Time + 5);
				}
				if (Input.IsActionJustPressed("seekback"))
				{
					SeekTo(Time - 5);
				}
			}
		}
		#endregion

		#region UISizing
		public void CalcUISize()
		{

			Vector2 newSize = DisplayServer.WindowGetSize();
			StageSize = new Vector2I((int)((double)newSize.Y * AspectRatio), (int)newSize.Y);
			if (!newSize.IsEqualApprox(WindowSize))
			{
				foreach (var line in JudgeLineInstances)
				{
					line.WindowSize = newSize;
					line.StageSize = StageSize;
					line.CalcScale();
				}
			}
			WindowSize = DisplayServer.WindowGetSize();

			BackGroundImage.Scale = StageSize / BackGroundImage.Texture.GetSize();

			Size = StageSize;
			Position = (WindowSize - StageSize) / 2;


			if (isPlaying)
			{
				LabelUI.Size = StageSize;
			}
			var Ratio = StageSize.Y / 648;
			ScoreLabel.LabelSettings.FontSize = (int)(30 * Ratio);
			ComboLabel.LabelSettings.FontSize = (int)(40 * Ratio);
			GameModeLabel.LabelSettings.FontSize = (int)(20 * Ratio);
			PauseBtn.CustomMinimumSize = Vector2.One * 25 * Ratio;
			LabelUI.AddThemeConstantOverride("margin_top", (int)(25 * Ratio));
			LabelUI.AddThemeConstantOverride("margin_right", (int)(25 * Ratio));
			LabelUI.AddThemeConstantOverride("margin_left", (int)(25 * Ratio));
			LabelUI.AddThemeConstantOverride("margin_bottom", (int)(25 * Ratio));
			ComboUI.AddThemeConstantOverride("margin_top", (int)(15 * Ratio));
		}
		#endregion

		#region Judge
		public const double BAD_RANGE = 0.22;
		public const double GOOD_RANGE = 0.16;
		public const double PERFECT_RANGE = 0.08;

		public List<FingerData> fingerDatas = new();
		public class FingerData
		{
			public Vector2 curPos;
			public Vector2 curVec;

			public FingerData(Vector2 Pos = new Vector2(), Vector2 Vec = new Vector2())
			{
				curPos = Pos;
				curVec = Vec;
			}
		}
		public override void _Input(InputEvent @event)
		{
			base._Input(@event);
			if (@event is InputEventScreenTouch)
			{
				var touch = @event as InputEventScreenTouch;
				if (!touch.Pressed && fingerDatas.Count > touch.Index)
				{
					fingerDatas.RemoveAt(touch.Index);
					return;
				}
				fingerDatas.Add(new FingerData(touch.Position));

				List<(NoteNode note, float dx, double dt)> groups = new();
				foreach (var line in JudgeLineInstances)
				{
					var lineClosest = line.HandleClick(touch.Position, Time);

					if (lineClosest.HasValue) groups.Add(lineClosest.Value);
				}
				if (groups.Count == 0) return;

				(NoteNode note, float dx, double dt) selectedGroup = groups[0];
				// argmin(dx)
				foreach (var group in groups)
				{
					if (group.dx < selectedGroup.dx)
					{
						selectedGroup = group;
					}
				}

				JudgeType judgement;
				if (selectedGroup.dt < PERFECT_RANGE) judgement = JudgeType.Perfect;
				else if (selectedGroup.dt < GOOD_RANGE) judgement = JudgeType.Good;
				else judgement = JudgeType.Bad;


				selectedGroup.note.EmitOnJudged(judgement, selectedGroup.note.NoteInfo.Type, true);
				selectedGroup.note.State = selectedGroup.note.NoteInfo.Type == NoteType.Hold ? JudgeState.Holding : JudgeState.Judged;
			}
			else if (@event is InputEventScreenDrag)
			{
				var dragEvent = @event as InputEventScreenDrag;
				fingerDatas[dragEvent.Index].curPos += dragEvent.Relative;
				fingerDatas[dragEvent.Index].curVec = dragEvent.Velocity;
			}
		}
		#endregion

		#region HitEffect
		public Color PerfectColor = new(0xecebb0e7);
		public Color GoodColor = new(0xb4e1ffeb);
		public void JudgedEventHandler(Vector2 globalPosition, JudgeType judgeType, NoteType noteType, bool shouldSoundAndRecord = true)
		{
			if (shouldSoundAndRecord)
			{
				Chart.JudgeData.Judge(judgeType);
			}
			Combo = Chart.JudgeData.MaxCombo;
			Score = Chart.JudgeData.CalcScore();

			AnimatedSprite2D effectInstance = HEScene.Instantiate<AnimatedSprite2D>();
			Color color = PerfectColor;
			switch (judgeType)
			{
				case JudgeType.Perfect:
					color = PerfectColor;
					break;
				case JudgeType.Good:
					color = GoodColor;
					break;
				default:
					break;
			}

			if (shouldSoundAndRecord && judgeType != JudgeType.Miss)
			{
				switch (noteType)
				{
					case NoteType.Tap:
						TapSFX.Play();
						break;
					case NoteType.Hold:
						TapSFX.Play();
						break;
					case NoteType.Flick:
						FlickSFX.Play();
						break;
					case NoteType.Drag:
						DragSFX.Play();
						break;
				}
			}
			effectInstance.Modulate = color;


			var curPack = resPackManager.CurPack;
			var Scale = 1.5f * curPack.HitFxScale * (StageSize.X * 175 / (1350 * effectInstance.SpriteFrames.GetFrameTexture("default", 0).GetSize().X));
			effectInstance.Scale = Vector2.One * Scale;

			effectInstance.Position = globalPosition - (WindowSize - StageSize) / 2;
			AddChild(effectInstance);
		}
		#endregion

	}
}