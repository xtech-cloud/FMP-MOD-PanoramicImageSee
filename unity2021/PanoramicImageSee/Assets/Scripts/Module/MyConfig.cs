
using System.Xml.Serialization;

namespace XTC.FMP.MOD.PanoramicImageSee.LIB.Unity
{
    /// <summary>
    /// 配置类
    /// </summary>
    public class MyConfig : MyConfigBase
    {
        public class Position
        {
            [XmlAttribute("x")]
            public float x { get; set; } = 0;
            [XmlAttribute("y")]
            public float y { get; set; } = 0;
            [XmlAttribute("z")]
            public float z { get; set; } = 0;
        }

        public class Rotation
        {
            [XmlAttribute("x")]
            public float x { get; set; } = 0;
            [XmlAttribute("y")]
            public float y { get; set; } = 0;
            [XmlAttribute("z")]
            public float z { get; set; } = 0;
        }

        public class Pending
        {
            [XmlAttribute("image")]
            public string image { get; set; } = "";
        }

        public class SpaceGrid
        {
            [XmlElement("Position")]
            public Position position { get; set; } = new Position();
        }


        public class Background
        {
            [XmlAttribute("visible")]
            public bool visible { get; set; } = true;
            [XmlAttribute("color")]
            public string color { get; set; } = "#00000000";
        }

        public class Style
        {
            [XmlAttribute("name")]
            public string name { get; set; } = "";
            [XmlElement("Background")]
            public Background background { get; set; } = new Background();
            [XmlElement("Pending")]
            public Pending pending { get; set; } = new Pending();
            [XmlElement("SpaceGrid")]
            public SpaceGrid spaceGrid { get; set; } = new SpaceGrid();
        }


        [XmlArray("Styles"), XmlArrayItem("Style")]
        public Style[] styles { get; set; } = new Style[0];
    }
}

