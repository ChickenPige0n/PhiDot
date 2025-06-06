using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Color = Godot.Color;
using System.Security.Authentication;
using System.Threading;
using FileAccess = Godot.FileAccess;

public partial class ChartManager : Control
{
    public bool IsAutoPlay = false;

    private ResPackManager _resPackManager;

    private ChartRpe _chart;

    private ChartData _chartData;

    public double AspectRatio = 1.777778d;


    #region UIReferences

    [Export] public Label FpsLabel;
    [Export] public Label ScoreLabel;
    public int Score;
    [Export] public Label SongNameLabel;
    [Export] public Label DiffLabel;
    [Export] public Label GameModeLabel;
    [Export] public Label ComboLabel;
    public int Combo;
    [Export] public TextureRect BackGroundControl;
    [Export] public TextureRect BackGroundImage;
    [Export] public TextureRect BackGroundTemp;

    [Export] public ProgressBar ProgressBar;

    [Export] public HSlider ProgressControl;

    [Export] public Control PauseUi;

    [Export] public MarginContainer LabelUi;
    [Export] public MarginContainer ComboUi;

    [Export] public TextureButton PauseBtn;

    #endregion

    public Vector2 WindowSize;


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

    public void LoadFromDir(DirAccess dir)
    {
        GD.Print($"Loading chart from dir {dir.GetCurrentDir()}");
        var file = FileAccess.Open($"{dir.GetCurrentDir()}/info.txt", FileAccess.ModeFlags.Read);
        var e = FileAccess.GetOpenError();
        if (e != Error.Ok)
        {
            GD.Print($"Error occoured while opening info.txt, error:\n    {e}");
        }

        var infoContent = file.GetAsText();
        GD.Print($"Loaded info.txt:\n{infoContent}");
        var data = ChartData.FromString(dir, infoContent);
        LoadFromChartData(data);
    }

    private void LoadFromChartData(ChartData data)
    {
        var rootPath = data.Root;
        _chartData = data;
        // TODO: Read from JSON META

        var image = new Image();
        image.Load(_chartData.ImageSource);
        BackGroundTemp.Texture = ImageTexture.CreateFromImage(image);
        var thread = new Thread(async () =>
        {
            await ToSignal(GetTree().CreateTimer(.5f), "timeout"); // wait for the shader to be computed.
            BackGroundImage.Texture = ImageTexture.CreateFromImage(BackGroundImage.Texture.GetImage());
            BackGroundTemp.Visible = false;
        });
        thread.Start();

        static AudioStream LoadMusic(string path)
        {
            GD.Print($"Loading MP3 from {path}");
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            var data = file.GetBuffer((long)file.GetLength());
            return Path.GetExtension(path).ToLower() switch
            {
                ".mp3" => new AudioStreamMP3 { Data = data },
                ".wav" => new AudioStreamWav { Data = data },
                ".ogg" => AudioStreamOggVorbis.LoadFromFile(path),
                _ => new AudioStream()
            };
        }

        Music.Stream = LoadMusic($"{rootPath.GetCurrentDir()}/{_chartData.MusicFileName}");
        SongNameLabel.Text = _chartData.ChartName;
        DiffLabel.Text = _chartData.ChartDiff;

        GD.Print($"Loading chart json from {rootPath.GetCurrentDir()}/{_chartData.ChartFileName}");

        var jsonText = FileAccess
            .Open($"{rootPath.GetCurrentDir()}/{_chartData.ChartFileName}", FileAccess.ModeFlags.Read)
            .GetAsText();

        _chart = JsonConvert.DeserializeObject<ChartRpe>(jsonText);
        LoadChart();
    }

    private float _startTime;

    /// <summary>
    /// TODO: Use User:// instead of C# provided file IO
    /// </summary>
    /// <param name="serverAddr"></param>
    public async Task ConnectChartServer(string serverAddr)
    {
        HttpClientHandler clientHandler = new HttpClientHandler();
        clientHandler.ServerCertificateCustomValidationCallback +=
            (_, _, _, _) => true;
        clientHandler.SslProtocols = SslProtocols.None;
        var client = new System.Net.Http.HttpClient(clientHandler);
        var tempPath = Path.Combine(Path.GetTempPath(), "ChartPreviewTemp");
        try
        {
            Directory.Delete(tempPath, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting temp folder: {ex.Message}");
        }

        Directory.CreateDirectory(tempPath);

        await DownloadFile("info.txt");

        var infoPath = Path.Combine(tempPath, "info.txt");
        var infoContent = await File.ReadAllTextAsync(infoPath);
        var t = DirAccess.Open(tempPath);
        var data = ChartData.FromString(t, infoContent);

        await DownloadFile(data.ChartFileName);
        await DownloadFile(data.MusicFileName);
        await DownloadFile(data.ImageFileName);
        LoadFromChartData(data);
        return;

        async Task DownloadFile(string fileName)
        {
            try
            {
                GD.Print($"Downloading {fileName} from http://{serverAddr}/{fileName}");
                var fileData = await client.GetByteArrayAsync($"http://{serverAddr}/{fileName}");
                {
                    GD.Print($"Saving file to {tempPath}");
                    await File.WriteAllBytesAsync(Path.Combine(tempPath, fileName), fileData);
                }
                GD.Print("Complete!");
            }
            catch (Exception ex)
            {
                GD.Print($"Error downloading file: {ex.Message}\nQuitting...");
                QueueFree();
            }
        }
    }
    
    public void LoadChart()
    {
        CalcUiSize();
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
            lineInstance!.FingerDataDict = FingerDataDict;
            WindowSize = DisplayServer.WindowGetSize();
            lineInstance.WindowSize = WindowSize;
            lineInstance.StageSize = new Vector2((float)(Size.Y * AspectRatio), Size.Y);

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
        PauseUi.Visible = !IsPlaying;
        if (IsPlaying)
        {
            Music.Play();
            Music.Seek((float)Time);
        }
        else
        {
            Music.Stop();
            ProgressControl.Value = ProgressBar.Value;
        }
    }


    public void SeekTo(double realTime)
    {
        realTime = realTime < 0 ? 0 : realTime;

        foreach (var noteNode in JudgeLineInstances.SelectMany(line => line.NoteInstances.Where(noteNode =>
                     noteNode.State != JudgeState.NotJudged &&
                     realTime <= noteNode.NoteInfo.StartTime.RealTime)))
        {
            if (realTime <= Time) _chart.JudgeData.RevertJudge();
            noteNode.State = JudgeState.NotJudged;
            noteNode.NoteJudgeType = JudgeType.Perfect;
            noteNode.FingerUpTimer = 0;
            noteNode.Visible = true;
            noteNode.Head.Visible = true;
            if (noteNode.NoteInfo.Type != NoteType.Hold) continue;
            noteNode.Body.Visible = true;
            noteNode.Tail.Visible = true;
            noteNode.Modulate = new Color(1, 1, 1);
        }

        foreach (var line in JudgeLineInstances)
        {
            line.CalcTime(Time, Size);
        }

        Time = realTime;
        Music.Seek((float)realTime);
    }

    public void SeekToRatio(double r)
    {
        SeekTo(Music.Stream.GetLength() * r);
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
        QueueFree();
        OnExit?.Invoke(_chart.JudgeData);
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


        LabelUi.Size = Size + Vector2.One * 400;
        LabelUi.Position = Vector2.One * -200;

        GameModeLabel.Visible = false;
        ComboLabel.Visible = false;
        var tween = LabelUi.CreateTween().SetParallel();
        CalcUiSize();
        foreach (var line in JudgeLineInstances)
        {
            var scale = line.CalcScale();
            line.TextureLine.Scale = new Vector2(0, scale.Y);
            tween.TweenProperty(line.TextureLine, "scale", scale, 1.5d).SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Cubic);
            line.CalcTime(0.0f, Size);
        }

        tween.TweenProperty(LabelUi, "size", Size, 1.5d).SetEase(Tween.EaseType.Out)
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
        _startDate = DateTime.Now;
        if (OS.HasFeature("movie"))
        {
            Music.Finished += () =>
            {
                GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
                GetTree().Quit();
            };
        }
    }

    private DateTime _startDate;
    #endregion


    #region Tick

    public override void _Process(double delta)
    {
        
        FpsLabel.Text = $"FPS: {(1 / delta):0.00}";
        CalcUiSize();

        PlaybackTime = Music.GetPlaybackPosition();
        if (OS.HasFeature("movie"))
        {
            FpsLabel.Text = $"{PlaybackTime / (DateTime.Now.Second - _startDate.Second):F1} * 60";
        }
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

        if (Input.IsKeyPressed(Key.Space))
        {
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
        foreach (var line in JudgeLineInstances)
        {
            line.WindowSize = WindowSize;
            line.StageSize = Size;
            line.CalcScale();
        }
    }

    #endregion

    #region Judge

    public const double BadRange = 0.22;
    public const double GoodRange = 0.16;
    public const double PerfectRange = 0.08;
    private float _noteJudgeSize = 100;

    public Dictionary<int, FingerData?> FingerDataDict = [];

    public class FingerData(Vector2 pos = new(), Vector2 vec = new())
    {
        public Vector2 CurPos = pos;
        public Vector2 CurVec = vec;
    }

    public override void _Input(InputEvent @event)
    {
        if (IsAutoPlay || !IsPlaying) return;
        base._Input(@event);
        switch (@event)
        {
            case InputEventScreenTouch { Pressed: false } touch when FingerDataDict.Count > touch.Index:
                FingerDataDict[touch.Index] = null;
                return;
            case InputEventScreenTouch touch:
            {
                FingerDataDict[touch.Index] = new FingerData(touch.Position);


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
                FingerDataDict[dragEvent.Index].CurPos += dragEvent.Relative;
                FingerDataDict[dragEvent.Index].CurVec = dragEvent.Velocity;
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