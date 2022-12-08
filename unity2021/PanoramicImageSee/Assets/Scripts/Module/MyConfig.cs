
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

        public class Button
        {
            [XmlAttribute("visible")]
            public bool visible { get; set; } = true;
            [XmlAttribute("image")]
            public string image { get; set; } = "";
        }

        public class Effect
        {
            [XmlAttribute("active")]
            public string active { get; set; } = "";
            [XmlElement("TeleporterZoomEffect")]
            public TeleporterZoomEffect teleporterZoomEffect { get; set; } = new TeleporterZoomEffect();
        }

        public class TeleporterZoomEffect
        {
            [XmlAttribute("scale")]
            public float scale { get; set; } = 1f;
            [XmlAttribute("duration")]
            public float duration { get; set; } = 2f;
        }

        public class Padding
        {
            [XmlAttribute("left")]
            public int left { get; set; } = 0;
            [XmlAttribute("right")]
            public int right { get; set; } = 0;
            [XmlAttribute("top")]
            public int top { get; set; } = 0;
            [XmlAttribute("bottom")]
            public int bottom { get; set; } = 0;
        }

        public class CellSize
        {
            [XmlAttribute("width")]
            public int width { get; set; } = 0;
            [XmlAttribute("height")]
            public int height { get; set; } = 0;
        }

        public class Spacing
        {
            [XmlAttribute("x")]
            public int x { get; set; } = 0;
            [XmlAttribute("y")]
            public int y { get; set; } = 0;
        }

        public class ToolBar : UiElement
        {
            [XmlAttribute("visible")]
            public bool visible { get; set; } = false;
            [XmlAttribute("color")]
            public string color { get; set; } = "#00000088";

            [XmlElement("Padding")]
            public Padding padding { get; set; } = new Padding();
            [XmlElement("CellSize")]
            public CellSize cellSize { get; set; } = new CellSize();
            [XmlElement("Spacing")]
            public Spacing spacing { get; set; } = new Spacing();

            [XmlElement("ButtonClose")]
            public Button buttonClose { get; set; } = new Button();
            [XmlElement("ButtonZoomIn")]
            public Button buttonZoomIn { get; set; } = new Button();
            [XmlElement("ButtonZoomOut")]
            public Button buttonZoomOut { get; set; } = new Button();
        }

        public class Style
        {
            [XmlAttribute("name")]
            public string name { get; set; } = "";
            [XmlAttribute("fov")]
            public int fov { get; set; } = 3;
            [XmlElement("Background")]
            public Background background { get; set; } = new Background();
            [XmlElement("Pending")]
            public Pending pending { get; set; } = new Pending();
            [XmlElement("SpaceGrid")]
            public SpaceGrid spaceGrid { get; set; } = new SpaceGrid();
            [XmlElement("Effect")]
            public Effect effect { get; set; } = new Effect();
            [XmlElement("ToolBar")]
            public ToolBar toolbar { get; set; } = new ToolBar();
        }


        [XmlArray("Styles"), XmlArrayItem("Style")]
        public Style[] styles { get; set; } = new Style[0];
    }
}

