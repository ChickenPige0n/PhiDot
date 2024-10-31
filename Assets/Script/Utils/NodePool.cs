using System.Collections.Generic;
using Godot;

public class NodePool
{
    private readonly Queue<HitEffect> _pool = new();

    private static void SetActive(CanvasItem node, bool active)
    {
        node.Visible = active;
        node.Modulate = Colors.Transparent;
        node.ProcessMode = active ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
    }

    public HitEffect GetNode(PackedScene scene)
    {
        var node = _pool.Count <= 0 ? scene.Instantiate<HitEffect>() : _pool.Dequeue();
        SetActive(node, true);
        return node;
    }

    public void PutNode(HitEffect node)
    {
        SetActive(node, false);
        _pool.Enqueue(node);
    }

    public void Clear()
    {
        foreach (var node in _pool)
        {
            node.QueueFree();
        }
    }
}