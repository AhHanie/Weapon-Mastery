using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SK_WeaponMastery
{

    // Store a list of bonuses, experience and mastery level for a single weapon
    public class MasteryWeaponCompData : MasteryCompData, IExposable
    {
        private Pawn pawn;
        public MasteryWeaponCompData() { }

        public MasteryWeaponCompData(Pawn pawn)
        {
            this.pawn = pawn;
            bonusStats = new Dictionary<StatDef, float>();
        }

        // Save/Load class variables to/from rws file
        // Store defName string instead of defs to handle defs being removed
        // mid game.
        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                List<KeyValuePair<StatDef, float>> list = bonusStats.ToList();
                List<float> floats = new List<float>();
                List<StatDef> defs = new List<StatDef>();
                foreach (KeyValuePair<StatDef, float> item in list)
                {
                    floats.Add(item.Value);
                    defs.Add(item.Key);
                }
                Scribe_Collections.Look(ref floats, "floats", LookMode.Value);
                Scribe_Collections.Look(ref defs, "defs", LookMode.Def);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<float> floats = new List<float>();
                List<StatDef> defs = new List<StatDef>();
                bonusStats = new Dictionary<StatDef, float>();
                Scribe_Collections.Look(ref floats, "floats", LookMode.Value);
                Scribe_Collections.Look(ref defs, "defs", LookMode.Def);
                for (int i = 0; i < defs.Count; i++)
                    if (defs[i] != null)
                        bonusStats.Add(defs[i], floats[i]);
            }
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref masteryLevel, "masterylevel");
            Scribe_Values.Look(ref experience, "experience");
        }
    }
}
