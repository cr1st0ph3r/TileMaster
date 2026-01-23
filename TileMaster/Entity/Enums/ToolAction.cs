namespace TileMaster.Entity.Enums
{
    /// <summary>
    /// Defines the action a tool can perform
    /// </summary>
    public enum ToolAction
    {
        /// <summary>
        /// Mine the block and remove from map
        /// </summary>
        MineBlock = 0,
        /// <summary>
        /// Hammer action to change block slope
        /// </summary>
        TransformBlock = 1,
        /// <summary>
        /// Hammer action to change block slope
        /// </summary>
        RangedWeapon = 2,
    }
}
