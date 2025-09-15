using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SK_WeaponMastery
{

    //  Store pawns with a list of bonuses, experience and mastery level 
    // for all pawns that used this weapon
    public class MasteryWeaponComp : ThingComp
    {

        private Dictionary<Pawn, MasteryWeaponCompData> bonusStatsPerPawn;
        private Dictionary<StatDef, float> relicBonuses;
        private Pawn currentOwner;
        private bool isActive = false;
        private string masteryDescription;
        private string weaponName;

        // These variables are needed for loading data from save file in PostExposeData()
        List<Pawn> dictKeysAsList;
        List<MasteryWeaponCompData> dictValuesAsList;

        public void Init()
        {
            bonusStatsPerPawn = new Dictionary<Pawn, MasteryWeaponCompData>();
            if (ModsConfig.IdeologyActive && this.parent.IsRelic())
            {
                relicBonuses = new Dictionary<StatDef, float>();
                for (int i = 0; i < ModSettings.numberOfRelicBonusStats; i++)
                {
                    MasteryStat bonus = ModSettings.PickBonus(this.parent.def.IsMeleeWeapon);
                    if (bonus != null)
                        if (relicBonuses.ContainsKey(bonus.GetStat()))
                            relicBonuses[bonus.GetStat()] += bonus.GetOffset();
                        else
                            relicBonuses[bonus.GetStat()] = bonus.GetOffset();
                }
            }
            isActive = true;
        }

        // Add new stat bonus for a pawn
        public void AddStatBonus(Pawn pawn, StatDef bonusStat, float value)
        {
            if (!bonusStatsPerPawn.ContainsKey(pawn))
                bonusStatsPerPawn[pawn] = new MasteryWeaponCompData(pawn);

            bonusStatsPerPawn[pawn].AddStatBonus(bonusStat, value);
        }

        // Check if anyone mastered comp attached to this weapon
        public bool IsActive()
        {
            return isActive;
        }

        // Get stat bonus for current owner
        private float GetStatBonus(StatDef stat)
        {
            if (!bonusStatsPerPawn.ContainsKey(currentOwner))
                return 0;
            return bonusStatsPerPawn[currentOwner].GetStatBonus(stat);
        }

        // Get Relic bonus
        private float GetStatBonusRelic(StatDef stat)
        {
            if (relicBonuses == null || !relicBonuses.ContainsKey(stat) || !OwnerBelievesInSameIdeology())
                return 0;

            return relicBonuses[stat];
        }

        // Get bonus from mastery and relics
        public float GetCombinedBonus(StatDef stat)
        {
            // The currentOwner could be deleted by the game due to 
            // different reasons. Raid, dying, a visitor, etc ...
            // Let's put a null check here
            if (currentOwner == null) return 0;
            return GetStatBonus(stat) + GetStatBonusRelic(stat);
        }

        // Check if current owner believes in this relic's ideology
        private bool OwnerBelievesInSameIdeology()
        {
            Precept_ThingStyle per = this.parent.StyleSourcePrecept;
            return per?.ideo == currentOwner.Ideo;
        }

        public void AddExp(Pawn pawn, int experience)
        {
            if (!bonusStatsPerPawn.ContainsKey(pawn))
                bonusStatsPerPawn[pawn] = new MasteryWeaponCompData(pawn);

            float multiplier = 1;
            if (ModsConfig.RoyaltyActive && IsBondedWeapon()) multiplier = ModSettings.bondedWeaponExperienceMultipier;

            bonusStatsPerPawn[pawn].AddExp((int)(experience * multiplier), this.parent.def.IsMeleeWeapon, delegate (int level)
            {
                float roll = Rand.Value;
                if (level == 1)
                {
                    if (weaponName == null && ModSettings.chanceToNameWeapon > 0 && roll <= ModSettings.chanceToNameWeapon)
                    {
                        if (ModSettings.KeepOriginalWeaponNameQuality)
                            weaponName = $"\"{ModSettings.PickWeaponName()}\" {this.parent.LabelCap}";
                        else
                            weaponName = ModSettings.PickWeaponName();
                        Messages.Message(ModSettings.messages.RandomElement().Translate(pawn.NameShortColored, weaponName), MessageTypeDefOf.NeutralEvent);
                    }
                }
                // Update cached description when pawn levels up
                GenerateDescription();
            });
        }

        // Check if weapon is bonded to current owner
        private bool IsBondedWeapon()
        {
            CompBladelinkWeapon link = this.parent.TryGetComp<CompBladelinkWeapon>();
            return link?.CodedPawn == currentOwner;
        }

        // Remove null pawns before saving
        void FilterNullPawns()
        {
            List<KeyValuePair<Pawn, MasteryWeaponCompData>> filtered = bonusStatsPerPawn.ToList();
            bonusStatsPerPawn.Clear();
            for (int i = 0; i < filtered.Count; i++)
            {
                KeyValuePair<Pawn, MasteryWeaponCompData> item = filtered[i];
                if (!item.Key.DestroyedOrNull()) bonusStatsPerPawn.Add(item.Key, item.Value);
            }
        }

        // Save/Load comp data to/from rws file
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_References.Look(ref currentOwner, "currentowner");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Saving process, allow only active comps to be saved so
                // we don't fill save file with null comps
                if (isActive)
                {
                    FilterNullPawns();
                    if (bonusStatsPerPawn == null && weaponName == null && relicBonuses == null)
                    {
                        isActive = false;
                        return;
                    }
                    if (bonusStatsPerPawn != null && bonusStatsPerPawn.Count > 0)
                    {
                        List<Pawn> pawns = bonusStatsPerPawn.Keys.ToList();
                        List<MasteryWeaponCompData> data = bonusStatsPerPawn.Values.ToList();
                        Scribe_Collections.Look(ref bonusStatsPerPawn, "bonusstatsperpawn", LookMode.Reference, LookMode.Deep, ref pawns, ref data);
                    }
                    Scribe_Values.Look(ref isActive, "isactive");
                    if (weaponName != null)
                        Scribe_Values.Look(ref weaponName, "weaponname");
                    if (this.parent.IsRelic() && relicBonuses != null)
                        Scribe_Collections.Look(ref relicBonuses, "relicbonuses", LookMode.Def, LookMode.Value);
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                // Loading process
                Scribe_Values.Look(ref isActive, "isactive", false);
                if (isActive)
                {
                    Scribe_Collections.Look(ref bonusStatsPerPawn, "bonusstatsperpawn", LookMode.Reference, LookMode.Deep, ref dictKeysAsList, ref dictValuesAsList);
                    Scribe_Values.Look(ref weaponName, "weaponname");
                    Scribe_Collections.Look(ref relicBonuses, "relicbonuses", LookMode.Def, LookMode.Value);
                }
            }
            else if (Scribe.mode == LoadSaveMode.PostLoadInit && isActive)
                if (bonusStatsPerPawn != null && AnyPawnHasMastery()) GenerateDescription();
                else if (bonusStatsPerPawn == null) bonusStatsPerPawn = new Dictionary<Pawn, MasteryWeaponCompData>();
        }

        public void SetCurrentOwner(Pawn newOwner)
        {
            currentOwner = newOwner;
        }

        public Pawn GetCurrentOwner()
        {
            return currentOwner;
        }

        public override bool AllowStackWith(Thing other)
        {
            return false;
        }

        // Weapon's name in GUI menus
        public override string TransformLabel(string label)
        {
            if (isActive && weaponName != null)
                return weaponName;
            return base.TransformLabel(label);
        }

        public void GenerateDescription()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(base.GetDescriptionPart());
            sb.AppendLine("SK_WeaponMastery_WeaponMasteryDescriptionItemWeaponBond".Translate());
            List<KeyValuePair<Pawn, MasteryWeaponCompData>> data = bonusStatsPerPawn.ToList();
            string positiveValueColumn = ": +";
            string negativeValueColumn = ": ";
            for (int i = 0; i < data.Count; i++)
            {
                KeyValuePair<Pawn, MasteryWeaponCompData> item = data[i];
                if (item.Key == null) continue;
                sb.AppendLine();
                sb.AppendLine(item.Key.Name.ToString());
                foreach (KeyValuePair<StatDef, float> statbonus in item.Value.GetStatBonusesAsList())
                    sb.AppendLine(" " + statbonus.Key.label.CapitalizeFirst() + (statbonus.Value >= 0f ? positiveValueColumn : negativeValueColumn) + statbonus.Key.ValueToString(statbonus.Value));
                if (ModSettings.displayExperience && !item.Value.IsMaxLevel()) sb.AppendLine("Level: " + item.Value.GetMasteryLevel() + "  Experience: " + item.Value.GetExperience() + "/" + ModSettings.GetExperienceForLevel(item.Value.GetMasteryLevel()));
            }
            sb.AppendLine();
            if (relicBonuses != null)
            {
                sb.AppendLine("SK_WeaponMastery_WeaponMasteryDescriptionTitleRelicBonus".Translate());
                foreach (KeyValuePair<StatDef, float> statbonus in relicBonuses.ToList())
                    sb.AppendLine(" " + statbonus.Key.label.CapitalizeFirst() + (statbonus.Value >= 0f ? positiveValueColumn : negativeValueColumn) + statbonus.Key.ValueToString(statbonus.Value));
            }
            masteryDescription = sb.ToString();
        }

        // Weapon Description
        // Display all mastered pawns with their bonuses
        public override string GetDescriptionPart()
        {
            if (!isActive || (!AnyPawnHasMastery() && !ModSettings.displayExperience)) return base.GetDescriptionPart();
            return masteryDescription;
        }

        public void GenerateWeaponName()
        {
            if (ModSettings.KeepOriginalWeaponNameQuality)
                weaponName = $"\"{ModSettings.PickWeaponName()}\" {this.parent.LabelCap}";
            else
                weaponName = ModSettings.PickWeaponName();
        }

        // Check if pawn mastered this weapon
        public bool PawnHasMastery(Pawn pawn)
        {
            if (bonusStatsPerPawn == null || !bonusStatsPerPawn.ContainsKey(pawn)) return false;
            return bonusStatsPerPawn[pawn].HasMastery();
        }

        // Check if any of the pawns stored mastered this weapon
        private bool AnyPawnHasMastery()
        {
            List<KeyValuePair<Pawn, MasteryWeaponCompData>> data = bonusStatsPerPawn.ToList();
            for (int i = 0; i < data.Count; i++)
            {
                KeyValuePair<Pawn, MasteryWeaponCompData> item = data[i];
                if (item.Value.HasMastery()) return true;
            }
            return false;
        }

        // Override weapon equip event
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (!isActive) return;
            SetCurrentOwner(pawn);
            if (ModSettings.useMoods && PawnHasMastery(pawn))
            {
                if (ModsConfig.IdeologyActive && Utils.PawnIdeologyDespisesWeapon(pawn, this.parent)) return;
                pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(Core.MasteredWeaponUnequipped);
            }
        }

        // Override weapon unequip event
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (!isActive || !ModSettings.useMoods) return;
            if (PawnHasMastery(pawn))
            {
                // Despised weapons do not give mood debuffs
                if (ModsConfig.IdeologyActive && Utils.PawnIdeologyDespisesWeapon(pawn, this.parent)) return;
                // Is this a main/sidearm weapon switch?
                if (Compat.SimpleSidearmsCompat.enabled && (Compat.SimpleSidearmsCompat.weaponSwitch || Compat.SimpleSidearmsCompat.PawnHasAnyMasteredWeapon(pawn))) return;
                // Trying to strip a dead pawn
                if (pawn.Dead) return;
                pawn.needs.mood.thoughts.memories.TryGainMemory(Core.MasteredWeaponUnequipped);
            }
        }

        public void SetLevel(Pawn pawn, int level)
        {
            bonusStatsPerPawn[pawn].SetMasteryLevel(level);
        }
    }
}
