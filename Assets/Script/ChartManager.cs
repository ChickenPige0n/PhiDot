using Godot;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using Phigodot.ChartStructure;
using Microsoft.VisualBasic;
using System.Drawing;

namespace Phigodot.Game
{
	// 主程序类
	public partial class ChartManager : Control
	{
		public RPEChart Chart;

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

		[Export] public AudioStreamPlayer Music;

		[Export] public PackedScene JudgeLineScene;


		public List<JudgeLineNode> judgeLineNodes = new List<JudgeLineNode>();


		public double PlaybackTime;
		public double Time;

		public bool isPlaying = false;

		public void Pause()
		{
			isPlaying = !isPlaying;
			PauseUI.Visible = !PauseUI.Visible;
        	if(isPlaying)
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
			isPlaying = true;
            Music.Play();
			Time = 0.0;
			PlaybackTime = 0.0;
		}

		public void Exit()
		{
			QueueFree();
		}



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
				int i = Chart.judgeLineList.IndexOf(Line);
				var LineInstance = JudgeLineScene.Instantiate() as JudgeLineNode;

				LineInstance.EventLayers = Chart.judgeLineList[i].eventLayers;
				LineInstance.AspectRatio = this.AspectRatio;
				AddChild(LineInstance);
				judgeLineNodes.Add(LineInstance);
			}
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
			var absPath = ProjectSettings.GlobalizePath("res://Assets/ExampleChart/35461163");
			LoadChart(absPath);

			PlayChart();
		}
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			Vector2I windowSize = DisplayServer.WindowGetSize();
			Vector2I StageSize = new Vector2I((int)((double)windowSize.Y*AspectRatio),(int)windowSize.Y);

			BackGroundImage.Scale = StageSize/BackGroundImage.Texture.GetSize();

			this.Size = StageSize;
			this.Position = (windowSize - StageSize)/2;

			if (isPlaying)
			{
				Time += delta;
				PlaybackTime = Music.GetPlaybackPosition();
				progressBar.Value = PlaybackTime/Music.Stream.GetLength();
				
				double ChartTime = Chart.RealTime2ChartTime(Time);

				foreach(JudgeLineNode line in judgeLineNodes)
				{
					line.ChartTime = ChartTime;
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