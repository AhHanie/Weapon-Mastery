﻿using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace SK_WeaponMastery
{
    // Main mod file
    public class WeaponMasteryMod : Mod
    {
        public static bool SHOULD_PRINT_LOG = false;
        // Mod name in about.xml
        public static string modName;
        private static string rootDirectory;
        public WeaponMasteryMod(ModContentPack content) : base(content)
        {
            rootDirectory = this.Content.RootDir;
            modName = this.Content.Name;
            Harmony instance = new Harmony("rimworld.sk.weaponmastery");
            HarmonyPatcher.SetInstance(instance);

            // Fires when all Defs are loaded
            LongEventHandler.ExecuteWhenFinished(Init);
        }

        public void Init()
        {
            GetSettings<ModSettings>();
            if (!ModSettings.initialLoad)
                ModSettings.SetSensibleDefaults();
            else
                ModSettings.ResolveStats();
            Core.MasteredWeaponUnequipped = DefDatabase<ThoughtDef>.AllDefsListForReading.Find((ThoughtDef def) => def.defName == "SK_WM_MasteredWeaponUnequipped");
            Core.MasteredWeaponEquipped = DefDatabase<HediffDef>.AllDefsListForReading.Find((HediffDef def) => def.defName == "SK_WM_MasteredWeaponEquippedBonusMood");
            Core.AddMasteryWeaponCompToWeaponDefs();
            if (ModSettings.useGeneralMasterySystem)
                Core.AddMasteryPawnCompToHumanoidDefs();
            Core.InjectStatPartIntoStatDefs();
            ModSettings.LoadWeaponNames();
            ModSettings.InitMessageKeys();
            HarmonyPatcher.PatchVanillaMethods();
            // DualWield Compatibility class
            SK_WeaponMastery.Compat.DualWieldCompat.Init();
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            ModSettingsWindow.Draw(rect);
            base.DoSettingsWindowContents(rect);
        }

        public override string SettingsCategory()
        {
            return "SK_WeaponMastery_SettingsListItemName".Translate();
        }

        public static string GetRootDirectory()
        {
            return rootDirectory;
        }
    }
}
