using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using SK_WeaponMastery.Compat;

namespace SK_WeaponMastery
{
    // Mod core logic
    public static class Core
    {
        private static bool writeLock;
        public static ThoughtDef MasteredWeaponUnequipped;

        // Add mastery experience to shooter pawn
        public static void OnPawnShoot(Verb_Shoot __instance)
        {
            Pawn pawn = __instance.CurrentTarget.Thing as Pawn;
            if (pawn != null && !pawn.Downed && __instance.CasterIsPawn && __instance.CasterPawn.skills != null)
            {
                float num = (pawn.HostileTo(__instance.caster) ? 170f : 20f);
                float num2 = __instance.verbProps.AdjustedFullCycleTime(__instance, __instance.CasterPawn);
                Pawn caster = __instance.Caster as Pawn;
                Thing weapon = GetPawnWeapon(caster);
                if (weapon == null) return;
                MasteryWeaponComp comp = weapon.TryGetComp<MasteryWeaponComp>();
                if (comp == null) return;
                comp.SetCurrentOwner(caster);
                if (ModSettings.useSpecificMasterySystem)
                {
                    if (!comp.IsActive()) comp.Init();
                    comp.AddExp(caster, (int)(num * num2));
                }
                if (!ModSettings.useGeneralMasterySystem) return;
                MasteryPawnComp compPawn = caster.TryGetComp<MasteryPawnComp>();
                if (compPawn == null) return;
                if (!compPawn.IsActive()) compPawn.Init();
                if (ModSettings.classes.ContainsKey(weapon.def))
                    compPawn.AddExp(ModSettings.classes[weapon.def], (int)(num * num2), weapon.def.IsMeleeWeapon);
            }
        }

        // Add mastery experience to melee pawn
        public static void OnPawnMelee(Verb_MeleeAttack __instance, LocalTargetInfo ___currentTarget)
        {
            Pawn casterPawn = __instance.CasterPawn;
            Thing thing = ___currentTarget.Thing;
            Pawn pawn = thing as Pawn;
            if ((!(thing.def.category != ThingCategory.Pawn || pawn.Downed || pawn.GetPosture() > PawnPosture.Standing)) && casterPawn.skills != null)
            {
                float exp = 200f * __instance.verbProps.AdjustedFullCycleTime(__instance, casterPawn);
                Thing weapon = GetPawnWeapon(casterPawn);
                if (weapon == null) return;
                MasteryWeaponComp comp = weapon.TryGetComp<MasteryWeaponComp>();
                if (comp == null) return;
                comp.SetCurrentOwner(casterPawn);
                if (ModSettings.useSpecificMasterySystem)
                {
                    if (!comp.IsActive()) comp.Init();
                    comp.AddExp(casterPawn, (int)exp);
                }
                if (!ModSettings.useGeneralMasterySystem) return;
                MasteryPawnComp compPawn = casterPawn.TryGetComp<MasteryPawnComp>();
                if (compPawn == null) return;
                if (!compPawn.IsActive()) compPawn.Init();
                if (ModSettings.classes.ContainsKey(weapon.def))
                    compPawn.AddExp(ModSettings.classes[weapon.def], (int)exp, weapon.def.IsMeleeWeapon);
            }
        }

        // Add custom stat part from my mod into base game stat defs
        // This should be used to influence stat calculation
        public static void InjectStatPartIntoStatDefs()
        {
            List<StatDef> injected = new List<StatDef>();
            for (int i = 0; i < ModSettings.rangedStats.Count; i++)
            {
                MasteryStat mStat = ModSettings.rangedStats[i];
                StatDef stat = mStat.GetStat();
                injected.Add(stat);
                if (stat.parts == null) stat.parts = new List<StatPart>();
                stat.parts.Add(new StatPart_Mastery(stat));
            }
            for (int i = 0; i < ModSettings.meleeStats.Count; i++)
            {
                MasteryStat mStat = ModSettings.meleeStats[i];
                StatDef stat = mStat.GetStat();
                if (injected.Contains(stat)) continue;
                injected.Add(stat);
                if (stat.parts == null) stat.parts = new List<StatPart>();
                stat.parts.Add(new StatPart_Mastery(stat));
            }
        }

        // Remove custom stat part from my mod from base game stat defs
        // Used for reloding configs
        public static void RemoveStatPartFromStatDefs()
        {
            for (int i = 0; i < ModSettings.rangedStats.Count; i++)
            {
                MasteryStat mStat = ModSettings.rangedStats[i];
                StatDef stat = mStat.GetStat();
                if (stat.parts.Any((StatPart item) => item is StatPart_Mastery))
                    stat.parts = stat.parts.Where((StatPart item) => !(item is StatPart_Mastery)).ToList();
            }
            for (int i = 0; i < ModSettings.meleeStats.Count; i++)
            {
                MasteryStat mStat = ModSettings.meleeStats[i];
                StatDef stat = mStat.GetStat();
                if (stat.parts.Any((StatPart item) => item is StatPart_Mastery))
                    stat.parts = stat.parts.Where((StatPart item) => !(item is StatPart_Mastery)).ToList();
            }
        }

        public static void AddMasteryWeaponCompToWeaponDefsAndSetClasses()
        {
            CompProperties compProperties = new Verse.CompProperties { compClass = typeof(MasteryWeaponComp) };
            IEnumerable<ThingDef> defs = GetModWeapons();

            foreach (ThingDef def in defs)
            {
                DefModExtension_Mastery ext = def.GetModExtension<DefModExtension_Mastery>();
                def.comps.Add(compProperties);
                if (ext == null) continue;
                ModSettings.classes[def] = ext.gmclass;
            }
        }

        public static void AddMasteryPawnCompToHumanoidDefs()
        {
            // Ensure that def is a weapon
            // Filter bad weapon defs suchs wood logs, thrumbo horns, etc...
            bool Predicate(ThingDef def)
            {
                return def.race != null && def.race.Humanlike;
            }

            CompProperties compProperties = new Verse.CompProperties { compClass = typeof(MasteryPawnComp) };
            IEnumerable<ThingDef> defs = DefDatabase<ThingDef>.AllDefs.Where(Predicate).ToList();

            foreach (ThingDef def in defs)
                def.comps.Add(compProperties);
        }

        public static void OnModWriteSettings(Mod __instance)
        {
            if (__instance.Content.Name != WeaponMasteryMod.modName || writeLock) return;
            writeLock = true;

            if (ModSettingsWindow.isOpen)
            {
                ModSettingsWindow.Destroy();
                RemoveStatPartFromStatDefs();
                InjectStatPartIntoStatDefs();
            }
        }

        // Add mastery levels to raid
        public static void OnRaid(IncidentWorker_Raid __instance, IncidentParms parms, List<Pawn> pawns, bool debugTest, ref bool __result)
        {
            if (!__result) return;
            GenerateMasteriesForPawns(pawns);
        }

        // Add mastery levels to neutral pawns. Trade caravans, visitors
        // etc ...
        public static void OnNeutralPawnSpawn(IncidentWorker_NeutralGroup __instance, IncidentParms parms, ref List<Pawn> __result)
        {
            GenerateMasteriesForPawns(__result);
        }

        // Generate weapon masteries on a list of pawns
        private static void GenerateMasteriesForPawns(List<Pawn> pawns)
        {
            if (pawns == null) return;
            List<Pawn> clonedReferences = new List<Pawn>(pawns);
            int masteryOwnersCount = (int)Math.Ceiling(clonedReferences.Count * ModSettings.masteriesPercentagePerEvent);
            int currentOwnersCount = 0;
            while (clonedReferences.Count != 0 && currentOwnersCount < masteryOwnersCount)
            {
                Pawn selectedPawn = clonedReferences.RandomElement();
                bool bonusAdded = false;
                // Is Pawn Humanoid and has weapon
                if (selectedPawn.RaceProps.Humanlike && selectedPawn.equipment?.Primary != null)
                {
                    if (ModSettings.useSpecificMasterySystem)
                    {
                        MasteryWeaponComp comp = selectedPawn.equipment.Primary.TryGetComp<MasteryWeaponComp>();
                        if (comp != null)
                        {
                            // Roll stats and weapon name
                            comp.SetCurrentOwner(selectedPawn);
                            comp.Init();
                            int statsCount = Rand.RangeInclusive(1, ModSettings.maxLevel);
                            for (int i = 0; i < statsCount; i++)
                            {
                                MasteryStat stat = ModSettings.PickBonus(selectedPawn.equipment.Primary.def.IsMeleeWeapon);
                                if (stat != null)
                                    comp.AddStatBonus(selectedPawn, stat.GetStat(), stat.GetOffset());
                            }
                            comp.SetLevel(selectedPawn, statsCount);
                            float weaponNameRoll = Rand.Value;
                            if (ModSettings.eventWeaponNameChance > 0 && weaponNameRoll <= ModSettings.eventWeaponNameChance) comp.GenerateWeaponName();
                            comp.GenerateDescription();
                            bonusAdded = true;
                        }
                    }
                    if (ModSettings.useGeneralMasterySystem)
                    {
                        MasteryPawnComp comp = selectedPawn.TryGetComp<MasteryPawnComp>();
                        if (comp != null && ModSettings.classes.ContainsKey(selectedPawn.equipment.Primary.def))
                        {
                            comp.Init();
                            int statsCount = Rand.RangeInclusive(1, ModSettings.maxLevel);
                            for (int i = 0; i < statsCount; i++)
                            {
                                MasteryStat stat = ModSettings.PickBonus(selectedPawn.equipment.Primary.def.IsMeleeWeapon);
                                if (stat != null)
                                    comp.AddStatBonus(ModSettings.classes[selectedPawn.equipment.Primary.def], stat.GetStat(), stat.GetOffset());
                            }
                            comp.SetLevel(ModSettings.classes[selectedPawn.equipment.Primary.def], statsCount);
                            comp.GenerateDescription();
                            bonusAdded = true;
                        }
                    }
                }
                if (bonusAdded) currentOwnersCount++;
                clonedReferences.Remove(selectedPawn);
            }
        }

        // Display general weapon mastery description in pawn's information
        // tab
        public static void AddMasteryDescriptionToDrawStats(ThingDef parentDef, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn == null) return;
            IEnumerable<StatDrawEntry> NewFunc(IEnumerable<StatDrawEntry> functionOutput)
            {
                foreach (StatDrawEntry item in functionOutput) yield return item;
                if (pawn.def.race.Humanlike)
                {
                    MasteryPawnComp comp = pawn.TryGetComp<MasteryPawnComp>();
                    if (comp != null && comp.IsActive())
                        yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "SK_WeaponMastery_StatWeaponMastery".Translate(), "", comp.GetDescription(), 20);
                }
            }
            __result = NewFunc(__result);
        }

        // On info window opening check if it's one of our comps and regenerate
        // description. This is used to update experience and levels in the
        // comp's description.
        public static void OnInfoWindowSetup(Dialog_InfoCard __instance, Thing ___thing)
        {
            if (___thing == null) return;
            MasteryWeaponComp weaponComp = ___thing.TryGetComp<MasteryWeaponComp>();
            if (weaponComp != null && weaponComp.IsActive())
            {
                weaponComp.GenerateDescription();
                return;
            }
            MasteryPawnComp pawnComp = ___thing.TryGetComp<MasteryPawnComp>();
            if (pawnComp != null && pawnComp.IsActive())
                pawnComp.GenerateDescription();
        }

        // Get pawn's weapon. Takes into account dual wielding
        public static Thing GetPawnWeapon(Pawn pawn)
        {
            if (DualWieldCompat.enabled && DualWieldCompat.isCurrentAttackOffhand)
                return DualWieldCompat.GetOffhandWeapon(pawn);
            return pawn?.equipment?.Primary;
        }

        // Let settings override patches
        public static void OverrideClasses()
        {
            if (ModSettings.overrideClasses == null)
            {
                ModSettings.overrideClasses = new Dictionary<string, string>();
                return;
            }
            List<string> keysToRemove = new List<string>();
            foreach (KeyValuePair<string, string> item in ModSettings.overrideClasses)
            {
                ThingDef weapon = DefDatabase<ThingDef>.AllDefsListForReading.Find((ThingDef def) => def.defName == item.Key);
                if (weapon == null)
                {
                    keysToRemove.Add(item.Key);
                    continue;
                }
                ModSettings.classes[weapon] = item.Value;
            }
            foreach (string key in keysToRemove)
                ModSettings.overrideClasses.Remove(key);
        }

        // Returns all weapons that are compatible with mod
        public static IEnumerable<ThingDef> GetModWeapons()
        {
            // Ensure that def is a weapon
            // Filter bad weapon defs suchs wood logs, thrumbo horns, etc...
            bool Predicate(ThingDef def)
            {
                DefModExtension_Mastery ext = def.GetModExtension<DefModExtension_Mastery>();
                if (ext != null && ext.blacklisted)
                    return false;
                return def.IsWeapon && def.HasComp(typeof(CompQuality));
            }
            return DefDatabase<ThingDef>.AllDefs.Where(Predicate).ToList();
        }

        public static IEnumerable<Gizmo> DisplayAdditionalPawnGizmos(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (var value in values)
                yield return value;
            if (DebugSettings.ShowDevGizmos)
            {
                if (__instance.equipment == null || __instance.equipment.Primary == null)
                {
                    yield break;
                }
                MasteryWeaponComp comp = __instance.equipment.Primary.TryGetComp<MasteryWeaponComp>();
                if (comp != null && comp.IsActive())
                {
                    yield return new Command_Action()
                    {
                        defaultLabel = "Reset Weapon Name",
                        icon = null,
                        action = () =>
                        {
                            comp.ResetWeaponName();
                        }
                    };
                }
            }
        }
    }
}
