using Godot;
using System;

public partial class HitEffect : AnimatedSprite2D
{
	public NodePool Pool;

	public void Destroy()
	{
		Stop();
		Pool.PutNode(this);
	}
}
