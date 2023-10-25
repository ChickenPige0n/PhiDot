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

		[Export]
		public Label ScoreLabel;
		public int Score = 0;

		[Export]
		public Label SongNameLabel;

		[Export]
		public Label DiffLabel;

		[Export]
		public Label GameModeLabel;

		[Export]
		public Label ComboLabel;
		public int Combo;
		public double Time;

		[Export]
		public TextureRect BackGroundImage;

		[Export]
		public AudioStreamPlayer Music;



		public bool LoadChart(string RootDir)
		{
			string infoPath = Path.Combine(RootDir,"info.txt");
			
            string infoContent = File.ReadAllText(infoPath);
			chartData = ChartData.FromString(infoContent, RootDir);
			
			string jsonText = File.ReadAllText(chartData.ChartFileName);
			Chart = JsonConvert.DeserializeObject<RPEChart>(jsonText);

			
			BackGroundImage.Texture = (Texture2D)GD.Load<Texture>(chartData.ImageSource);
			Music.Stream = (AudioStream)GD.Load(chartData.MusicFileName);
			SongNameLabel.Text = chartData.ChartName;
			DiffLabel.Text = chartData.ChartDiff;

			return true;
		}

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			GameModeLabel.Text = "Autoplay";
			bool loadSuccess = LoadChart("res://Assets/ExampleChart/35461163/");
		}
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
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