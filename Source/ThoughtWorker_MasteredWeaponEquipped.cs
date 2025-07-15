using RimWorld;
using Verse;

namespace SK_WeaponMastery
{
    public class ThoughtWorker_MasteredWeaponEquipped : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.equipment == null) {
                return false;
            }
            ThingWithComps weapon = p.equipment?.Primary;
            if (weapon == null) { 
                return false; 
            }
            MasteryWeaponComp comp = weapon.TryGetComp<MasteryWeaponComp>();
            if (comp == null || !comp.IsActive()) return false;
            if (ModSettings.useMoods && comp.PawnHasMastery(p)) {
                if (ModsConfig.IdeologyActive && Utils.PawnIdeologyDespisesWeapon(p, weapon)) 
                { 
                    return false; 
                }
                return true;
            }
            return false;
        }
    }
}
