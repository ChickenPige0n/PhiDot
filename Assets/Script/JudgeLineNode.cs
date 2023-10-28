using Godot;
using Phigodot.ChartStructure;
using System;
using System.Collections.Generic;

public partial class JudgeLineNode : Sprite2D
{
	// TODO:
	// Properties Calculate by time
	// Note Generate
	// Note YPos Calculate
	public List<EventLayer> EventLayers;

	public Vector2I StageSize;
	public int count;
	
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

			Position = RPEChart.RPEPos2PixelPos(new Vector2((float)xPos,-(float)yPos),StageSize) + (StageSize/2);
			RotationDegrees = (float)rotate;
			var m = SelfModulate;
			SelfModulate = new Color(m.R,m.G,m.B,(float)alpha/255.0f);
			
			//count ++;
			//if (count >= 100){
			//	count = 0;
			//	GD.Print(Position);
			//}
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
