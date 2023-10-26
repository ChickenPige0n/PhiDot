using Godot;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using Phigodot.ChartStructure;
using Microsoft.VisualBasic;

namespace Phigodot.Game
{
	// 主程序类
	public partial class ChartManager : Node
	{
		public RPEChart Chart;

		public ChartData chartData;
#region UIReferences
		[Export] public Label ScoreLabel;
		public int Score = 0;
		[Export] public Label SongNameLabel;
		[Export] public Label DiffLabel;
		[Export] public Label GameModeLabel;
		[Export] public Label ComboLabel;
		public int Combo;
		[Export] public TextureRect BackGroundImage;
#endregion

		[Export] public AudioStreamPlayer Music;

		[Export] public PackedScene JudgeLineScene;


		public List<JudgeLineNode> judgeLineNodes = new List<JudgeLineNode>();


		public double PlaybackTime;
		public double Time;

		public bool isPlaying = false;

		public void LoadChart(string RootDir)
		{
			string infoPath = Path.Combine(RootDir,"info.txt");
            string infoContent = File.ReadAllText(infoPath);
			chartData = ChartData.FromString(RootDir, infoContent);

			// TODO: Read from JSON META
			BackGroundImage.Texture = (Texture2D)GD.Load<Texture>(chartData.ImageSource);
			Music.Stream = (AudioStream)GD.Load(Path.Combine(RootDir,chartData.MusicFileName));
			SongNameLabel.Text = chartData.ChartName;
			DiffLabel.Text = chartData.ChartDiff;
			
			string jsonText = File.ReadAllText(Path.Combine(RootDir,chartData.ChartFileName));
			Chart = JsonConvert.DeserializeObject<RPEChart>(jsonText);
			
			foreach(var Line in Chart.judgeLineList)
			{
				var LineInstance = JudgeLineScene.Instantiate() as JudgeLineNode;
				AddChild(LineInstance);
				var Pos = LineInstance.Position;
				Pos = new Vector2(1000,500);
				LineInstance.Position = Pos;
				judgeLineNodes.Add(LineInstance);
			}
			

		}

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			GameModeLabel.Text = "Autoplay";
			var absPath = ProjectSettings.GlobalizePath("res://Assets/ExampleChart/35461163/");
			LoadChart(absPath);
		}
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			if (isPlaying)
			{
				PlaybackTime = Music.GetPlaybackPosition();
				Time += delta;
				
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
		}
	}
}