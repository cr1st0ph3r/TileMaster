 using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileMaster
{
   public class Camera
    {
        public Matrix transform;//<-draws the camera on the screen
        private Viewport viewPort;//FOV
        private Vector2 center;//<-center of the camera
        private Vector2 position;//<-position of the camera

        public Vector2 Center => center;
        public Vector2 Position => position;

        public Camera(Viewport view)
        {
            viewPort = view;
            position = new Vector2(0,0);
        }

        public Matrix Transform
        {
            get { return transform; }
        }

        public void Update(Vector2 playerPosition/*to follow*/, int xOffset, int yOffset) 
        {
            //debug
            //var value = ((xOffset / Global.TileSize) + Global.ChunkSize) - (viewPort.Width / 2);

            //=========== X axis camera ============//

            //prevents camera from going beyond boundary x (<----)
            if (playerPosition.X < (viewPort.Width / 2))
            {
               center.X = (viewPort.Width/2);
            }
            //prevents camera from going beyond boundary x (---->)            
            else if (playerPosition.X > xOffset - (viewPort.Width / 2))
            {
                //do not move
            }
            //segue player
            else
            {
               center.X = playerPosition.X;
               position.X = center.X - (viewPort.Width / 2);
            }

            //=========== Y axis camera ============//
             
            //prevents the camera from moving beyond the upper bounds of the map
            if (playerPosition.Y < (viewPort.Height / 2))
            {
                center.Y = (viewPort.Height/2);
            }

            else if (playerPosition.Y > yOffset - (viewPort.Height / 2))
            {
                //center.Y = yOffset - (viewPort.Height / 2);
                //do not move
            }
            else
            {
                center.Y = playerPosition.Y;
                position.Y = center.Y - (viewPort.Height / 2);
            }

            transform = Matrix.CreateTranslation(
                new Vector3(
                    -center.X + (viewPort.Width / 2),
                    -center.Y + (viewPort.Height / 2), 0));
        }
    }
}
