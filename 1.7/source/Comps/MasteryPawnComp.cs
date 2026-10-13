using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SK_WeaponMastery
{
    class MasteryPawnComp : ThingComp
    {
        private Dictionary<string, MasteryCompData> bonusStatsPerClass;
        private bool isActive = false;
        private string masteryDescription;

        // These variables are needed for loading data from save file in PostExposeData()
        List<string> dictKeysAsList;
        List<MasteryCompData> dictValuesAsList;

        public void Init()
        {
            bonusStatsPerClass = new Dictionary<string, MasteryCompData>();
            isActive = true;
        }

        // Add new stat bonus for a weaoon
        public void AddStatBonus(string weaponClass, StatDef bonusStat, float value)
        {
            if (!bonusStatsPerClass.ContainsKey(weaponClass))
                bonusStatsPerClass[weaponClass] = new MasteryCompData();

            bonusStatsPerClass[weaponClass].AddStatBonus(bonusStat, value);
        }

        // Check if comp was init
        public bool IsActive()
        {
            return isActive;
        }

        // Get stat bonus for current weapon
        public float GetStatBonus(string weaponClass, StatDef stat)
        {
            if (!bonusStatsPerClass.ContainsKey(weaponClass))
                return 0;
            return bonusStatsPerClass[weaponClass].GetStatBonus(stat);
        }

        public void AddExp(string weaponClass, int experience, bool isMelee)
        {
            if (!bonusStatsPerClass.ContainsKey(weaponClass))
                bonusStatsPerClass[weaponClass] = new MasteryCompData();

            float multiplier = 1;
            if (ModsConfig.RoyaltyActive && IsBondedWeapon()) multiplier = ModSettings.bondedWeaponExperienceMultipier;

            bonusStatsPerClass[weaponClass].AddExp((int)(experience * multiplier), isMelee, delegate (int level)
            {
                // Update cached description when pawn levels up
                GenerateDescription();
            });
        }

        // Check if weapon is bonded to current owner
        private bool IsBondedWeapon()
        {
            Pawn parent = this.parent as Pawn;
            CompBladelinkWeapon link = parent?.equipment?.Primary?.TryGetComp<CompBladelinkWeapon>();
            return link?.CodedPawn == parent;
        }

        // Save/Load comp data to/from rws file
        public override void PostExposeData()
        {
            base.PostExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Saving process, allow only active comps to be saved so
                // we don't fill save file with null comps
                if (isActive)
                {
                    List<string> weapons = bonusStatsPerClass.Keys.ToList();
                    List<MasteryCompData> data = bonusStatsPerClass.Values.ToList();
                    if (bonusStatsPerClass != null)
                        Scribe_Collections.Look(ref bonusStatsPerClass, "bonusstatsperclass", LookMode.Value, LookMode.Deep, ref weapons, ref data);
                    Scribe_Values.Look(ref isActive, "isactive");
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                // Loading process
                Scribe_Values.Look(ref isActive, "isactive", false);
                if (isActive)
                    Scribe_Collections.Look(ref bonusStatsPerClass, "bonusstatsperclass", LookMode.Value, LookMode.Deep, ref dictKeysAsList, ref dictValuesAsList);
            }
            else if (Scribe.mode == LoadSaveMode.PostLoadInit && isActive)
            {
                if (bonusStatsPerClass == null)
                {
                    bonusStatsPerClass = new Dictionary<string, MasteryCompData>();
                    return;
                }
                if (AnyWeaponHasMastery()) GenerateDescription();
            }
        }

        public void GenerateDescription()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("SK_WeaponMastery_StatWeaponMastery".Translate());
            List<KeyValuePair<string, MasteryCompData>> data = bonusStatsPerClass.ToList();
            string positiveValueColumn = ": +";
            string negativeValueColumn = ": ";
            for (int i = 0; i < data.Count; i++)
            {
                KeyValuePair<string, MasteryCompData> item = data[i];
                sb.AppendLine();
                sb.AppendLine($"{Utils.Capitalize(item.Key)}: ");
                foreach (KeyValuePair<StatDef, float> statbonus in item.Value.GetStatBonusesAsList())
                    sb.AppendLine(" " + statbonus.Key.label.CapitalizeFirst() + (statbonus.Value >= 0f ? positiveValueColumn : negativeValueColumn) + statbonus.Key.ValueToString(statbonus.Value));
                if (ModSettings.displayExperience && !item.Value.IsMaxLevel()) sb.AppendLine("Level: " + item.Value.GetMasteryLevel() + "  Experience: " + item.Value.GetExperience() + "/" + ModSettings.GetExperienceForLevel(item.Value.GetMasteryLevel()));
            }
            sb.AppendLine();
            masteryDescription = sb.ToString();
        }

        private bool AnyWeaponHasMastery()
        {
            List<KeyValuePair<string, MasteryCompData>> data = bonusStatsPerClass.ToList();
            for (int i = 0; i < data.Count; i++)
            {
                KeyValuePair<string, MasteryCompData> item = data[i];
                if (item.Value.HasMastery()) return true;
            }
            return false;
        }

        public string GetDescription()
        {
            if (!isActive || (!AnyWeaponHasMastery() && !ModSettings.displayExperience)) return base.GetDescriptionPart();
            return masteryDescription;
        }

        public void SetLevel(string weaponClass, int level)
        {
            bonusStatsPerClass[weaponClass].SetMasteryLevel(level);
        }
    }
}
