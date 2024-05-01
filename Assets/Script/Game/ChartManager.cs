using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Phidot.ChartStructure;
using Color = Godot.Color;

namespace Phidot.Game
{
    public partial class ChartManager : Control
    {
        public bool IsAutoPlay = false;

        private ResPackManager _resPackManager;

        private ChartRpe _chart;

        private ChartData _chartData;

        public double AspectRatio = 1.666667d;


        #region UIReferences

        [Export] public Label ScoreLabel;
        public int Score;
        [Export] public Label SongNameLabel;
        [Export] public Label DiffLabel;
        [Export] public Label GameModeLabel;
        [Export] public Label ComboLabel;
        public int Combo;
        [Export] public Sprite2D BackGroundImage;

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


        public List<JudgeLineNode> JudgeLineInstances = new List<JudgeLineNode>();


        public double PlaybackTime;
        public double Time;

        public bool IsPlaying;

        #region LoadChart

        // Called when the node enters the scene tree for the first time.
        public void LoadFromDir(string dir)
        {
            Vector2 newSize = DisplayServer.WindowGetSize();
            StageSize = new Vector2I((int)(newSize.Y * AspectRatio), (int)newSize.Y);


            GameModeLabel.Text = IsAutoPlay ? "AUTOPLAY" : "COMBO";
            var absPath = ProjectSettings.GlobalizePath(dir);

            LoadChart(absPath);

            _resPackManager = GetNode<ResPackManager>("/root/ResPackManager");
            var curPack = _resPackManager.CurPack;
            var heInstance = HeScene.Instantiate<AnimatedSprite2D>();
            heInstance.SpriteFrames = curPack.HitEffectFrames;

            var scene = new PackedScene();
            scene.Pack(heInstance);
            HeScene = scene;

            CalcUiSize();
            PlayChart();
        }

        public void LoadChart(string rootDir)
        {
            string infoPath = Path.Combine(rootDir, "info.txt");
            string infoContent = File.ReadAllText(infoPath);
            _chartData = ChartData.FromString(rootDir, infoContent);
            // TODO: Read from JSON META
            BackGroundImage.Texture = (Texture2D)GD.Load<Texture>(_chartData.ImageSource);
            Music.Stream = (AudioStream)GD.Load(Path.Combine(rootDir, _chartData.MusicFileName));
            SongNameLabel.Text = _chartData.ChartName;
            DiffLabel.Text = _chartData.ChartDiff;

            string jsonText = File.ReadAllText(Path.Combine(rootDir, _chartData.ChartFileName));
            _chart = JsonConvert.DeserializeObject<ChartRpe>(jsonText);
            GD.Print(_chart.JudgeLineList.Count);
            // Must be done as initialization
            _chart.PreCalculation();
            foreach (var line in _chart.JudgeLineList)
            {
                int i = _chart.JudgeLineList.IndexOf(line);
                var lineInstance = JudgeLineScene.Instantiate() as JudgeLineNode;
                AddChild(lineInstance);
                lineInstance.FingerDataList = FingerDatas;
                lineInstance.Init(_chart, i);
                lineInstance.ZIndex = _chart.JudgeLineList[i].ZOrder;
                lineInstance.AspectRatio = AspectRatio;
                JudgeLineInstances.Add(lineInstance);

                foreach (NoteNode note in lineInstance.NoteInstances)
                {
                    note.OnJudged += JudgedEventHandler;
                }
            }
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

            foreach (var line in JudgeLineInstances)
            {
                foreach (var noteNode in line.NoteInstances)
                {
                    if (noteNode.State != JudgeState.NotJudged && realTime <= noteNode.NoteInfo.StartTime.RealTime)
                    {
                        if (realTime <= Time) _chart.JudgeData.RevertJudge();
                        noteNode.State = JudgeState.NotJudged;
                        noteNode.Visible = true;
                        noteNode.Head.Visible = true;
                        if (noteNode.NoteInfo.Type == NoteType.Hold)
                        {
                            noteNode.Body.Visible = true;
                            noteNode.Tail.Visible = true;
                            noteNode.Modulate = new Color(1, 1, 1);
                        }
                    }
                }
            }

            Time = realTime;
            Music.Seek((float)realTime);
        }

        public void Restart()
        {
            PauseUi.Visible = !PauseUi.Visible;
            _chart.JudgeData.RevertJudge();
            ComboLabel.Text = "0";
            Combo = 0;
            ScoreLabel.Text = "0000000";
            Score = 0;
            ProgressBar.Value = 0;
            SeekTo(0);
            PlayChart();
        }

        public void Exit()
        {
            QueueFree();
        }

        #endregion

        #region StartChart

        public void PlayChart()
        {
            LabelUi.Size = StageSize + Vector2.One * 400;
            LabelUi.Position = Vector2.One * -200;

            GameModeLabel.Visible = false;
            ComboLabel.Visible = false;
            var tween = LabelUi.CreateTween().SetParallel();
            foreach (JudgeLineNode line in JudgeLineInstances)
            {
                var scale = line.CalcScale();
                line.TextureLine.Scale = new Vector2(0, scale.Y);
                tween.TweenProperty(line.TextureLine, "scale", scale, 1.5d).SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Cubic);
                line.CalcTime(0.0f);
            }

            tween.TweenProperty(LabelUi, "size", StageSize, 1.0d).SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quint);
            tween.TweenProperty(LabelUi, "position", Vector2.Zero, 1.0d).SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quint);
            tween.TweenCallback(
                Callable.From
                (() =>
                {
                    IsPlaying = true;
                    Music.Play();
                })).SetDelay(1.0f);
            tween.Play();
        }

        #endregion


        #region Tick

        public override void _Process(double delta)
        {
            CalcUiSize();

            PlaybackTime = Music.GetPlaybackPosition();
            ProgressBar.Value = PlaybackTime / Music.Stream.GetLength();

            if (IsPlaying)
            {
                Time += delta;

                foreach (JudgeLineNode line in JudgeLineInstances)
                {
                    line.ChartJudgeState = _chart.JudgeData.ChartJudgeType;
                    line.CalcTime(Time);
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
            if (IsAutoPlay)
            {
                if (Input.IsActionJustPressed("seek_forward"))
                {
                    SeekTo(Time + 5);
                }

                if (Input.IsActionJustPressed("seek_back"))
                {
                    SeekTo(Time - 5);
                }
            }
        }

        #endregion

        #region UISizing

        public void CalcUiSize()
        {
            Vector2 newSize = DisplayServer.WindowGetSize();
            StageSize = new Vector2I((int)(newSize.Y * AspectRatio), (int)newSize.Y);
            if (!newSize.IsEqualApprox(WindowSize))
            {
                foreach (var line in JudgeLineInstances)
                {
                    line.WindowSize = newSize;
                    line.StageSize = StageSize;
                    line.CalcScale();
                }
            }

            WindowSize = DisplayServer.WindowGetSize();

            BackGroundImage.Scale = StageSize / BackGroundImage.Texture.GetSize();

            Size = StageSize;
            Position = (WindowSize - StageSize) / 2;


            if (IsPlaying)
            {
                LabelUi.Size = StageSize;
            }

            var ratio = StageSize.Y / 648;
            ScoreLabel.LabelSettings.FontSize = (int)(30 * ratio);
            ComboLabel.LabelSettings.FontSize = (int)(40 * ratio);
            GameModeLabel.LabelSettings.FontSize = (int)(20 * ratio);
            PauseBtn.CustomMinimumSize = Vector2.One * 25 * ratio;
            LabelUi.AddThemeConstantOverride("margin_top", (int)(25 * ratio));
            LabelUi.AddThemeConstantOverride("margin_right", (int)(25 * ratio));
            LabelUi.AddThemeConstantOverride("margin_left", (int)(25 * ratio));
            LabelUi.AddThemeConstantOverride("margin_bottom", (int)(25 * ratio));
            ComboUi.AddThemeConstantOverride("margin_top", (int)(15 * ratio));
        }

        #endregion

        #region Judge

        public const double BadRange = 0.22;
        public const double GoodRange = 0.16;
        public const double PerfectRange = 0.08;
        private float _noteJudgeSize = 100;

        public List<FingerData> FingerDatas = new();

        public class FingerData
        {
            public Vector2 CurPos;
            public Vector2 CurVec;

            public FingerData(Vector2 pos = new Vector2(), Vector2 vec = new Vector2())
            {
                CurPos = pos;
                CurVec = vec;
            }
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            switch (@event)
            {
                case InputEventScreenTouch { Pressed: false } touch when FingerDatas.Count > touch.Index:
                    FingerDatas.RemoveAt(touch.Index);
                    return;
                case InputEventScreenTouch touch:
                {
                    FingerDatas.Add(new FingerData(touch.Position));


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
                    selected.Note.EmitOnJudged(selected.Note.NoteJudgeType, selected.Note.NoteInfo.Type);
                    if (selected.Note.NoteInfo.Type == NoteType.Hold)
                    {
                        selected.Note.State = JudgeState.Holding;
                        return;
                    }

                    selected.Note.State = JudgeState.Judged;
                    selected.Note.Visible = false;
                    break;
                }
                case InputEventScreenDrag dragEvent:
                    FingerDatas[dragEvent.Index].CurPos += dragEvent.Relative;
                    FingerDatas[dragEvent.Index].CurVec = dragEvent.Velocity;
                    break;
            }
        }

        #endregion

        #region HitEffect

        public static Color PerfectColor = new(0xecebb0e7);
        public static Color GoodColor = new(0xb4e1ffeb);

        public void JudgedEventHandler(Vector2 globalPosition, JudgeType judgeType, NoteType noteType,
            bool shouldSoundAndRecord = true)
        {
            if (shouldSoundAndRecord)
            {
                _chart.JudgeData.Judge(judgeType);
            }

            Combo = _chart.JudgeData.MaxCombo;
            Score = _chart.JudgeData.CalcScore();

            if (judgeType == JudgeType.Miss) return;

            AnimatedSprite2D effectInstance = HeScene.Instantiate<AnimatedSprite2D>();
            Color color = PerfectColor;
            switch (judgeType)
            {
                case JudgeType.Perfect:
                    color = PerfectColor;
                    break;
                case JudgeType.Good:
                    color = GoodColor;
                    break;
            }

            if (shouldSoundAndRecord)
            {
                switch (noteType)
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
                }
            }

            effectInstance.Modulate = color;


            var curPack = _resPackManager.CurPack;
            var scale = 1.5f * curPack.HitFxScale * (StageSize.X * 175 /
                                                     (1350 * effectInstance.SpriteFrames.GetFrameTexture("default", 0)
                                                         .GetSize().X));
            effectInstance.Scale = Vector2.One * scale;

            effectInstance.Position = globalPosition - (WindowSize - StageSize) / 2;
            AddChild(effectInstance);
        }

        #endregion
    }
}