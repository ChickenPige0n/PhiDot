using Godot;
using System;

namespace Phidot.Game
{
	public partial class InfoLine : Control
	{

		[Export] public Label NameLabel;
		[Export] public Label DiffLabel;
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			var tween = CreateTween().SetParallel();
			Modulate = new Color(1, 1, 1, 0);
			tween
			.TweenProperty(this, "position", new Vector2(0, 0), 2.0d)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quint);
			tween
			.TweenProperty(this, "modulate", new Color(1, 1, 1), 2.0d)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quint);

			tween.Play();
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}
	}

}
