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

		// Pixel unit
		public Vector2I StageSize;
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
#endregion

		[Export] public AudioStreamPlayer Music;

		[Export] public PackedScene JudgeLineScene;


		public List<JudgeLineNode> judgeLineNodes = new List<JudgeLineNode>();


		public double PlaybackTime;
		public double Time;

		public bool isPlaying = false;

		public void Pause(){
			isPlaying = !isPlaying;
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

		public void LoadChart(string RootDir)
		{
			string infoPath = Path.Combine(RootDir,"info.txt");
            string infoContent = File.ReadAllText(infoPath);
			chartData = ChartData.FromString(RootDir, infoContent);
			// TODO: Read from JSON META
			BackGroundImage.Texture = (Texture2D)GD.Load<Texture>(chartData.ImageSource);
			BackGroundImage.Scale = StageSize/BackGroundImage.Texture.GetSize();
			BackGroundImage.Position = DisplayServer.WindowGetSize()/2;
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
				LineInstance.StageSize = this.StageSize;

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
			Vector2I windowSize = DisplayServer.WindowGetSize();
			
			StageSize = new Vector2I((int)((double)windowSize.Y*AspectRatio),(int)windowSize.Y);

			GameModeLabel.Text = "Autoplay";
			var absPath = ProjectSettings.GlobalizePath("res://Assets/ExampleChart/42853068/");
			LoadChart(absPath);

			PlayChart();
		}
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			if (isPlaying)
			{
				PlaybackTime = Music.GetPlaybackPosition();
				Time += delta;
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