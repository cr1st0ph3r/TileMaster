using TileMaster.Entity.Enums;

namespace TileMaster.Data
{
    public class ReferenceMob
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Movement { get; set; }
        public int MoveSpeed { get; set; }
        public int WalkFrames { get; set; }
        public int DamageFrames { get; set; }
        public int Health { get; set; }
        public int AttackPower { get; set; }
        public int Defense { get; set; }
        public MobType MobType { get; set; }
    }
}
