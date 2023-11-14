using Godot;
using Phigodot.ChartStructure;

namespace Phigodot.Game
{
	public partial class NoteNode : Sprite2D
	{
		[Signal]
		public delegate void OnJudjedEventHandler(Vector2 globalPosition,int judgeType,int noteType);

		[Export] public Sprite2D HeadSprite;
		[Export] public Sprite2D BodySprite;
		[Export] public Sprite2D TailSprite;
		
		[Export] public Label floorPos;
		public RPENote NoteInfo;
		private bool _judged = false;
		public bool Judged
		{
			get
			{
				return _judged;
			}
			set
			{
				if(!_judged && value)
				{
					EmitSignal(SignalName.OnJudjed,GlobalPosition,(int)JudgeType.perfect,(int)NoteInfo.Type);
				}
				_judged = value;
				Visible = !value;
			}
		}


		public override void _Process(double delta)
		{
			//floorPos.Text = Position.Y.ToString();
		}
	}
}