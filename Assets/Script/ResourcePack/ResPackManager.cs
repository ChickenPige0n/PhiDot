using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public partial class ResPackManager : Node
{
    public ResPack CurPack { get; set; }
    public List<ResPackInfo> ResPackList { get; set; } = new();

    public override void _Ready()
    {
        var absPath = ProjectSettings.GlobalizePath("res://Assets/ResPacks");
        foreach (var dir in Directory.EnumerateDirectories(absPath))
        {
            LoadFromDir(dir);
        }
        CurPack = new ResPack(ResPackList[0]);
    }

    public void LoadFromDir(string dir)
    {
        var infoPath = Path.Combine(dir, "info.yml");

        if (!File.Exists(infoPath)) return;
        var deserializer = new DeserializerBuilder().Build();
        try
        {
            var info = deserializer.Deserialize<ResPackInfo>(File.ReadAllText(infoPath));
            info.DirPath = dir;
            ResPackList.Add(info);
        }
        catch (Exception e)
        {
            GD.Print(e.StackTrace);
        }
    }

    public void SetCurPack(int index)
    {
        CurPack = new ResPack(ResPackList[index]);
    }
}







public class ResPackInfo
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; }
    [YamlMember(Alias = "author")]
    public string Author { get; set; }
    [YamlMember(Alias = "description")]
    public string Description { get; set; }
    [YamlMember(Alias = "hitFx")]
    public List<int> HitFx { get; set; }
    [YamlMember(Alias = "holdAtlas")]
    public List<int> HoldAtlas { get; set; }
    [YamlMember(Alias = "holdAtlasMH")]
    public List<int> HoldAtlasMh { get; set; }
    [YamlMember(Alias = "hitFxScale")]
    public float HitFxScale { get; set; } = 1.0f;

    [YamlIgnore]
    public string DirPath { get; set; }

}


public class ResPack
{
    public Texture2D TapTexture;
    public Texture2D FlickTexture;
    public Texture2D DragTexture;
    public (Texture2D head, Texture2D body, Texture2D tail) HoldTextures;



    public Texture2D TapHlTexture;
    public Texture2D FlickHlTexture;
    public Texture2D DragHlTexture;
    public (Texture2D head, Texture2D body, Texture2D tail) HoldHlTextures;


    public SpriteFrames HitEffectFrames;


    public AudioStream TapSound;
    public AudioStream FlickSound;
    public AudioStream DragSound;

    public float HitFxScale = 1.0f;



    public static SpriteFrames GetHeFrames(AtlasTexture heTexture, (int line, int column) heInfo)
    {
        var frames = new SpriteFrames();
        var size = heTexture.Atlas.GetSize();

        var fWidth = (int)(size.X / heInfo.column);
        var fHeight = (int)(size.Y / heInfo.line);

        frames.ClearAll();
        for (var i = 0; i < heInfo.line; i++)
        {
            for (var j = 0; j < heInfo.column; j++)
            {
                var frame = (AtlasTexture)heTexture.Duplicate(true);
                frame.Region = new Rect2(j * fWidth, i * fHeight, fWidth, fHeight);
                frames.AddFrame("default", frame);
            }
        }
        frames.SetAnimationSpeed("default", heInfo.line * heInfo.column * 2);
        return frames;
    }

    public static (Texture2D head, Texture2D body, Texture2D tail) GetHoldTexture(AtlasTexture holdTexture, (int head, int tail) info)
    {

        var size = holdTexture.Atlas.GetSize();
        var head = (AtlasTexture)holdTexture.Duplicate(true);
        var body = (AtlasTexture)holdTexture.Duplicate(true);
        var tail = (AtlasTexture)holdTexture.Duplicate(true);

        head.Region = new Rect2(0, size.Y - info.head, size.X, info.head);
        body.Region = new Rect2(0, info.tail, size.X, size.Y - info.head - info.tail);
        tail.Region = new Rect2(0, 0, size.X, info.tail);

        return (head, body, tail);
    }

    public ResPack(ResPackInfo info)
    {
        try
        {
            TapTexture = GD.Load<Texture2D>(Path.Combine(info.DirPath, "click.png"));
            FlickTexture = GD.Load<Texture2D>(Path.Combine(info.DirPath, "flick.png"));
            DragTexture = GD.Load<Texture2D>(Path.Combine(info.DirPath, "drag.png"));
            var holdTexture2D = GD.Load<Texture2D>(Path.Combine(info.DirPath, "hold.png"));

            var texture = new AtlasTexture
            {
                Atlas = holdTexture2D
            };
            HoldTextures = GetHoldTexture(texture, (head: info.HoldAtlas[1], tail: info.HoldAtlas[0]));

            TapHlTexture = GD.Load<Texture2D>(Path.Combine(info.DirPath, "click_mh.png"));
            FlickHlTexture = GD.Load<Texture2D>(Path.Combine(info.DirPath, "flick_mh.png"));
            DragHlTexture = GD.Load<Texture2D>(Path.Combine(info.DirPath, "drag_mh.png"));

            texture = new AtlasTexture
            {
                Atlas = GD.Load<Texture2D>(Path.Combine(info.DirPath, "hold_mh.png"))
            };

            HoldHlTextures = GetHoldTexture(texture, (head: info.HoldAtlasMh[1], tail: info.HoldAtlasMh[0]));

            texture = new AtlasTexture
            {
                Atlas = GD.Load<Texture2D>(Path.Combine(info.DirPath, "hit_fx.png"))
            };
            HitEffectFrames = GetHeFrames(texture, (line: info.HitFx[1], column: info.HitFx[0]));

            TapSound = GD.Load<AudioStream>(Path.Combine(info.DirPath, "click.ogg"));
            FlickSound = GD.Load<AudioStream>(Path.Combine(info.DirPath, "flick.ogg"));
            DragSound = GD.Load<AudioStream>(Path.Combine(info.DirPath, "drag.ogg"));

            HitFxScale = info.HitFxScale;

        }
        catch (IOException e)
        {
            GD.Print(e.StackTrace);
        }

    }



}