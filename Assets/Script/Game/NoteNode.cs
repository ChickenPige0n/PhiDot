using Godot;
using Phidot.ChartStructure;

namespace Phidot.Game
{
	public partial class NoteNode : Node2D
	{
		[Signal]
		public delegate void OnJudjedEventHandler(Vector2 globalPosition, int judgeType, int noteType, bool shouldSound = true);

		public Sprite2D Head;
		public Sprite2D Body;
		public Sprite2D Tail;
		
		
        public double HoldTimer = 0;
        public JudgeState State;


		private RPENote _noteInfo;
		public RPENote NoteInfo
		{
			get
			{
				return _noteInfo;
			}
			set
			{
				_noteInfo = value;
				switch ((NoteType)value.Type)
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
		private bool _touched = false;
		public bool Touched
		{
			get
			{
				return _touched;
			}
			set
			{
				if (!_touched && value)
				{
					//EmitSignal(SignalName.OnJudjed, GlobalPosition, (int)JudgeType.Perfect, (int)NoteInfo.Type, true);
				}
				_touched = value;
			}
		}


		public override void _Process(double delta)
		{
		}
	}
}