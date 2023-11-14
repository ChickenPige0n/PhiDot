using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using YamlDotNet.Serialization;

namespace Phigodot.Game
{
    public partial class ResPackManager : Node
    {
        public ResPack CurPack { get; set; }
        public List<ResPackInfo> ResPackList { get; set; } = new List<ResPackInfo>();

        public override void _Ready()
        {
            var absPath = ProjectSettings.GlobalizePath("res://Assets/ResPacks");
            foreach (var dir in Directory.EnumerateDirectories(absPath))
            {
                var infoPath = Path.Combine(dir, "info.yml");
                if (File.Exists(infoPath))
                {
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
            }
            if (ResPackList.Count == 1)
            {
                CurPack = new ResPack(ResPackList[0]);
            }
        }
    }







    public class ResPackInfo
    {
        public string name { get; set; }
        public string author { get; set; }
        public List<int> hitFx { get; set; }
        public List<int> holdAtlas { get; set; }
        public List<int> holdAtlasMH { get; set; }

        [YamlIgnore]
        public string DirPath { get; set; }

    }


    public class ResPack
    {
        public Texture2D TapTexture;
        public Texture2D FlickTexture;
        public Texture2D DragTexture;
        public (Texture2D head, Texture2D body, Texture2D tail) HoldTextures;



        public Texture2D TapHLTexture;
        public Texture2D FlickHLTexture;
        public Texture2D DragHLTexture;
        public (Texture2D head, Texture2D body, Texture2D tail) HoldHLTextures;


        public SpriteFrames HitEffectFrames;


        public AudioStream TapSound;
        public AudioStream FlickSound;
        public AudioStream DragSound;





        public static SpriteFrames GetHEFrames(AtlasTexture HETexture, (int line, int column) HEInfo)
        {
            var frames = new SpriteFrames();
            var size = HETexture.Atlas.GetSize();

            var fWidth = (int)(size.X / HEInfo.column);
            var fHeight = (int)(size.Y / HEInfo.line);

            frames.ClearAll();
            for (int i = 1; i <= HEInfo.line; i++)
            {
                for (int j = 1; j <= HEInfo.column; j++)
                {
                    var frame = (AtlasTexture)HETexture.Duplicate(true);
                    frame.Region = new Rect2(i * fWidth, j * fHeight, fWidth, fHeight);
                    frames.AddFrame(new StringName("default"), frame);
                }
            }
            frames.SetAnimationSpeed(new StringName("default"), 60);
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
                var HoldTexture2D = GD.Load<Texture2D>(Path.Combine(info.DirPath, "hold.png"));

                var texture = new AtlasTexture
                {
                    Atlas = HoldTexture2D
                };
                HoldTextures = GetHoldTexture(texture, (head: info.holdAtlas[1], tail: info.holdAtlas[0]));

                TapHLTexture = GD.Load<Texture2D>(Path.Combine(info.DirPath, "click_mh.png"));
                FlickHLTexture = GD.Load<Texture2D>(Path.Combine(info.DirPath, "flick_mh.png"));
                DragHLTexture = GD.Load<Texture2D>(Path.Combine(info.DirPath, "drag_mh.png"));

                texture = new AtlasTexture
                {
                    Atlas = GD.Load<Texture2D>(Path.Combine(info.DirPath, "hold_mh.png"))
                };

                HoldHLTextures = GetHoldTexture(texture, (head: info.holdAtlasMH[1], tail: info.holdAtlasMH[0]));

                texture = new AtlasTexture
                {
                    Atlas = GD.Load<Texture2D>(Path.Combine(info.DirPath, "hit_fx.png"))
                };
                HitEffectFrames = GetHEFrames(texture, (line: info.hitFx[1], column: info.hitFx[0]));

                TapSound = GD.Load<AudioStream>(Path.Combine(info.DirPath, "click.ogg"));
                FlickSound = GD.Load<AudioStream>(Path.Combine(info.DirPath, "flick.ogg"));
                DragSound = GD.Load<AudioStream>(Path.Combine(info.DirPath, "click.ogg"));

            }
            catch (IOException e)
            {
                GD.Print(e.StackTrace);
            }

        }



    }


}