using Godot;

public partial class SceneManager : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	[Export] public PackedScene GameScene;
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
		AddChild(scene);
		scene.LoadFromDir(path);
	}

}
