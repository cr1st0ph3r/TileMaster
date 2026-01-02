using Myra.Graphics2D.UI;

namespace TileMaster.UI
{
	public partial class DebugWindow
	{
        public SpinButton SPFramesPerSecond = new SpinButton();
        public SpinButton SPPlayerPositionX = new SpinButton();
        public SpinButton SPPlayerPositionY = new SpinButton();
        public DebugWindow()
		{
			BuildUI();			
		}
	}
}