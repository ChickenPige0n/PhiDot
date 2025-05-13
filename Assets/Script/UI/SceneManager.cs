using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using FileAccess = Godot.FileAccess;

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
	[Export] private CheckButton _autoPlayButton;
	
	[Export] public AspectRatioContainer GameContainer;
	
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
	
	public void AddChartDisplay(DirAccess chartDir)
	{
		var element = new ChartDisplayElement(chartDir);
		_listControl.AddChild(element);
		_displayList.Add(element);
		CurIndex = 0;
	}

	public override void _Ready()
	{
		OS.RequestPermission("android.permission.READ_EXTERNAL_STORAGE");
		OS.RequestPermission("android.permission.WRITE_EXTERNAL_STORAGE");
		OS.RequestPermissions();
		OS.RequestPermission("android.permission.READ_EXTERNAL_STORAGE");
		OS.RequestPermission("android.permission.WRITE_EXTERNAL_STORAGE");
		
		Engine.MaxFps = 144;
		DirAccess.Open("user://").MakeDir("Charts");
		
		using var dir = DirAccess.Open("user://Charts");
		if (dir != null)
		{
			dir.ListDirBegin();
			var fileName = dir.GetNext();
			while (fileName != "")
			{
				if (!dir.CurrentIsDir())
				{
					fileName = dir.GetNext();
					continue;
				}
				var data = FileAccess.Open($"user://Charts/{fileName}/info.txt", FileAccess.ModeFlags.Read);
				if (data == null)
				{
					fileName = dir.GetNext();
					continue;
				}
				AddChartDisplay(DirAccess.Open($"user://Charts/{fileName}"));
				fileName = dir.GetNext();
			}
			dir.ListDirEnd();
		}
		_slider.MaxValue = _displayList.Count - 1;
		_slider.ValueChanged += value => CurIndex = (int)Math.Round(value);
		_slider.Value = 0;
		CurIndex = 0;
		
		if (!OS.HasFeature("movie")) return;
		_autoPlayButton.ButtonPressed = true;
		LoadSelectedChart();
	}

	private partial class ChartDisplayElement : TextureRect
	{
		public ChartData Data;
		public ChartDisplayElement(DirAccess chartDir)
		{
			var ratio = 1.0f;  //DisplayServer.WindowGetSize().Y / 648.0f;
			var normalSize = new Vector2(280f, 280 * 1.77778f) * ratio * 0.64f;

			Size = normalSize;
			ExpandMode = ExpandModeEnum.IgnoreSize;
			StretchMode = StretchModeEnum.KeepAspectCovered;
			
			GD.Print($"Loading ChartData from {chartDir.GetCurrentDir()}");
			var infoContent = FileAccess.Open($"{chartDir.GetCurrentDir()}/info.txt", FileAccess.ModeFlags.Read).GetAsText();
			Data = ChartData.FromString(chartDir, infoContent);
            //ResourceLoader.LoadThreadedRequest(Data.ImageSource);
            Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(Data.ImageSource));
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

			
			Size = SizeFunc(Position.X) * selectedSize;
			return;
			
			float SizeFunc(float sizeX)
            {
            	if (sizeX > lb && sizeX <= eps) return (float)Easings.EaseInOutCubic((sizeX - lb) / -lb) * 0.36f + 0.64f;
            	if (sizeX < rb && sizeX >= -eps) return (1.0f - (float)Easings.EaseInOutCubic(sizeX / rb)) * 0.36f + 0.64f;
            	return 0.64f;
            }

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
				// path = "/storage/emulated/0/";草你妈怎么拿权限啊妈个逼的
				path = "/storage/emulated/0/";
				break;
			case "iOS":
				GD.Print("iOS");
				break;
			case "Web":
				GD.Print("Web");
				break;
		}

		//path = OS.GetSystemDir(OS.SystemDir.Downloads);
		OS.RequestPermissions();
		OS.RequestPermission("android.permission.READ_EXTERNAL_STORAGE");
		OS.RequestPermission("android.permission.WRITE_EXTERNAL_STORAGE");
		FileDialog fd = new()
		{
			Size = new Vector2I(900, 600),
			CurrentDir = path,
			Access = FileDialog.AccessEnum.Filesystem
		};
		//_diffLabel.Text = $"using path:{path}";
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

	public void LoadSelectedChart()
	{
		var scene = GameScene.Instantiate<ChartManager>();
		scene.IsAutoPlay = _autoPlayButton.ButtonPressed;
		scene.OnExit += GameExited;
		GameContainer.AddChild(scene);
		scene.LoadFromDir(_displayList[CurIndex].Data.Root);
		_bgTexture.Visible = false;
		_uiControl.Visible = false;
		GameContainer.Visible = true;
	}
	public void DelSelectedChart()
	{
		var path = _displayList[CurIndex].Data.Root;
		_displayList[CurIndex].QueueFree();
		_displayList.RemoveAt(CurIndex);
		CurIndex--; // refresh layout
		path.ListDirBegin();
		var fileName = path.GetNext();
		while (fileName != "")
		{
			var err = path.Remove(fileName);
			if (err != Error.Ok)
			{
				GD.Print($"Remove file error:\n  {err.ToString()}");
			}
			fileName = path.GetNext();
		}

		var dir = DirAccess.Open("user://Charts"); // can't find a better way to get parent directory
		var error = dir.Remove(path.GetCurrentDir().GetFile());
		if (error != Error.Ok)
		{
			GD.Print($"Remove dir error:\n  {error.ToString()}");
		}
		// path.Remove()
	}

	

	public void ChartSelected(string dir)
	{
		var originDir = DirAccess.Open(dir);
		var dirName = originDir.GetCurrentDir().GetFile(); // Yes directory is considered as a file here.
		DirAccess.Open("user://Charts").MakeDir(dirName);
		var destDir = DirAccess.Open($"user://Charts/{dirName}");
		originDir.ListDirBegin();
		var fileName = originDir.GetNext();
		while (!String.IsNullOrEmpty(fileName))
		{
			if (originDir.CurrentIsDir())
			{
				fileName = originDir.GetNext();
				continue;
			}
			GD.Print($"Copying file {originDir.GetCurrentDir()}/{fileName} to {destDir.GetCurrentDir()}/{fileName}");
			var err = originDir.Copy($"{originDir.GetCurrentDir()}/{fileName}", $"{destDir.GetCurrentDir()}/{fileName}");
			if (err != Error.Ok)
			{
				GD.Print($"import copy failed, error:\n    {err}");
			}
			fileName = originDir.GetNext();
		}
		originDir.ListDirEnd();
		AddChartDisplay(DirAccess.Open($"{destDir.GetCurrentDir()}/{fileName}"));
	}

	public void GameExited(JudgeManager judgeData)
	{
		GameContainer.Visible = false;
		_bgTexture.Visible = true;
		_uiControl.Visible = true;
	}
	
	

	[Export] public LineEdit AddressEdit;
	public async void OnConnectRemote()
	{
		var scene = GameScene.Instantiate<ChartManager>();
		scene.IsAutoPlay = _autoPlayButton.ButtonPressed;
		scene.OnExit += GameExited;
		scene.Visible = false;
		GameContainer.AddChild(scene);
		await scene.ConnectChartServer(AddressEdit.Text);
		scene.Visible = true;
	}

}
