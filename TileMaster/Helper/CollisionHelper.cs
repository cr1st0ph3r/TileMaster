using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileMaster.Entity;

namespace TileMaster.Helper
{
    public static class CollisionHelper
    {

        public static bool IsColliding(Tile Player, Component Tile, string Direction)
        {

            //if (Direction == "Left")
            //{
            //    return
            //        Player.Rectangle.Right + Player.velocity.X > Tile.Rectangle.Left &&
            //        Player.Rectangle.Left < Tile.Rectangle.Left &&
            //        Player.Rectangle.Bottom > Tile.Rectangle.Top &&
            //        Player.Rectangle.Top < Tile.Rectangle.Bottom;
            //}
            //else if (Direction == "Right")
            //{
            //    return
            //        Player.Rectangle.Left + Player.velocity.X < Tile.Rectangle.Right &&
            //        Player.Rectangle.Right > Tile.Rectangle.Right &&
            //        Player.Rectangle.Bottom > Tile.Rectangle.Top &&
            //        Player.Rectangle.Top < Tile.Rectangle.Bottom;
            //}
            //else if (Direction == "Bottom")
            //{
            //    return
            //        Player.Rectangle.Top + Player.velocity.Y < Tile.Rectangle.Bottom &&
            //        Player.Rectangle.Bottom > Tile.Rectangle.Bottom &&
            //        Player.Rectangle.Right > Tile.Rectangle.Left &&
            //        Player.Rectangle.Left < Tile.Rectangle.Right;


            //}
            //else if (Direction == "Top")
            //{
            //    return
            //        Player.Rectangle.Bottom + Player.velocity.Y > Tile.Rectangle.Top &&
            //        Player.Rectangle.Top < Tile.Rectangle.Top &&
            //        Player.Rectangle.Right > Tile.Rectangle.Left &&
            //        Player.Rectangle.Left < Tile.Rectangle.Right;
            //}
            return false;
        }
    }
}
