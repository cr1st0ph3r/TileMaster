 using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileMaster
{
   public class Camera
    {
        public Matrix transform;//<-draws the camera o the screeen
        private Viewport viewPort;//FOV
        private Vector2 center;//<-center of the camera

        public Vector2 Center => center;

        public Camera(Viewport view)
        {
            this.viewPort = view;
        }

        public Matrix Tramsform
        {
            get { return transform; }
        }

        public void Update(Vector2 playerPosistion/*to follow*/, int xOffset, int yOffset) 
        {
            var value = ((xOffset / Global.Tilesize) + Global.ChunkSize) - (viewPort.Width / 2);

            //=========== X axis camera ============//

            //bloqueia a camera de se mover alem da boundary x (<----)
            if (playerPosistion.X < (viewPort.Width / 2))
            {
               center.X = (viewPort.Width/2);
            }
            //bloqueia a camera de se mover alem da boundary x (---->)            
            else if (playerPosistion.X > xOffset - (viewPort.Width / 2))
            {
                //do not move
            }
            //segue player
            else
            {
               center.X = playerPosistion.X;
            }

            //=========== Y axis camera ============//

            //bloqueia a camera de mover alem da fronteira superior do mapa
            if (playerPosistion.Y < (viewPort.Height / 2))
            {
                center.Y = (viewPort.Height/2);
            }

            else if (playerPosistion.Y > yOffset - (viewPort.Height / 2))
            {
                //center.Y = yOffset - (viewPort.Height / 2);
                //do not move
            }
            else
            {
                center.Y = playerPosistion.Y;
            }

            transform = Matrix.CreateTranslation(
                new Vector3(
                    -center.X + (viewPort.Width / 2),
                    -center.Y + (viewPort.Height / 2), 0));
        }
    }
}
