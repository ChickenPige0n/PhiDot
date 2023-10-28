using Godot;
using Phigodot.ChartStructure;
using System;
using System.Collections.Generic;
using Phigodot.Game;

public partial class JudgeLineNode : Sprite2D
{
	// TODO:
	// Properties Calculate by time -finished!
	// Note Generate                -half done!
	// Note YPos Calculate

	
	[Export] public Texture2D TapTexture;
	[Export] public Texture2D FlickTexture;
	[Export] public Texture2D DragTexture;	


	public List<RPENote> Notes
	{
		set
		{
			foreach(RPENote noteInfo in value)
			{
				var instance = NoteScene.Instantiate<NoteNode>();
				instance.NoteInfo = noteInfo;
				noteInstances.Add(instance);
				switch (noteInfo.type)
				{
					case 0:
						instance.Texture = TapTexture;
						break;
					case 1:
						instance.Texture = DragTexture;
						break;
					case 2:
						instance.Texture = FlickTexture;
						break;
					case 3:
						// Todo: Hold
						break;
					default:
						break;
				}
				instance.Position = new Vector2(noteInfo.positionX*StageSize.X/1350.0f,100);
				AddChild(instance);
			}
		}
	}

	[Export] public PackedScene NoteScene;
	public List<NoteNode> noteInstances = new List<NoteNode>();

	public List<EventLayer> EventLayers;


	public Vector2I StageSize;
	public double AspectRatio = 1.666667d;

	public double ChartTime
	{
		set
		{
			double xPos = 0;
			double yPos = 0;
			double alpha = 0;
			double rotate = 0;

			foreach(EventLayer layer in EventLayers)
			{
				xPos += layer.moveXEvents.GetValByTime(value);
				yPos += layer.moveYEvents.GetValByTime(value);
				alpha += layer.alphaEvents.GetValByTime(value);
				rotate += layer.rotateEvents.GetValByTime(value);
			}

			Position = ChartRPE.RPEPos2PixelPos(new Vector2((float)xPos,-(float)yPos),StageSize) + (StageSize/2);
			RotationDegrees = (float)rotate;
			var m = SelfModulate;
			SelfModulate = new Color(m.R,m.G,m.B,(float)alpha/255.0f);
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Vector2I windowSize = DisplayServer.WindowGetSize();
		StageSize = new Vector2I((int)((double)windowSize.Y*AspectRatio),(int)windowSize.Y);
	}
}
