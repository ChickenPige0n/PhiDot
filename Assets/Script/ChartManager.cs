using Godot;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using Phigodot.ChartStructure;

namespace Phigodot.Game
{
	// 主程序类
	public partial class ChartManager : Node
	{
		// 声明变量
		public RPEChart Chart;

		[Export]
		public Label ScoreInfo;
		public int Points = 0;

		[Export]
		public Label GameModeInfo;
		public int GameMode = 0;

		[Export]
		public Label ComboInfo;
		public int Combo;
		public int Time;

		// 生成函数
		public void Load_Node(int Type, float X = 0, float Y = 0, float Speed = 0, float Long = 0, string Strings = "", int Mode = 0)  // 生成游戏对象函数
		{
			// X.Max = 1152 
			X += 576;
			switch (Type)
			{
				case 0:  // 设置背景图
					{
						GetNode<TextureRect>("Backgroun").Texture = (Texture2D)GD.Load<Texture>(Strings);
						break;
					}
				case 1:  // 生成Tap
					{
						Node2D Node_Info = (Node2D)((PackedScene)ResourceLoader.Load("res://Assets/Scene/Tap.tscn")).Instantiate();
						Node_Info.Position = new Vector2(X, Y - (Node_Info.Scale.Y * 53.34f));
						var Set_Speed = (Tap)Node_Info;
						Set_Speed.Speed = Speed;
						GetNode("Backgroun/Game").CallDeferred("add_child", Node_Info);
						break;
					}
				case 2:  // 生成Drag
					{
						Node2D Node_Info = (Node2D)((PackedScene)ResourceLoader.Load("res://Assets/Scene/Drag.tscn")).Instantiate();
						Node_Info.Position = new Vector2(X, Y - (Node_Info.Scale.Y * 33.34f));
						var Set_Speed = (Drag)Node_Info;
						Set_Speed.Speed = Speed;
						GetNode("Backgroun/Game").CallDeferred("add_child", Node_Info);
						break;
					}
				case 3:  // 生成Flick
					{
						Node2D Node_Info = (Node2D)((PackedScene)ResourceLoader.Load("res://Assets/Scene/Flick.tscn")).Instantiate();
						Node_Info.Position = new Vector2(X, Y - (Node_Info.Scale.Y * 53.34f));
						var Set_Speed = (Flick)Node_Info;
						Set_Speed.Speed = Speed;
						GetNode("Backgroun/Game").CallDeferred("add_child", Node_Info);
						break;
					}
				case 4:  // 生成Hold
					{
						Node2D Node_Info = (Node2D)((PackedScene)ResourceLoader.Load("res://Assets/Scene/Hold.tscn")).Instantiate();
						Node_Info.Scale = new Vector2(0.15f, Long);
						Node_Info.Position = new Vector2(X, Y - (Long * 1000));
						GetNode("Backgroun/Game").CallDeferred("add_child", Node_Info);
						break;
					}
				case 5:  // 生成判定线
					{
						Node2D Node_Info = (Node2D)((PackedScene)ResourceLoader.Load("res://Assets/Scene/Determine_Line.tscn")).Instantiate();
						Node_Info.Position = new Vector2(X, Y);
						GetNode("Backgroun/Game").CallDeferred("add_child", Node_Info);
						break;
					}
				case 6:  // 加载歌曲信息
					{
						GetNode<Label>("Backgroun/UI/UI_Down/UI_Down/UI_Left/UI_Down/Song").Text = Strings;
						break;
					}
				case 7:  // 加载难度信息
					{
						GetNode<Label>("Backgroun/UI/UI_Down/UI_Down/UI_Right/UI_Down/Difficulty").Text = Strings;
						break;
					}
				case 8: // 加载模式
					{
						if (Mode == 0)  // 正常模式
						{
							GameMode = Mode;
						}
						else if (Mode == 1)  // 自动模式
						{
							GameMode = Mode;
						}
						break;
					}
				case 9:  // 加载音乐
					{
						GetNode<AudioStreamPlayer>("Backgroun/Game/Music").Stream = (AudioStream)GD.Load(Strings);
						break;
					}
			}
		}

		public void Load_Json(string Json_File)
		{
			string json = File.ReadAllText(Json_File);
			Chart = JsonConvert.DeserializeObject<RPEChart>(json);
		}
		public void Timer()
		{
			while (true)
			{
				Time++;
				Thread.Sleep(10);
			}
		}
		public void Load_Note()
		{
			// todo
		}
		public void Load_Event()
		{
		}
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			Load_Node(0, Strings: "res://Assets/Image/Song_Cover/Test.png");
			Load_Node(6, Strings: "测试");
			Load_Node(7, Strings: "EZ Lv.-999");
			Load_Node(8, Mode: 1);
			Load_Node(5, 0, 600);
			Load_Json(@"F:\bei_fen\QaQ\phigros\1\TextAsset\0.json");
			// new Thread(new ThreadStart(Timer)).Start();
			// new Thread(new ThreadStart(Load_Note)).Start();
		}
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			if (Combo >= 3)  // 显示连击数
			{
				if (GameMode == 0)  // 显示COMBO
				{
					GameModeInfo.Text = "COMBO";
				}
				else if (GameMode == 1)  // 显示Auto Play
				{
					GameModeInfo.Text = "Auto Play";
				}
				ComboInfo.Text = Combo.ToString();
			}
			else  // 隐藏连击数
			{
				GameModeInfo.Text = "";
				ComboInfo.Text = "";
			}
			ScoreInfo.Text = Points.ToString("D7");  // 显示分数
		}
	}
}