using Godot;
using Phigodot.ChartStructure;
using System;
using System.Collections.Generic;
using Phigodot.Game;
using System.Runtime.InteropServices;

public partial class JudgeLineNode : Sprite2D
{
	// TODO:
	// Properties Calculate by time -finished!
	// Note Generate                -half done!
	// Note YPos Calculate
	public int LineIndex;
	public ChartRPE Chart;
	
	[Export] public Texture2D TapTexture;
	[Export] public Texture2D FlickTexture;
	[Export] public Texture2D DragTexture;	

	[Export] public Label Idex;



	/// <summary>
	/// Thank DianZhuiXingKong for providing Note Position Calculate Logic.
	/// </summary>
	public List<RPENote> Notes
	{
		set
		{
			Idex.Text = LineIndex.ToString();
			var noteList = value;
			if(noteList == null) return;

			foreach(RPENote noteInfo in noteList)
			{
				var instance = NoteScene.Instantiate<NoteNode>();
				instance.NoteInfo = noteInfo;
				noteInstances.Add(instance);
				switch (noteInfo.Type)
				{
					case 1:
						instance.Texture = TapTexture;
						break;
					case 2:
						instance.Texture = TapTexture;//hold
						break;
					case 3:
						instance.Texture = FlickTexture;
						break;
					case 4:
						instance.Texture = DragTexture;
						break;
					default:
						break;
				}
				var posX = noteInfo.PositionX*StageSize.X/1350.0f;
				var posY = 0.0f;
				// TODO var posY =noteInfo.floorPosition*StageSize.Y;
				if(instance.NoteInfo.Above != 1) {posX = -posX;posY = -posY;}
				instance.Position = new Vector2(-posX ,posY);
				AddChild(instance);
			}
		}
	}

	[Export] public PackedScene NoteScene;
	public List<NoteNode> noteInstances = new List<NoteNode>();



	public Vector2I StageSize;
	public double AspectRatio = 1.666667d;

	public double ChartTime
	{
		set
		{
			var EventLayers = Chart.JudgeLineList[LineIndex].EventLayers;

			double xPos = 0;
			double yPos = 0;
			double alpha = 0;
			double rotate = 0;

			foreach(EventLayer layer in EventLayers)
			{
				xPos += layer.MoveXEvents.GetValByTime(value);
				yPos += layer.MoveYEvents.GetValByTime(value);
				alpha += layer.AlphaEvents.GetValByTime(value);
				rotate += layer.RotateEvents.GetValByTime(value);
			}

			Position = ChartRPE.RPEPos2PixelPos(new Vector2((float)xPos,-(float)yPos),StageSize) + (StageSize/2);
			RotationDegrees = (float)rotate + 180;
			var m = SelfModulate;
			SelfModulate = new Color(m.R,m.G,m.B,(float)alpha/255.0f);



			
			foreach(NoteNode noteNode in noteInstances)
			{
				foreach(EventLayer layer in EventLayers)
				{
					// todo calc note yPos
				}
			}
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
