using System;
using System.Collections.Generic;
using System.IO;
using Godot;

public partial class SceneManager : Node2D
{
	[Export] private Control _uiControl;
	private List<ChartDisplayElement> _displayList = new();
	[Export] private Control _listControl;
	[Export] private Label _diffLabel;
	[Export] private Label _charterLabel;
	[Export] private Label _songNameLabel;
	[Export] private Label _composerLabel;
	[Export] private HSlider _slider;
	[Export] private TextureRect _bgTexture;

	private int _i;
	private int CurIndex
	{
		get => _i;
		set
		{
			if (value > _displayList.Count - 1 || value < 0) return;
			var ratio = 1.0f;//DisplayServer.WindowGetSize().Y / 648.0f;
			var selectedSize = new Vector2(280 * 1.77778f, 280) * ratio;
			var normalSize = selectedSize * 0.64f;
			var heightOffset = 120 * ratio;
			var seperation = 30;
			
			var tween = CreateTween().SetParallel();
			
			for (var i = 0; i < _displayList.Count; i++)
			{
				var item = _displayList[i];
				Vector2 targetSize = normalSize;
				Vector2 targetPos;
				if (i == value)
				{
					targetSize = selectedSize;
					targetPos = Vector2.Zero;
				}
				else if (i < value)
				{
					targetPos = new Vector2(
						(i - value) * (seperation + normalSize.X),
						heightOffset
					);
				}
				else
				{
					targetPos = new Vector2(
						selectedSize.X + (i - value) * seperation + (i - value - 1) * normalSize.X,
						heightOffset
					);
					
				}
				
				Modulate = new Color(1, 1, 1, 0);
				tween
					.TweenProperty(item, "position:x", targetPos.X, 1.0d)
					.SetEase(Tween.EaseType.Out)
					.SetTrans(Tween.TransitionType.Expo);
			}
			
			tween.Play();


			var cur = _displayList[value].Data;
			_diffLabel.Text = cur.ChartDiff;
			_charterLabel.Text = $"谱面设计:{cur.Charter}";
			_songNameLabel.Text = cur.ChartName;
			_composerLabel.Text = cur.Composer;
			_bgTexture.Texture = _displayList[value].Texture;
			
			
			_i = value;
		}
	}
	

	public override void _Ready()
	{
		Engine.MaxFps = 144;
		var charts = Directory.GetDirectories("D:\\Projects\\godot\\godot\\PhiGodot\\Assets\\ExampleChart");
		foreach (var chart in charts)
		{
			if(!File.Exists(Path.Combine(chart, "info.txt"))) continue;
			var element = new ChartDisplayElement(chart);
			_listControl.AddChild(element);
			_displayList.Add(element);
		}
		_slider.MaxValue = _displayList.Count - 1;
		_slider.ValueChanged += value => CurIndex = (int)Math.Round(value);
		_slider.Value = 0;
		CurIndex = 0;
	}

	private partial class ChartDisplayElement : TextureRect
	{
		public ChartData Data;
		public ChartDisplayElement(string path)
		{
			var ratio = 1.0f;//DisplayServer.WindowGetSize().Y / 648.0f;
			var normalSize = new Vector2(280f, 280 * 1.77778f) * ratio * 0.64f;

			Size = normalSize;
			ExpandMode = ExpandModeEnum.IgnoreSize;
			StretchMode = StretchModeEnum.KeepAspectCovered;
			Data = ChartData.FromString(path, File.ReadAllText(Path.Combine(path, "info.txt")));
            //ResourceLoader.LoadThreadedRequest(Data.ImageSource);
            Texture = (Texture2D)GD.Load<Texture>(Data.ImageSource);
		}

		public override void _Process(double delta)
		{
            var ratio = 1.0f;//DisplayServer.WindowGetSize().Y / 648.0f;
			var selectedSize = new Vector2(280 * 1.77778f, 280) * ratio;
			var normalSize = selectedSize * 0.64f;
			var heightOffset = 120 * ratio;
			var seperation = 30;
			var lb = -normalSize.X - seperation;
			var rb =  selectedSize.X + seperation;
			const double eps = 1e-3;
			var x = (float x) =>
			{
				if (x > lb && x <= eps) return (1 - Easings.EaseInOutCubic((x - lb) / -lb))* heightOffset;
				if (x < rb && x >= -eps) return Easings.EaseInOutCubic(x / rb) * heightOffset;
				return heightOffset;
			};
			Position = new Vector2(Position.X, (float)x(Position.X));

			float SizeFunc(float x)
			{
				if (x > lb && x <= eps) return (float)Easings.EaseInOutCubic((x - lb) / -lb) * 0.36f + 0.64f;
				if (x < rb && x >= -eps) return (1.0f - (float)Easings.EaseInOutCubic(x / rb)) * 0.36f + 0.64f;
				return 0.64f;
			}
			Size = SizeFunc(Position.X) * selectedSize;
			

		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_listControl.Scale = Vector2.One * 1.0f;
		if (Input.IsActionJustPressed("scroll_down"))
		{
			CurIndex += 1;
			_slider.Value = CurIndex;
		}
		if (Input.IsActionJustPressed("scroll_up"))
		{
			CurIndex -= 1;
			_slider.Value = CurIndex;
		}
	}
	

	[Export]

	public PackedScene GameScene;
	public void Load(string loadType)
	{
		OS.RequestPermissions();
		var path = OS.GetExecutablePath();

		switch (OS.GetName())
		{
			case "Windows":
				GD.Print("Windows");
				path = "C:\\";
				break;
			case "macOS":
				GD.Print("macOS");
				break;
			case "Linux":
				GD.Print("Linux");
				break;
			case "BSD":
				GD.Print("Linux/BSD");
				break;
			case "Android":
				GD.Print("Android");
				path = "/storage/emulated/0/";
				break;
			case "iOS":
				GD.Print("iOS");
				break;
			case "Web":
				GD.Print("Web");
				break;
		}

		if (loadType.Contains("chart"))
		{
			ChartSelected(_displayList[CurIndex].Data.Root);
			_bgTexture.Visible = false;
			_uiControl.Visible = false;
			return;
		}
		FileDialog fd = new()
		{
			Size = new Vector2I(900, 600),
			CurrentDir = path,
			Access = FileDialog.AccessEnum.Filesystem
		};
		AddChild(fd);
		fd.Popup();
		fd.FileMode = FileDialog.FileModeEnum.OpenDir;
		fd.DirSelected +=
			loadType.Contains("chart") ? ChartSelected :
			loadType.Contains("res_pack") ? ResPackSelected :
			ChartSelected;    // Default
	}
	public void ResPackSelected(string path)
	{
		var resPackManager = GetNode<ResPackManager>("/root/ResPackManager");
		resPackManager.LoadFromDir(path);
		resPackManager.SetCurPack(resPackManager.ResPackList.Count - 1);
	}

	

	public void ChartSelected(string path)
	{
		var scene = GameScene.Instantiate<ChartManager>();
		scene.OnExit += GameExited;
		GetNode("/root").AddChild(scene);
		path = ProjectSettings.GlobalizePath(path);
		scene.LoadFromDir(path);
	}

	public void GameExited(JudgeManager judgeData)
	{
		_bgTexture.Visible = true;
		_uiControl.Visible = true;
	}
	
	

	[Export] public LineEdit AddressEdit;
	public async void OnConnectRemote()
	{
		var scene = GameScene.Instantiate<ChartManager>();
		scene.OnExit += GameExited;
		scene.Visible = false;
		GetNode("/root").AddChild(scene);
		await scene.ConnectChartServer(AddressEdit.Text);
		scene.Visible = true;
	}

}
