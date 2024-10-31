using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Godot;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Color = Godot.Color;
using WebSocketSharp;
using WebSocketSharp.Server;

public partial class ChartManager : Control
{
    public bool IsAutoPlay = true;

    private ResPackManager _resPackManager;

    private ChartRpe _chart;

    private ChartData _chartData;

    public double AspectRatio = 1.777778d;


    #region UIReferences

    [Export] public Label FPSLabel;

    [Export] public Label ScoreLabel;
    public int Score;
    [Export] public Label SongNameLabel;
    [Export] public Label DiffLabel;
    [Export] public Label GameModeLabel;
    [Export] public Label ComboLabel;
    public int Combo;
    [Export] public TextureRect BackGroundControl;
    [Export] public TextureRect BackGroundImage;

    [Export] public ProgressBar ProgressBar;

    [Export] public Control PauseUi;

    [Export] public MarginContainer LabelUi;
    [Export] public MarginContainer ComboUi;

    [Export] public TextureButton PauseBtn;

    #endregion

    public Vector2 WindowSize;
    public Vector2 StageSize;


    #region SFX

    [Export] public AudioStreamPlayer Music;
    [Export] public AudioStreamPlayer TapSfx;
    [Export] public AudioStreamPlayer FlickSfx;
    [Export] public AudioStreamPlayer DragSfx;

    #endregion

    [Export] public PackedScene HeScene;
    [Export] public PackedScene JudgeLineScene;


    public List<JudgeLineNode> JudgeLineInstances = new();


    public double PlaybackTime;
    public double Time;

    public bool IsPlaying;

    #region LoadChart

    // Called when the node enters the scene tree for the first time.
    public void LoadFromDir(string absPath)
    {
        var infoPath = Path.Combine(absPath, "info.txt");
        var infoContent = File.ReadAllText(infoPath);
        var data = ChartData.FromString(absPath, infoContent);
        LoadFromChartData(data);
    }

    private void LoadFromChartData(ChartData data)
    {
        var absPath = data.Root;
        _chartData = data;
        // TODO: Read from JSON META
        BackGroundImage.Texture = (Texture2D)GD.Load<Texture>(_chartData.ImageSource);
        Music.Stream = (AudioStream)GD.Load(Path.Combine(absPath, _chartData.MusicFileName));
        SongNameLabel.Text = _chartData.ChartName;
        DiffLabel.Text = _chartData.ChartDiff;

        var jsonText = File.ReadAllText(Path.Combine(absPath, _chartData.ChartFileName));
        _chart = JsonConvert.DeserializeObject<ChartRpe>(jsonText);
        
        LoadChart();
    }

    private float _startTime = 0.0f;

    public void HandleRequestResult(long result,long responseCode,string[] headers,byte[] body)
    {
        GD.Print("started to process result");
        var fileData = body;
        GD.Print($"loaded filedata: {fileData}");
        // 使用MemoryStream来处理字节数据
        using var compressedStream = new MemoryStream(fileData);
        using var archive = new ZipArchive(compressedStream, ZipArchiveMode.Read);
        var tempPath = Path.Combine(Path.GetTempPath(), "ChartPreviewTemp");
        GD.Print($"path: {tempPath}");
        //if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
        Directory.CreateDirectory(tempPath);

        foreach (var entry in archive.Entries)
        {
            entry.ExtractToFile(Path.Combine(tempPath, entry.FullName), true);
        }
    }
    public async Task ConnectChartServer(string serverAddr){
        string filePath = @"C:\Downloads\file.txt";
        var client = new System.Net.Http.HttpClient();
        var fileData = await client.GetByteArrayAsync($"{serverAddr}/info.txt");
        {
            await File.WriteAllBytesAsync(filePath, fileData);
        }
        GD.Print("Started to download...");
        var theClient = new HttpRequest();
        theClient.RequestCompleted += HandleRequestResult;
        theClient.Request(serverAddr);
    }

    public void LoadChart()
    {
        Vector2 newSize = DisplayServer.WindowGetSize();
        StageSize = new Vector2I((int)(newSize.Y * AspectRatio), (int)newSize.Y);
        GameModeLabel.Text = IsAutoPlay ? "AUTOPLAY" : "COMBO";
        
        _resPackManager = GetNode<ResPackManager>("/root/ResPackManager");
        var curPack = _resPackManager.CurPack;
        var heInstance = HeScene.Instantiate<AnimatedSprite2D>();
        heInstance.SpriteFrames = curPack.HitEffectFrames;

        var scene = new PackedScene();
        scene.Pack(heInstance);
        HeScene = scene;

        GD.Print(_chart.JudgeLineList.Count);
        // Must be done as initialization
        _chart.PreCalculation();
        foreach (var line in _chart.JudgeLineList)
        {
            var i = _chart.JudgeLineList.IndexOf(line);
            var lineInstance = JudgeLineScene.Instantiate() as JudgeLineNode;
            AddChild(lineInstance);
            lineInstance!.FingerDataList = FingerDataList;
            lineInstance.Init(_chart, i, IsAutoPlay);
            lineInstance.ZIndex = _chart.JudgeLineList[i].ZOrder;
            lineInstance.AspectRatio = AspectRatio;
            JudgeLineInstances.Add(lineInstance);

            foreach (var note in lineInstance.NoteInstances)
            {
                note.OnJudged += JudgedEventHandler;
            }
        }
        
        CalcUiSize();
        PlayChart();
    }

    #endregion

    #region ChartControl

    public void Pause()
    {
        IsPlaying = !IsPlaying;
        PauseUi.Visible = !PauseUi.Visible;
        if (IsPlaying)
        {
            Music.Play();
            Music.Seek((float)Time);
        }
        else
        {
            Music.Stop();
        }
    }


    public void SeekTo(double realTime)
    {
        realTime = realTime < 0 ? 0 : realTime;

        foreach (var noteNode in JudgeLineInstances.SelectMany(line => line.NoteInstances.Where(noteNode => noteNode.State != JudgeState.NotJudged &&
                     realTime <= noteNode.NoteInfo.StartTime.RealTime)))
        {
            if (realTime <= Time) _chart.JudgeData.RevertJudge();
            noteNode.State = JudgeState.NotJudged;
            noteNode.Visible = true;
            noteNode.Head.Visible = true;
            if (noteNode.NoteInfo.Type != NoteType.Hold) continue;
            noteNode.Body.Visible = true;
            noteNode.Tail.Visible = true;
            noteNode.Modulate = new Color(1, 1, 1);
        }

        Time = realTime;
        Music.Seek((float)realTime);
    }

    public void Restart()
    {
        PauseUi.Visible = !PauseUi.Visible;
        _chart.JudgeData.RevertJudge();
        SeekTo(0);
        PlayChart();
    }
    
    public delegate void OnExitHandler(JudgeManager judgeData);
    public event OnExitHandler OnExit;
    public void Exit()
    {
        OnExit?.Invoke(_chart.JudgeData);
        QueueFree();
    }

    #endregion

    #region StartChart

    public void PlayChart()
    {
        CalcUiSize();
        ComboLabel.Text = "0";
        Combo = 0;
        ScoreLabel.Text = "0000000";
        Score = 0;
        ProgressBar.Value = 0;


        LabelUi.Size = StageSize + Vector2.One * 400;
        LabelUi.Position = Vector2.One * -200;

        GameModeLabel.Visible = false;
        ComboLabel.Visible = false;
        var tween = LabelUi.CreateTween().SetParallel();
        foreach (var line in JudgeLineInstances)
        {
            var scale = line.CalcScale();
            line.TextureLine.Scale = new Vector2(0, scale.Y);
            tween.TweenProperty(line.TextureLine, "scale", scale, 1.5d).SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Cubic);
            line.CalcTime(0.0f, Size);
        }

        tween.TweenProperty(LabelUi, "size", StageSize, 1.5d).SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quint);
        tween.TweenProperty(LabelUi, "position", Vector2.Zero, 1.5d).SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quint);
        tween.TweenCallback(
            Callable.From
            (() =>
            {
                IsPlaying = true;
                Music.Play();
            })).SetDelay(1.5f);
        tween.Play();
    }

    #endregion


    #region Tick

    public override void _Process(double delta)
    {
        
        FPSLabel.Text = $"FPS: {(1/delta):0.00}";
        CalcUiSize();

        PlaybackTime = Music.GetPlaybackPosition();
        ProgressBar.Value = PlaybackTime / Music.Stream.GetLength();

        if (IsPlaying)
        {
            Time += delta;

            foreach (var line in JudgeLineInstances)
            {
                line.ChartJudgeState = _chart.JudgeData.ChartJudgeType;
                line.CalcTime(Time, Size);
            }

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


        // Time Control
        //if (IsAutoPlay)
        //{
            if (Input.IsActionJustPressed("seek_forward"))
            {
                SeekTo(Time + 5);
            }
            if (Input.IsKeyPressed(Key.Space)){
                Pause();
            }

            if (Input.IsActionJustPressed("seek_back"))
            {
                SeekTo(Time - 5);
            }
        //}
    }

    #endregion

    #region UISizing

    public void CalcUiSize()
    {
        LayoutMode = 1;
        WindowSize = DisplayServer.WindowGetSize();
        var pxSize = new Vector2((int)(WindowSize.Y * AspectRatio), (int)WindowSize.Y);
        var actualSize = pxSize * 648 / WindowSize.Y;
        Size = actualSize;
        Position = new Vector2(((WindowSize - pxSize) / 2).X, 0);
        return;
        if (!WindowSize.IsEqualApprox(WindowSize))
        {
            foreach (var line in JudgeLineInstances)
            {
                line.WindowSize = WindowSize;
                line.StageSize = StageSize;
                line.CalcScale();
            }
        }

        WindowSize = DisplayServer.WindowGetSize();

        BackGroundControl.Scale = StageSize / BackGroundControl.Size;

        Size = StageSize;
        Position = (WindowSize - StageSize) / 2;


        if (IsPlaying)
        {
            LabelUi.Size = StageSize;
        }

        var ratio = StageSize.Y / 648;
        
        ScoreLabel.LabelSettings.FontSize = (int)(32 * ratio);
        
        ComboLabel.LabelSettings.FontSize = (int)(45 * ratio);
        GameModeLabel.LabelSettings.FontSize = (int)(16 * ratio);
        
        PauseBtn.CustomMinimumSize = Vector2.One * 24 * ratio;

        SongNameLabel.LabelSettings.FontSize = (int)(24 * ratio);
        DiffLabel.LabelSettings.FontSize = (int)(24 * ratio);
        
        LabelUi.AddThemeConstantOverride("margin_left", (int)(20 * ratio));
        LabelUi.AddThemeConstantOverride("margin_top", (int)(19 * ratio));
        LabelUi.AddThemeConstantOverride("margin_right", (int)(24 * ratio));
        LabelUi.AddThemeConstantOverride("margin_bottom", (int)(18 * ratio));
        ComboUi.AddThemeConstantOverride("margin_top", (int)(9 * ratio));
    }

    #endregion

    #region Judge

    public const double BadRange = 0.22;
    public const double GoodRange = 0.16;
    public const double PerfectRange = 0.08;
    private float _noteJudgeSize = 100;

    public List<FingerData> FingerDataList = new();

    public class FingerData
    {
        public Vector2 CurPos;
        public Vector2 CurVec;

        public FingerData(Vector2 pos = new (), Vector2 vec = new ())
        {
            CurPos = pos;
            CurVec = vec;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if(IsAutoPlay || IsPlaying) return;
        base._Input(@event);
        switch (@event)
        {
            case InputEventScreenTouch { Pressed: false } touch when FingerDataList.Count > touch.Index:
                FingerDataList.RemoveAt(touch.Index);
                return;
            case InputEventScreenTouch touch:
            {
                FingerDataList.Add(new FingerData(touch.Position));


                var selected = JudgeLineInstances
                    .Select(line => new
                    {
                        line,
                        LocalTouchPos = (touch.Position - line.GlobalPosition).Rotated(-line.Rotation)
                    })
                    .SelectMany(
                        data => data.line.NoteInstances,
                        (data, note) =>
                        {
                            var dt = Math.Abs(Time - note.NoteInfo.StartTime.RealTime);
                            var dx = Math.Abs(note.Position.X - data.LocalTouchPos.X);
                            return new { Note = note, dx, dt };
                        })
                    .Where(it =>
                        (int)it.Note.NoteInfo.Type <= 2 && it.Note.State != JudgeState.Judged &&
                        it.dt < BadRange &&
                        it.dx < _noteJudgeSize)
                    .MinBy(it => it.dx);

                if (selected is null) return;
                var judgement = selected.dt switch
                {
                    < PerfectRange => JudgeType.Perfect,
                    < GoodRange => JudgeType.Good,
                    _ => JudgeType.Bad
                };

                selected.Note.NoteJudgeType = judgement;
                selected.Note.EmitOnJudged();
                if (selected.Note.NoteInfo.Type == NoteType.Hold)
                {
                    return;
                }
                selected.Note.Visible = false;
                break;
            }
            case InputEventScreenDrag dragEvent:
                FingerDataList[dragEvent.Index].CurPos += dragEvent.Relative;
                FingerDataList[dragEvent.Index].CurVec = dragEvent.Velocity;
                break;
        }
    }

    #endregion

    #region HitEffect

    public static Color PerfectColor = new(0xecebb0e7);
    public static Color GoodColor = new(0xb4e1ffeb);


    public void JudgedEventHandler(Vector2 globalPosition, NoteNode instance, bool shouldSoundAndRecord = true)
    {
        if (shouldSoundAndRecord)
        {
            _chart.JudgeData.Judge(instance.NoteJudgeType);
        }

        Combo = _chart.JudgeData.MaxCombo;
        Score = _chart.JudgeData.CalcScore();

        if (instance.NoteJudgeType == JudgeType.Miss) return;

        var effect = HeScene.Instantiate<AnimatedSprite2D>();
        
        var color = instance.NoteJudgeType switch
        {
            JudgeType.Perfect => PerfectColor,
            JudgeType.Good => GoodColor,
            _ => PerfectColor
        };

        if (shouldSoundAndRecord)
        {
            switch (instance.NoteInfo.Type)
            {
                case NoteType.Tap:
                    TapSfx.Play();
                    break;
                case NoteType.Hold:
                    TapSfx.Play();
                    break;
                case NoteType.Flick:
                    FlickSfx.Play();
                    break;
                case NoteType.Drag:
                    DragSfx.Play();
                    break;
                default:
                    TapSfx.Play();
                    break;
            }
        }
        
        effect.Modulate = color;
        var curPack = _resPackManager.CurPack;
        var scale = 1.5f * curPack.HitFxScale * (Size.X * 175 /
                                                 (1350 * effect.SpriteFrames.GetFrameTexture("default", 0)
                                                     .GetSize().X));
        effect.Scale = Vector2.One * scale;

        effect.Position = globalPosition - Position;
        AddChild(effect);
    }

    #endregion
}
