using Myra.Graphics2D.UI;

namespace TileMaster.UI
{
	public partial class DebugWindow
	{
        public SpinButton SPFramesPerSecond = new SpinButton();
        public SpinButton SPPlayerPositionX = new SpinButton();
        public SpinButton SPPlayerPositionY = new SpinButton();

        public Label LblCameraPosition = new Label();
        public Label LblMapSize = new Label();
        public Label LblPlayerGrid = new Label();
        public Label LblCursorGrid = new Label();
        public Label LblIsMoving = new Label();
        public Label LblPlayerVelocity = new Label();
        public Label LblPlayerInsideBlock = new Label();
        public Label LblPlayerLayer = new Label();
        public Label LblPlayerSteppingOn = new Label();
        public Label LblPlayerOnChunk = new Label();
        public Label LblPlayerOnSolidGround = new Label();
        public Label LblMouseOnChunk = new Label();
        public Label LblMousePos = new Label();
        public Label LblMouseBlockIn = new Label();

        public Label LblTileId = new Label();
        public Label LblTileName = new Label();
        public Label LblTileLocalId = new Label();
        public Label LblTileGlobalId = new Label();
        public Label LblTileChunkId = new Label();
        public Label LblTileIsEdge = new Label();
        public Label LblTileIsSolid = new Label();

        public DebugWindow()
		{
			BuildUI();			
		}

        public void UpdateDebugInfo(
            string cameraPos, string mapSize, string playerGrid, string cursorGrid,
            string isMoving, string velocity, string insideBlock, string layer,
            string steppingOn, string onChunk, string solidGround, string mouseChunk,
            string mousePos, string mouseBlock,
            TileMaster.Entity.Tiles.Tile block)
        {
            LblCameraPosition.Text = cameraPos;
            LblMapSize.Text = mapSize;
            LblPlayerGrid.Text = playerGrid;
            LblCursorGrid.Text = cursorGrid;
            LblIsMoving.Text = isMoving;
            LblPlayerVelocity.Text = velocity;
            LblPlayerInsideBlock.Text = insideBlock;
            LblPlayerLayer.Text = layer;
            LblPlayerSteppingOn.Text = steppingOn;
            LblPlayerOnChunk.Text = onChunk;
            LblPlayerOnSolidGround.Text = solidGround;
            LblMouseOnChunk.Text = mouseChunk;
            LblMousePos.Text = mousePos;
            LblMouseBlockIn.Text = mouseBlock;

            if (block != null)
            {
                LblTileId.Text = block.TileId.ToString();
                LblTileName.Text = block.Name;
                LblTileLocalId.Text = block.LocalId.ToString();
                LblTileGlobalId.Text = block.GlobalId.ToString();
                LblTileChunkId.Text = block.ChunkId.ToString();
                LblTileIsEdge.Text = block.isEdgeTile.ToString();
                LblTileIsSolid.Text = block.IsSolid.ToString();
            }
            else
            {
                LblTileId.Text = "N/A";
                LblTileName.Text = "N/A";
                LblTileLocalId.Text = "N/A";
                LblTileGlobalId.Text = "N/A";
                LblTileChunkId.Text = "N/A";
                LblTileIsEdge.Text = "N/A";
                LblTileIsSolid.Text = "N/A";
            }
        }
    }
}