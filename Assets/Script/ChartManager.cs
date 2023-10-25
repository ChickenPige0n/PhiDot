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
		public Label GameModeInfo;

		[Export]
		public Label ComboInfo;
		public int Combo;
		public int Time;

		[Export]
		public TextureRect BackGroundImage;

		[Export]
		public AudioStreamPlayer Music;



        public partial class ChartData
        {
            //曲绘绝对路径
            public string ImageSource { get; set; }
            public string MusicFileName { get; set; }
            public string ChartFileName { get; set; }
            
            public string ChartName { get; set; }
            public string ChartDiff { get; set; }
            public string ChartPath { get; set; }

            public string Composer { get; set; }
            public string Charter { get; set; }

            public string Illustrator { get; set; }

			public ChartData()
        	{
        	    ImageSource   = string.Empty;
        	    MusicFileName = string.Empty;
        	    ChartFileName = string.Empty;
        	    ChartName     = string.Empty;
        	    ChartDiff     = string.Empty;
        	    ChartPath     = string.Empty;
        	    Composer      = string.Empty;
        	    Charter       = string.Empty;
        	    Illustrator   = string.Empty;
        	}
			public static ChartData FromString(string RootDir, string infoContent)
			{
				ChartData cd = new ChartData();
            	string[] lines = infoContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            	foreach (string line in lines)
            	{
            	    string[] infos = line.Split(new[] { ':' });
            	    switch (infos[0].Trim())
            	    {
            	        case "Name":
            	            cd.ChartName = infos[1].Trim();
            	            break;
            	        case "Path":
            	            cd.ChartPath = infos[1].Trim();
            	            break;
            	        case "Picture":
            	            cd.ImageSource = Path.Combine(RootDir,infos[1].Trim());
            	            break;
            	        case "Level":
            	            cd.ChartDiff = infos[1].Trim();
            	            break;
            	        case "Song":
            	            cd.MusicFileName = infos[1].Trim();
            	            break;
            	        case "Chart":
            	            cd.ChartFileName = infos[1].Trim();
            	            break;
            	        case "Composer":
            	            cd.Composer = infos[1].Trim();
            	            break;
            	        case "Charter":
            	            cd.Charter = infos[1].Trim();
            	            break;
            	        default:
            	            break;
            	    }
            	}
				return cd;
			}
		}

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
			GameModeInfo.Text = "Autoplay";
			bool loadSuccess = LoadChart("res://Assets/ExampleChart/35461163/");
		}
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			ComboInfo.Text = Combo.ToString();
			ScoreLabel.Text = Score.ToString("D7");

			if (Combo >= 3)
			{
				GameModeInfo.Visible = true;
				ComboInfo.Visible = true;
			}
			else
			{
				GameModeInfo.Visible = false;
				ComboInfo.Visible = false;
			}
		}
	}
}