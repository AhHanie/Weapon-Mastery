using RimWorld;
using Verse;

namespace SK_WeaponMastery
{
    // Store a single statdef with a bonus offset
    public class MasteryStat : IExposable
    {
        private StatDef stat;
        private string statDefName;
        private float offset;

        // Empty constructor needed for Scribe loading
        public MasteryStat()
        {

        }

        public MasteryStat(StatDef stat, float offset)
        {
            this.stat = stat;
            this.statDefName = stat.defName;
            this.offset = offset;
        }

        public StatDef GetStat()
        {
            return stat;
        }

        public float GetOffset()
        {
            return offset;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref statDefName, "stat");
            Scribe_Values.Look(ref offset, "offset");
        }

        public void SetOffset(float value)
        {
            offset = value;
        }

        public string GetStatDefName()
        {
            return statDefName;
        }

        // Load StatDef from DefDatabase
        public void Resolve()
        {
            stat = DefDatabase<StatDef>.GetNamed(statDefName, false);
        }
    }
}
