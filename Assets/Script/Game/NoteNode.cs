using System;
using Godot;

public partial class NoteNode : Node2D
{
	[Export] public Label Label;
	
	public delegate void OnJudjedEventHandler(Vector2 globalPosition, NoteNode instance, bool shouldSound = true);
	public event OnJudjedEventHandler OnJudged;

	public void EmitOnJudged(bool shouldSound = true){
		if (NoteInfo.IsFake == 1) return;
		if (shouldSound)
		{
			State = NoteInfo.Type == NoteType.Hold ? JudgeState.Holding : JudgeState.Judged;
			Head.Visible = NoteInfo.Type == NoteType.Hold;
		}
		var x = Position.X;
		var parent = GetParent<JudgeLineNode>();
		var pos = new Vector2(
			x * (float)Math.Cos(parent.Rotation),
			x * (float)Math.Sin(parent.Rotation)
		) + parent.GlobalPosition;
		OnJudged?.Invoke(pos,this, shouldSound);
	}

	public Sprite2D Head;
	public Sprite2D Body;
	public Sprite2D Tail;
	
	


	private RpeNote _noteInfo;
	public RpeNote NoteInfo
	{
		get => _noteInfo;
		set
		{
			_noteInfo = value;
			switch (value.Type)
			{
				case NoteType.Hold:
					Head = new Sprite2D();
					Body = new Sprite2D();
					Tail = new Sprite2D();
					AddChild(Head);
					AddChild(Body);
					AddChild(Tail);
					break;
				default:
					Head = new Sprite2D();
					AddChild(Head);
					break;
			}

		}
	}

	
    public double HoldTimer = 0;
	public double FingerUpTimer = 0;
    public JudgeState State;
	public JudgeType NoteJudgeType = JudgeType.Perfect;



	public override void _Process(double delta)
	{
	}
}