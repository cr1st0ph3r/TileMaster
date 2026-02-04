using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace TileMaster.Model
{
    public class RectangleData
    {
        public int TextureId { get; set; }
        public Rectangle Rectangle { get; set; }
        public List<Rectangle> AlternativeRectangles { get; set; }
        public bool HaveAlternativeData => AlternativeRectangles != null && AlternativeRectangles.Count > 0;
    }
}
