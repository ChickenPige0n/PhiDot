using System;
using System.Collections.Generic;
using System.IO;
using PhiGodot.Assets.Script.ResourcePack;
using Godot;
using YamlDotNet.Serialization;
using FileAccess = Godot.FileAccess;

public partial class ResPackManager : Node
{
    [Export] public ResPack CurPack { get; private set; }
    public List<ResPackInfo> ResPackList { get; } = new();

    public override void _Ready()
    {
        // GD.Print("Loading ResPacks from res://Assets/ResPacks");
        // var absPath = DirAccess.Open("res://Assets/ResPacks");
        // foreach (var dir in absPath.GetDirectories())
        // {
        //     GD.Print($"Loading respack from {dir}");
        //     LoadFromDir($"{absPath.GetCurrentDir()}/{dir}");
        // }
        // 
        CurPack = GD.Load<ResPack>("res://Assets/Script/ResourcePack/Official.tres");
        CurPack.HitEffectTexture = CurPack.HitEffectTexture;
    }

    public void LoadFromDir(string dir)
    {
        var infoPath = $"{dir}/info.yml";

        if (!FileAccess.FileExists(infoPath)) return;
        var deserializer = new DeserializerBuilder().Build();
        try
        {
            var info = deserializer.Deserialize<ResPackInfo>(FileAccess.Open(infoPath, FileAccess.ModeFlags.Read).GetAsText());
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

