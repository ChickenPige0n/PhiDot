using Godot;
using Phigodot.ChartStructure;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Phigodot.Game
{
	
	public enum JudgeType
	{
		perfect,
		good,
		miss
	}
	public partial class NoteNode : Sprite2D
	{
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
				_judged = value;
				Visible = !value;
			}
		}

		public Color PerfectColor = new Color(0.839216f, 0.741176f, 0.360784f, 1);
		public Color GoodColor = new Color(0x31cce1);

		[Export] public PackedScene HitEffect;

		public void Judge(JudgeType type = JudgeType.perfect)
		{
			var effectInstance = HitEffect.Instantiate<AnimatedSprite2D>();
			switch (type) {
				case JudgeType.perfect:
					effectInstance.Modulate = PerfectColor;
					break;
				case JudgeType.good:
					effectInstance.Modulate = GoodColor;
					break;
				default:
					break;
			}
			AddChild(effectInstance);
			Judged = true;
		}

        public override void _Process(double delta)
        {
            
        }
    }
}