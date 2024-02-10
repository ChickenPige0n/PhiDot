using Godot;
using Phidot.Game;
using System.Threading;

public partial class Pause : TextureButton
{
    private bool Paused = false;  // 计时器缓存

    [Export]
    public ChartManager chartManager;

    public void Pause_Game()  // 暂停游戏函数
    {
        
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Pressed += Pause_Game;  // 连接事件信号
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
