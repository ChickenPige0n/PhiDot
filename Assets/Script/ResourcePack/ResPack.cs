namespace PhiGodot.Assets.Script.ResourcePack;

using System.IO;
using Godot;

public partial class ResPack : Resource
{
    [Export] public Texture2D TapTexture;
    [Export] public Texture2D FlickTexture;
    [Export] public Texture2D DragTexture;
    
    [Export] public Vector2I HoldAtlas = new(50, 50);
    
    private Texture2D _holdTexture;
    [Export] public Texture2D HoldTexture
    {
        get => _holdTexture;
        set
        {
            _holdTexture = value;
            GD.Print($"HoldTexture set, atlas: {HoldAtlas.X}, {HoldAtlas.Y}");
            HoldTextures = GetHoldTextures(new AtlasTexture {Atlas = value}, (tail: HoldAtlas.X, head: HoldAtlas.Y));
            HoldHeadTexture = HoldTextures.head;
            GD.Print($"Head texture set: {(HoldHeadTexture as AtlasTexture).Region}");
        }
    }
    public (Texture2D head, Texture2D body, Texture2D tail) HoldTextures;
    
    [Export] public Texture2D HoldHeadTexture;


    [Export] public Texture2D TapHlTexture;
    [Export] public Texture2D FlickHlTexture;
    [Export] public Texture2D DragHlTexture;
    
    [Export] public Vector2I HoldHlAtlas = new(50, 95);
    
    private Texture2D _holdHlTexture;
    [Export]
    public Texture2D HoldHlTexture
    {
        get => _holdHlTexture;
        set
        {
            _holdHlTexture = value;
            HoldHlTextures = GetHoldTextures(new AtlasTexture {Atlas = value}, (tail: HoldHlAtlas.X, head: HoldHlAtlas.Y));
        }
    }

public (Texture2D head, Texture2D body, Texture2D tail) HoldHlTextures;
    

    [Export] public Vector2I HitEffectSplit = new(5, 6);
    
    private Texture2D _hitEffectTexture;
    [Export]
    public Texture2D HitEffectTexture
    {
        get => _hitEffectTexture;
        set
        {
            _hitEffectTexture = value;
            HitEffectFrames = GetHeFrames(new AtlasTexture {Atlas = value}, (line: HitEffectSplit.X, column: HitEffectSplit.Y));
        }
    }
    public SpriteFrames HitEffectFrames;


    [Export] public AudioStream TapSound;
    [Export] public AudioStream FlickSound;
    [Export] public AudioStream DragSound;

    [Export] public float HitFxScale = 1.0f;


    private static SpriteFrames GetHeFrames(AtlasTexture heTexture, (int line, int column) heInfo)
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
                var frame = (AtlasTexture)heTexture.Duplicate();
                frame.Region = new Rect2(j * fWidth, i * fHeight, fWidth, fHeight);
                frames.AddFrame("default", frame);
            }
        }

        frames.SetAnimationSpeed("default", heInfo.line * heInfo.column * 2);
        return frames;
    }

    private static (Texture2D head, Texture2D body, Texture2D tail) GetHoldTextures(AtlasTexture holdTexture,
        (int tail, int head) info)
    {
        var size = holdTexture.Atlas.GetSize();
        var head = (AtlasTexture)holdTexture.Duplicate();
        var body = (AtlasTexture)holdTexture.Duplicate();
        var tail = (AtlasTexture)holdTexture.Duplicate();

        head.Region = new Rect2(0, size.Y - info.head, size.X, info.head);
        body.Region = new Rect2(0, info.tail, size.X, size.Y - info.head - info.tail);
        tail.Region = new Rect2(0, 0, size.X, info.tail);

        return (head, body, tail);
    }

    public ResPack()
    {
    }

    public ResPack(ResPackInfo info)
    {
        try
        {
            TapTexture = GD.Load<Texture2D>($"{info.DirPath}/click.png");
            FlickTexture = GD.Load<Texture2D>($"{info.DirPath}/flick.png");
            DragTexture = GD.Load<Texture2D>($"{info.DirPath}/drag.png");
            
            HoldAtlas = new Vector2I(info.HoldAtlas[0], info.HoldAtlas[1]);
            var holdTexture2D = GD.Load<Texture2D>($"{info.DirPath}/hold.png");
            HoldTexture = holdTexture2D;
            
            HoldHlAtlas = new Vector2I(info.HoldAtlasMh[0], info.HoldAtlasMh[1]);
            var holdHlTexture2D = GD.Load<Texture2D>($"{info.DirPath}/hold_mh.png");
            HoldHlTexture = holdHlTexture2D;
            
            TapHlTexture = GD.Load<Texture2D>($"{info.DirPath}/click_mh.png");
            FlickHlTexture = GD.Load<Texture2D>($"{info.DirPath}/flick_mh.png");
            DragHlTexture = GD.Load<Texture2D>($"{info.DirPath}/drag_mh.png");

            var texture = new AtlasTexture
            {
                Atlas = GD.Load<Texture2D>($"{info.DirPath}/hit_fx.png")
            };
            HitEffectFrames = GetHeFrames(texture, (line: info.HitFx[1], column: info.HitFx[0]));

            TapSound = GD.Load<AudioStream>($"{info.DirPath}/click.ogg");
            FlickSound = GD.Load<AudioStream>($"{info.DirPath}/flick.ogg");
            DragSound = GD.Load<AudioStream>($"{info.DirPath}/drag.ogg");

            HitFxScale = info.HitFxScale;
        }
        catch (IOException e)
        {
            GD.Print(e.StackTrace);
        }
    }
}