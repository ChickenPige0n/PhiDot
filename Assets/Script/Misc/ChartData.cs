using System;
using System.IO;

namespace Phidot.Game
{
    public class ChartData
    {
        //曲绘绝对路径
        public string ImageSource { get; set; }
        public string MusicFileName { get; set; }
        public string ChartFileName { get; set; }
        
        public string ChartName { get; set; }
        public string ChartDiff { get; set; }
        public string ChartPath { get; set; }

        public string Composer { get; set; }
        public string Charter { get; set; }

        public string Illustrator { get; set; }

	public ChartData()
    	{
    	    ImageSource   = string.Empty;
    	    MusicFileName = string.Empty;
    	    ChartFileName = string.Empty;
    	    ChartName     = string.Empty;
    	    ChartDiff     = string.Empty;
    	    ChartPath     = string.Empty;
    	    Composer      = string.Empty;
    	    Charter       = string.Empty;
    	    Illustrator   = string.Empty;
    	}
	public static ChartData FromString(string rootDir, string infoContent)
	{
		ChartData cd = new ChartData();
        	string[] lines = infoContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        	foreach (string line in lines)
        	{
        	    string[] infos = line.Split(new[] { ':' });
        	    switch (infos[0].Trim())
        	    {
        	        case "Name":
        	            cd.ChartName = infos[1].Trim();
        	            break;
        	        case "Path":
        	            cd.ChartPath = infos[1].Trim();
        	            break;
        	        case "Picture":
        	            cd.ImageSource = Path.Combine(rootDir,infos[1].Trim());
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


}
