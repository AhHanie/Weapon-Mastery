using RimWorld;
using Verse;

namespace SK_WeaponMastery
{
    /*
     Stat Defs contain a list of stat parts which affect the final stat value
     calculation.
    
     This class is added into each vanilla Stat Def stat parts list 
     in order to modify the final stat value calculations.

     Class tries to find MasteryWeaponComp and MasteryPawnComp, gets the 
     necessary bonus value and modifies calculation accordingly.
    */
    class StatPart_Mastery : StatPart
    {
        public StatPart_Mastery(StatDef parentStat)
        {
            this.parentStat = parentStat;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing) return;

            if (req.Thing.def.race != null)
            {
                // Stat bonuses for pawn
                Pawn pawn = req.Thing as Pawn;
                if (pawn == null || pawn.equipment?.Primary == null) return;
                MasteryWeaponComp comp = pawn.equipment.Primary.TryGetComp<MasteryWeaponComp>();
                if (comp != null && comp.IsActive())
                {
                    val += comp.GetCombinedBonus(parentStat);
                }

                MasteryPawnComp compPawn = pawn.TryGetComp<MasteryPawnComp>();
                if (compPawn == null || !compPawn.IsActive() || !ModSettings.classes.ContainsKey(pawn.equipment.Primary.def)) return;
                val += compPawn.GetStatBonus(ModSettings.classes[pawn.equipment.Primary.def], parentStat);
            }
            else if (req.Thing.def.HasComp(typeof(MasteryWeaponComp)))
            {
                // Stat bonuses for gun
                MasteryWeaponComp comp = req.Thing.TryGetComp<MasteryWeaponComp>();
                if (comp == null || !comp.IsActive()) return;
                val += comp.GetCombinedBonus(parentStat);

                Pawn owner = comp.GetCurrentOwner();
                if (owner == null) return;
                MasteryPawnComp compPawn = comp.GetCurrentOwner().TryGetComp<MasteryPawnComp>();
                if (compPawn == null || !compPawn.IsActive() || !ModSettings.classes.ContainsKey(req.Thing.def)) return;
                val += compPawn.GetStatBonus(ModSettings.classes[req.Thing.def], parentStat);
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            float bonus = 0;
            if (req.HasThing && req.Thing.def.race != null)
            {
                // Stat bonuses for pawn
                Pawn pawn = req.Thing as Pawn;
                if (pawn == null || pawn.equipment?.Primary == null) return "";
                MasteryWeaponComp comp = pawn.equipment.Primary.TryGetComp<MasteryWeaponComp>();
                if (comp != null && comp.IsActive())
                {
                    bonus += comp.GetCombinedBonus(parentStat);
                }

                MasteryPawnComp compPawn = pawn.TryGetComp<MasteryPawnComp>();
                if (compPawn != null && compPawn.IsActive() && ModSettings.classes.ContainsKey(pawn.equipment.Primary.def))
                    bonus += compPawn.GetStatBonus(ModSettings.classes[pawn.equipment.Primary.def], parentStat);
            }
            else if (req.HasThing && req.Thing.def.HasComp(typeof(MasteryWeaponComp)))
            {
                // Stat bonuses for gun
                MasteryWeaponComp comp = req.Thing.TryGetComp<MasteryWeaponComp>();
                if (comp == null || !comp.IsActive()) return "";
                bonus += comp.GetCombinedBonus(parentStat);

                Pawn owner = comp.GetCurrentOwner();
                if (owner != null)
                {
                    MasteryPawnComp compPawn = comp.GetCurrentOwner().TryGetComp<MasteryPawnComp>();
                    if (compPawn != null && compPawn.IsActive() && ModSettings.classes.ContainsKey(req.Thing.def))
                        bonus += compPawn.GetStatBonus(ModSettings.classes[req.Thing.def], parentStat);
                }
            }
            else bonus = 0;

            return bonus >= 0f ? 
                "SK_WeaponMastery_WeaponMasteryDescriptionItem".Translate().ToString() + " + " + parentStat.ValueToString(UnityEngine.Mathf.Abs(bonus)) : 
                "SK_WeaponMastery_WeaponMasteryDescriptionItem".Translate().ToString() + " - " + parentStat.ValueToString(UnityEngine.Mathf.Abs(bonus));
        }
    }
}
