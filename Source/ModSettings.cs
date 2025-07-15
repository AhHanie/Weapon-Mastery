using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.IO;
using Verse;

namespace SK_WeaponMastery
{
    /*
     * ModSettings are loaded using GetSettings if there's a file.
     * ModSettings DO NOT load the first time because there's no settings
     * file initially. Thus ExposeData() doesn't run when the game loads.
     * ModSettings are saved when the player closes the modsettings window.
     * You could also trigger a manual save by calling ModSettings.Write()
     */
    public class ModSettings : Verse.ModSettings
    {
        public static List<int> experiencePerLevel;
        public static int maxLevel = 3;
        public static bool initialLoad = false;
        public static bool useCustomNames = false;
        public static bool masteryOnOutsidePawns = true;
        public static bool useMoods = false;
        public static bool useSpecificMasterySystem = true;
        public static bool useGeneralMasterySystem = false;
        public static bool displayExperience = false;
        public static bool KeepOriginalWeaponNameQuality = false;
        public static float chanceToNameWeapon = 0.35f;
        public static float bondedWeaponExperienceMultipier = 1.5f;
        public static float masteriesPercentagePerEvent = 0.25f;
        public static float eventWeaponNameChance = 0.15f;
        public static int numberOfRelicBonusStats = 5;
        public static Dictionary<string, string> overrideClasses = new Dictionary<string, string>();
        public static Dictionary<ThingDef, string> classes = new Dictionary<ThingDef, string>();
        public static List<MasteryStat> rangedStats;
        public static List<MasteryStat> meleeStats;
        public static List<string> weaponNamesPool;
        public static List<string> customWeaponNamesPool = new List<string>();
        public static List<string> messages;
        private static readonly string WEAPON_NAMES_DEF_PATH = System.IO.Path.Combine("Languages", "English", "WeaponNamesList.txt");
        private static readonly string MOD_PACKAGE_ID = "Sk.WeaponMastery";

        public override void ExposeData()
        {
            initialLoad = true;
            base.ExposeData();
            Scribe_Collections.Look(ref experiencePerLevel, "experienceperlevel", LookMode.Value);
            Scribe_Collections.Look(ref rangedStats, "rangedstats", true, LookMode.Deep);
            Scribe_Collections.Look(ref meleeStats, "meleestats", true, LookMode.Deep);
            Scribe_Values.Look(ref maxLevel, "maxLevel", 3);
            Scribe_Values.Look(ref chanceToNameWeapon, "chancetonameweapon", 0.35f);
            Scribe_Values.Look(ref bondedWeaponExperienceMultipier, "bondedweaponexperiencemultipier", 1.5f);
            Scribe_Values.Look(ref numberOfRelicBonusStats, "numberofrelicbonusstats", 5);
            Scribe_Values.Look(ref useCustomNames, "usecustomnames", false);
            Scribe_Collections.Look(ref customWeaponNamesPool, "customweaponnamespool", LookMode.Value);
            Scribe_Values.Look(ref masteriesPercentagePerEvent, "masteriespercentageperevent", 0.25f);
            Scribe_Values.Look(ref eventWeaponNameChance, "eventweaponnamechance", 0.15f);
            Scribe_Values.Look(ref masteryOnOutsidePawns, "masteryonoutsidepawns", true);
            Scribe_Values.Look(ref useMoods, "usemoods", false);
            Scribe_Values.Look(ref useSpecificMasterySystem, "usespecificmasterysystem", true);
            Scribe_Values.Look(ref useGeneralMasterySystem, "usegeneralmasterysystem", false);
            Scribe_Values.Look(ref displayExperience, "displayexperience", false);
            Scribe_Values.Look(ref KeepOriginalWeaponNameQuality, "displayweaponoriginalnamequality", false);
            Scribe_Collections.Look(ref overrideClasses, "overrideclasses");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && customWeaponNamesPool == null) customWeaponNamesPool = new List<string>();
        }

        // Set default settings
        public static void SetSensibleDefaults()
        {
            // Default ExperiencePerLevel
            experiencePerLevel = new List<int>();
            experiencePerLevel.Add(15000);
            experiencePerLevel.Add(20000);
            experiencePerLevel.Add(25000);

            // Default Ranged Stats
            rangedStats = new List<MasteryStat>();
            rangedStats.Add(new MasteryStat(StatDefOf.ShootingAccuracyPawn, 0.01f));
            rangedStats.Add(new MasteryStat(StatDefOf.MoveSpeed, 0.2f));
            rangedStats.Add(new MasteryStat(StatDefOf.HuntingStealth, 0.01f));
            rangedStats.Add(new MasteryStat(StatDefOf.RangedWeapon_Cooldown, -0.1f));
            rangedStats.Add(new MasteryStat(StatDefOf.AimingDelayFactor, -0.02f));

            // Default Melee Stats
            meleeStats = new List<MasteryStat>();
            meleeStats.Add(new MasteryStat(StatDefOf.MoveSpeed, 0.2f));
            meleeStats.Add(new MasteryStat(StatDefOf.MeleeDodgeChance, 0.01f));
            meleeStats.Add(new MasteryStat(StatDefOf.MeleeHitChance, 0.01f));
            meleeStats.Add(new MasteryStat(StatDefOf.CarryingCapacity, 10f));
            if (ModsConfig.IdeologyActive)
                meleeStats.Add(new MasteryStat(StatDefOf.SuppressionPower, 0.01f));
        }

        // Find MasteryStat containing the same StatDef parameter
        public static MasteryStat FindStatWithStatDef(StatDef stat, bool melee)
        {
            List<MasteryStat> result;
            if (!melee)
                result = rangedStats.Where((MasteryStat item) => item.GetStat() == stat).ToList();
            else
                result = meleeStats.Where((MasteryStat item) => item.GetStat() == stat).ToList();
            return result.Count > 0 ? result.First() : null;
        }

        public static void RemoveStat(MasteryStat stat, bool melee)
        {
            if (!melee)
                rangedStats.Remove(stat);
            else
                meleeStats.Remove(stat);
        }

        public static void AddStat(MasteryStat stat, bool melee)
        {
            if (!melee)
                rangedStats.Add(stat);
            else
                meleeStats.Add(stat);
        }

        // Convert string defnames to StatDefs and remove ones that didn't load
        public static void ResolveStats()
        {
            for (int i = 0; i < rangedStats.Count; i++)
            {
                MasteryStat item = rangedStats[i];
                item.Resolve();
            }
            for (int i = 0; i < meleeStats.Count; i++)
            {
                MasteryStat item = meleeStats[i];
                item.Resolve();
            }
            rangedStats = rangedStats.Where((MasteryStat item) => item.GetStat() != null).ToList();
            meleeStats = meleeStats.Where((MasteryStat item) => item.GetStat() != null).ToList();
        }

        // Get a random MasteryStat
        public static MasteryStat PickBonus(bool melee)
        {
            return melee ? meleeStats.RandomElement() : rangedStats.RandomElement();
        }

        public static int GetExperienceForLevel(int level)
        {
            return experiencePerLevel[level];
        }

        public static void LoadWeaponNames()
        {
            // Loop on all mods and find any mods that loadafter or 
            // depend on my mod as they might be translation mods
            List<string> dependecyModsRootDirectories = new List<string>();
            foreach (ModContentPack mod in LoadedModManager.RunningMods)
            {
                ModMetaData meta = mod.ModMetaData;
                if (meta.LoadAfter != null)
                {
                    foreach (string package in meta.LoadAfter)
                    {
                        if (package == MOD_PACKAGE_ID)
                        {
                            dependecyModsRootDirectories.Add(mod.RootDir);
                            break;
                        }
                    }
                }
                if (meta.Dependencies != null)
                {
                    foreach (ModDependency dependency in meta.Dependencies)
                    {
                        if (dependency.packageId == MOD_PACKAGE_ID)
                        {
                            dependecyModsRootDirectories.Add(mod.RootDir);
                            break;
                        }
                    }
                }
            }
            // Check if they actually have a language folder for the 
            // current language
            foreach (string path in dependecyModsRootDirectories)
            {
                string[] namesFromLegacyFolder = Files.GetLinesFromTextFile(System.IO.Path.Combine(path, "Languages", LanguageDatabase.activeLanguage.LegacyFolderName, "WeaponNamesList.txt"), false);
                string[] namesFromCurrentFolder = Files.GetLinesFromTextFile(System.IO.Path.Combine(path, "Languages", LanguageDatabase.activeLanguage.folderName, "WeaponNamesList.txt"), false);
                if (namesFromCurrentFolder != null)
                    weaponNamesPool = new List<string>(namesFromCurrentFolder);
                else if (namesFromLegacyFolder != null)
                    weaponNamesPool = new List<string>(namesFromLegacyFolder);
            }
            // Default case
            if (weaponNamesPool == null)
            {
                string[] defaultWeaponNames = Files.GetLinesFromTextFile(WEAPON_NAMES_DEF_PATH, true);
                if (defaultWeaponNames != null)
                    weaponNamesPool = new List<string>(defaultWeaponNames);
                else
                {
                    weaponNamesPool = new List<string>();
                    weaponNamesPool.Add("Missing Names");
                }
            }

        }

        // Message Language Keys
        public static void InitMessageKeys()
        {
            messages = new List<string>();
            string key = "SK_WeaponMastery_Message";
            for (int i = 0; i < 4; i++)
            {
                messages.Add(key + (i + 1).ToString());
            }
        }

        public static string PickWeaponName()
        {
            if (ModSettings.useCustomNames && ModSettings.customWeaponNamesPool.Count > 0)
                return ModSettings.customWeaponNamesPool.RandomElement();
            else
                return ModSettings.weaponNamesPool.RandomElement();
        }
    }
}
