using Godot;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using Phigodot.ChartStructure;
using Microsoft.VisualBasic;
using System.Drawing;
using Color = Godot.Color;
using Newtonsoft.Json.Linq;

namespace Phigodot.Game
{

	// 主程序类
	public partial class ChartManager : Control
	{


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
		#endregion

		public Vector2 WindowSize;
		public Vector2 StageSize;


		#region SFX

		[Export] public AudioStreamPlayer Music;
		[Export] public AudioStreamPlayer TapSFX;
		[Export] public AudioStreamPlayer FlickSFX;
		[Export] public AudioStreamPlayer DragSFX;

		#endregion

		[Export] public GpuParticles2D HitParticle;
		[Export] public PackedScene HEScene;
		[Export] public PackedScene JudgeLineScene;



		public List<JudgeLineNode> judgeLineInstances = new List<JudgeLineNode>();


		public double PlaybackTime;
		public double Time;

		public bool isPlaying = false;

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

		public void Restart()
		{
			PauseUI.Visible = !PauseUI.Visible;
			Time = 0.0;
			PlaybackTime = 0.0;
			Chart.JudgeData.RevertJudge();
			Combo = 0;
			Score = 0;
			foreach (var line in judgeLineInstances){
				foreach(var noteNode in line.noteInstances){
					noteNode.Judged = false;
				}
			}
			isPlaying = true;
			Music.Play();
		}

		public void Exit()
		{
			QueueFree();
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
			// Must done as initialization
			Chart.PreCalculation();
			foreach (var Line in Chart.JudgeLineList)
			{
				int i = Chart.JudgeLineList.IndexOf(Line);
				var LineInstance = JudgeLineScene.Instantiate() as JudgeLineNode;
				LineInstance.Init(Chart, i);
				LineInstance.AspectRatio = this.AspectRatio;
				AddChild(LineInstance);
				judgeLineInstances.Add(LineInstance);

				foreach (NoteNode note in LineInstance.noteInstances)
				{
					note.OnJudjed += JudgedEventHandler;
				}
			}
		}



		public Color PerfectColor = new(0.839216f, 0.741176f, 0.360784f, 1);
		public Color GoodColor = new(0x31cce1);
		public void JudgedEventHandler(Vector2 globalPosition, int judgeIntType, int noteIntType)
		{
			var judgeType = (JudgeType)judgeIntType;
			var noteType = (NoteType)noteIntType;
			
			Chart.JudgeData.Judge(judgeType);
			Combo = Chart.JudgeData.MaxCombo;
			Score = Chart.JudgeData.CalcScore();

			AnimatedSprite2D effectInstance = HEScene.Instantiate<AnimatedSprite2D>();
			Color color = PerfectColor;
			switch (judgeType)
			{
				case JudgeType.perfect:
					color = PerfectColor;
					break;
				case JudgeType.good:
					color = GoodColor;
					break;
				default:
					break;
			}

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

			effectInstance.Modulate = color;

			HitParticle.EmitParticle(new Transform2D(), globalPosition, color, color, 1);

			effectInstance.Position = globalPosition - (WindowSize - StageSize) / 2;
			AddChild(effectInstance);
		}

		public void PlayChart()
		{
			isPlaying = true;
			Music.Play();
		}

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			GameModeLabel.Text = "Autoplay";
			var absPath = ProjectSettings.GlobalizePath("res://Assets/ExampleChart/17666805");
			LoadChart(absPath);

			PlayChart();
		}
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			WindowSize = DisplayServer.WindowGetSize();
			StageSize = new Vector2I((int)((double)WindowSize.Y * AspectRatio), (int)WindowSize.Y);


			BackGroundImage.Scale = StageSize / BackGroundImage.Texture.GetSize();

			this.Size = StageSize;
			this.Position = (WindowSize - StageSize) / 2;

			if (isPlaying)
			{
				Time += delta;
				PlaybackTime = Music.GetPlaybackPosition();
				progressBar.Value = PlaybackTime / Music.Stream.GetLength();

				foreach (JudgeLineNode line in judgeLineInstances)
				{
					line.CalcTime(Time);
				}

				#region UI Calculate

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

				#endregion
			}
		}
	}
}