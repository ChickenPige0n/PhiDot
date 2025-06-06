using System;
using System.IO;
using Godot;


public class ChartData
{
    //曲绘绝对路径
    public string ImageSource { get; private set; }
    public string ImageFileName { get; private set; }
    public string MusicFileName { get; private set; }
    public string ChartFileName { get; private set; }
    
    public string ChartName { get; private set; }
    public string ChartDiff { get; private set; }
    public string ChartPath { get; set; }

    public string Composer { get; set; }
    public string Charter { get; set; }

    public string Illustrator { get; set; }
    
    public DirAccess Root { get; set; }

    private ChartData()
	{
	    ImageSource   = string.Empty;
	    ImageFileName = string.Empty;
	    MusicFileName = string.Empty;
	    ChartFileName = string.Empty;
	    ChartName     = string.Empty;
	    ChartDiff     = string.Empty;
	    ChartPath     = string.Empty;
	    Composer      = string.Empty;
	    Charter       = string.Empty;
	    Illustrator   = string.Empty;
        Root          = null;
    }
    public static ChartData FromString(DirAccess rootDir, string infoContent)
    {
    	var cd = new ChartData
        {
            Root = rootDir
        };
        var lines = infoContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var infos = line.Split(new[] { ':' });
            switch (infos[0].Trim())
            {
                case "Name":
                    cd.ChartName = infos[1].Trim();
                    break;
                case "Path":
                    cd.ChartPath = infos[1].Trim();
                    break;
                case "Picture":
                    cd.ImageSource = $"{rootDir.GetCurrentDir()}/{infos[1].Trim()}";
                    cd.ImageFileName = infos[1].Trim();
                    break;
                case "Level":
                    cd.ChartDiff = infos[1].Trim();
                    break;
                case "Song":
                    cd.MusicFileName = infos[1].Trim();
                    break;
                case "Chart":
                    cd.ChartFileName = infos[1].Trim();
                    break;
                case "Composer":
                    cd.Composer = infos[1].Trim();
                    break;
                case "Charter":
                    cd.Charter = infos[1].Trim();
                    break;
            }
        }
    	return cd;
    }
}

