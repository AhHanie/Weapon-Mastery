using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SK_WeaponMastery
{
    // Responsible for drawing modsettings ui window and updating modsettings  
    public static class ModSettingsWindow
    {
        public static int selectedLevelIndex;
        public static int selectedLevelValue;
        private static int selectedWeaponIndex;
        private static int selectedClassIndex;
        private static string selectedLevelStringBuffer;
        private static string customNameInputStringBuffer;
        private static string addClassInputStringBuffer;
        private static string removeClassInputStringBuffer;
        private static string selectedCustomName;
        private static ModSettingsWindowThread thread;
        private static List<StatDef> allStatDefs;
        private static List<ThingDef> allWeapons;
        private static List<string> allClasses;
        private static StatDef selectedRangedDef;
        private static StatDef selectedMeleeDef;
        public static MasteryStat selectedRangedMasteryStat;
        public static MasteryStat selectedMeleeMasteryStat;
        private static Texture2D cachedWeaponTexture;
        public static float rangedStatOffset;
        public static float meleeStatOffset;
        public static bool isRangedStatEnabled;
        public static bool isMeleeStatEnabled;
        public static bool isRangedStatPercentage;
        public static bool isMeleeStatPercentage;
        public static bool isOpen = false;
        public static bool initSemaphore = false;
        private static Vector2 scrollPosition = Vector2.zero;
        public const int MAX_LEVELS = 100;
        private readonly static int DEFAULT_EXP = 15000;
        private readonly static float DEFAULT_STAT_OFFSET = 1f;
        private readonly static float MIN_STAT_BONUS = -5f;
        private readonly static float MAX_STAT_BONUS = 5;
        private readonly static float MIN_BONDED_WEAPON_MULTIPLIER = 1f;
        private readonly static float MAX_BONDED_WEAPON_MULTIPLIER = 3f;
        private readonly static int MIN_RELIC_BONUS_STATS = 1;
        private readonly static int MAX_RELIC_BONUS_STATS = 10;

        public static void Init()
        {
            if (initSemaphore) return;
            initSemaphore = true;
            isOpen = true;

            selectedLevelIndex = 0;
            selectedWeaponIndex = 0;
            selectedClassIndex = 0;
            selectedLevelValue = ModSettings.experiencePerLevel[0];
            allStatDefs = DefDatabase<StatDef>.AllDefs.ToList();
            selectedRangedDef = allStatDefs[0];
            selectedMeleeDef = allStatDefs[0];
            isRangedStatPercentage = IsStatDefPercentage(selectedRangedDef);
            isMeleeStatPercentage = IsStatDefPercentage(selectedMeleeDef);
            allWeapons = Core.GetModWeapons().ToList();
            cachedWeaponTexture = ContentFinder<Texture2D>.Get(allWeapons[selectedWeaponIndex].graphicData.texPath, true);
            allClasses = new List<string>();
            foreach (string gmclass in ModSettings.classes.Values)
                if (!allClasses.Contains(gmclass))
                    allClasses.Add(gmclass);


            selectedRangedMasteryStat = ModSettings.FindStatWithStatDef(allStatDefs[0], false);
            if (selectedRangedMasteryStat != null)
            {
                rangedStatOffset = selectedRangedMasteryStat.GetOffset();
                isRangedStatEnabled = true;
            }
            else
            {
                rangedStatOffset = DEFAULT_STAT_OFFSET;
                isRangedStatEnabled = false;
            }

            selectedMeleeMasteryStat = ModSettings.FindStatWithStatDef(allStatDefs[0], true);
            if (selectedMeleeMasteryStat != null)
            {
                meleeStatOffset = selectedMeleeMasteryStat.GetOffset();
                isMeleeStatEnabled = true;
            }
            else
            {
                meleeStatOffset = DEFAULT_STAT_OFFSET;
                isMeleeStatEnabled = false;
            }

            if (ModSettings.customWeaponNamesPool.Count > 0) selectedCustomName = ModSettings.customWeaponNamesPool[0];

            thread = new ModSettingsWindowThread(selectedLevelValue, rangedStatOffset, meleeStatOffset);
            new System.Threading.Thread(new System.Threading.ThreadStart(thread.Run)).Start();
            initSemaphore = false;
        }
        public static void Draw(Rect parent)
        {
            if (!isOpen)
                Init();

            if (initSemaphore) return;

            parent.yMin += 15f;
            parent.yMax -= 15f;
            Listing_Standard list = new Listing_Standard(GameFont.Medium);

            Rect outerRect = new Rect(parent.x, parent.y + 20, parent.width, parent.height - 20);
            Rect innerRect = new Rect(outerRect);
            innerRect.x += 5;
            innerRect.width -= 10;
            innerRect.y += 10;
            innerRect.height -= 20;
            Widgets.DrawBox(outerRect, 1, Texture2D.whiteTexture);
            Rect scrollRect = new Rect(0f, innerRect.y, innerRect.width - 20f, parent.height * 2f + (ModSettings.useCustomNames ? 100 : 0));
            Widgets.DrawMenuSection(outerRect);
            Widgets.BeginScrollView(innerRect, ref scrollPosition, scrollRect, true);
            list.Begin(scrollRect);

            DrawModOptionsSection(list);
            DrawExperiencePerLevelSection(list);
            DrawStatsSection(list);
            DrawCustomNamesSection(list);
            DrawGeneralMasterySection(list);

            list.End();
            Widgets.EndScrollView();
        }

        private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateLevelOptions()
        {
            for (int i = 0; i < ModSettings.maxLevel; i++)
            {
                int localCopyOfI = i;
                yield return new Widgets.DropdownMenuElement<string>
                {
                    option = new FloatMenuOption("SK_WeaponMastery_LevelSectionDropdownOptionLabel".Translate(localCopyOfI + 1), delegate ()
                    {
                        selectedLevelIndex = localCopyOfI;
                        selectedLevelValue = ModSettings.experiencePerLevel[localCopyOfI];
                        selectedLevelStringBuffer = selectedLevelValue.ToString();
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = "SK_WeaponMastery_LevelSectionLevelExpLabel".Translate(localCopyOfI + 1)
                };
            }
        }

        private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateRangedStatsOptions()
        {
            for (int i = 0; i < allStatDefs.Count; i++)
            {
                int localCopyOfI = i;

                yield return new Widgets.DropdownMenuElement<string>
                {
                    option = new FloatMenuOption(allStatDefs[localCopyOfI].defName, delegate ()
                    {
                        selectedRangedDef = allStatDefs[localCopyOfI];
                        isRangedStatPercentage = IsStatDefPercentage(selectedRangedDef);
                        MasteryStat item = ModSettings.FindStatWithStatDef(allStatDefs[localCopyOfI], false);
                        if (item != null)
                        {
                            selectedRangedMasteryStat = item;
                            rangedStatOffset = item.GetOffset();
                            isRangedStatEnabled = true;
                        }
                        else
                            isRangedStatEnabled = false;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = allStatDefs[localCopyOfI].defName
                };
            }
        }

        private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateMeleeStatsOptions()
        {
            for (int i = 0; i < allStatDefs.Count; i++)
            {
                int localCopyOfI = i;

                yield return new Widgets.DropdownMenuElement<string>
                {
                    option = new FloatMenuOption(allStatDefs[localCopyOfI].defName, delegate ()
                    {
                        selectedMeleeDef = allStatDefs[localCopyOfI];
                        isMeleeStatPercentage = IsStatDefPercentage(selectedMeleeDef);
                        MasteryStat item = ModSettings.FindStatWithStatDef(allStatDefs[localCopyOfI], true);
                        if (item != null)
                        {
                            selectedMeleeMasteryStat = item;
                            meleeStatOffset = item.GetOffset();
                            isMeleeStatEnabled = true;
                        }
                        else
                            isMeleeStatEnabled = false;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = allStatDefs[localCopyOfI].defName
                };
            }
        }

        private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateCustomNameOptions()
        {
            for (int i = 0; i < ModSettings.customWeaponNamesPool.Count; i++)
            {
                int localCopyOfI = i;

                yield return new Widgets.DropdownMenuElement<string>
                {
                    option = new FloatMenuOption(ModSettings.customWeaponNamesPool[localCopyOfI], delegate ()
                    {
                        selectedCustomName = ModSettings.customWeaponNamesPool[localCopyOfI];
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = ModSettings.customWeaponNamesPool[localCopyOfI]
                };
            }
        }

        private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateWeaponOptions()
        {
            for (int i = 0; i < allWeapons.Count; i++)
            {
                int localCopyOfI = i;

                yield return new Widgets.DropdownMenuElement<string>
                {
                    option = new FloatMenuOption(allWeapons[localCopyOfI].defName, delegate ()
                    {
                        selectedWeaponIndex = localCopyOfI;
                        cachedWeaponTexture = ContentFinder<Texture2D>.Get(allWeapons[localCopyOfI].graphicData.texPath, true);
                    },  MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = allWeapons[localCopyOfI].LabelCap
                };
            }
        }

        private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateClassOptions()
        {
            for (int i = 0; i < allClasses.Count; i++)
            {
                int localCopyOfI = i;

                yield return new Widgets.DropdownMenuElement<string>
                {
                    option = new FloatMenuOption(allClasses[localCopyOfI], delegate ()
                    {
                        selectedClassIndex = localCopyOfI;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = allClasses[localCopyOfI]
                };
            }
        }

        private static void DrawExperiencePerLevelSection(Listing_Standard list)
        {
            Rect subSectionRect = list.GetRect(100);
            subSectionRect.width -= 30;
            Listing_Standard subSection = new Listing_Standard();
            subSection.Begin(subSectionRect);
            subSection.Label("SK_WeaponMastery_LevelSectionTitle".Translate());
            subSection.Gap();
            Rect lblRect = subSection.Label("SK_WeaponMastery_LevelSectionDetail".Translate());
            Rect dropdownRect = new Rect(subSectionRect.x, lblRect.y + 30, 100, 20);
            // This is an incorrect usage of the dropdown's API but I can't
            // wrap my head around how it works exactly. This is good enough
            // for my needs and it works.
            Widgets.Dropdown(dropdownRect, null, null, (string s) => GenerateLevelOptions(), "SK_WeaponMastery_LevelSectionDropdownOptionLabel".Translate(selectedLevelIndex + 1));
            Rect expLblRect = new Rect(dropdownRect.x + 110, lblRect.y + 30, 900, 30);
            Listing_Standard expLblRectList = new Listing_Standard();
            expLblRectList.Begin(expLblRect);
            expLblRectList.ColumnWidth = 230;
            expLblRectList.Label("SK_WeaponMastery_LevelSectionLevelExpLabel".Translate(selectedLevelIndex + 1));
            expLblRectList.NewColumn();
            expLblRectList.ColumnWidth = 160;
            expLblRectList.TextFieldNumeric(ref selectedLevelValue, ref selectedLevelStringBuffer);
            if (ModSettings.maxLevel != MAX_LEVELS)
            {
                expLblRectList.NewColumn();
                expLblRectList.ColumnWidth = 100;
                bool addButtonClicked = expLblRectList.ButtonText("SK_WeaponMastery_LevelSectionAddButton".Translate());
                if (addButtonClicked)
                    OnAddLevel();
            }
            if (ModSettings.maxLevel != 1)
            {
                expLblRectList.NewColumn();
                expLblRectList.ColumnWidth = 120;
                bool removeButtonClicked = expLblRectList.ButtonText("SK_WeaponMastery_LevelSectionRemoveButton".Translate());
                if (removeButtonClicked)
                    OnRemoveLevel();
            }
            expLblRectList.End();
            subSection.End();
            list.GapLine();
        }

        private static void DrawStatsSection(Listing_Standard list)
        {
            Rect subSectionRect = list.GetRect(150);
            Listing_Standard subSection = new Listing_Standard();
            subSection.Begin(subSectionRect);
            subSection.ColumnWidth = 390;
            // Ranged Stats Section
            subSection.Label("SK_WeaponMastery_RangedStatsSectionTitle".Translate());
            subSection.Gap();
            // Bad dropdown API usage
            Widgets.Dropdown(subSection.GetRect(30), null, null, (string s) => GenerateRangedStatsOptions(), selectedRangedDef.defName);
            subSection.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionLabel".Translate());
            // Percentage stats use values from 0 -> 1 which maps to 0% -> 100%

            if (!isRangedStatPercentage)
                rangedStatOffset = Widgets.HorizontalSlider(subSection.GetRect(22f), rangedStatOffset, MIN_STAT_BONUS, MAX_STAT_BONUS, false, ((float)System.Math.Round(rangedStatOffset, 2)).ToString(), null, null, 0.1f);
            else
                rangedStatOffset = Widgets.HorizontalSlider(subSection.GetRect(22f), rangedStatOffset, -1f, 1f, false, ((float)System.Math.Round(rangedStatOffset, 2)).ToString(), null, null, 0.01f);
            if (isRangedStatEnabled)
            {
                bool disableStatButtonClicked = subSection.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionDisableButton".Translate());
                if (disableStatButtonClicked)
                    OnDisableStat(false);
            }
            else
            {
                bool enableStatButtonClicked = subSection.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionEnableButton".Translate());
                if (enableStatButtonClicked)
                    OnEnableStat(false);
            }
            subSection.NewColumn();
            subSection.ColumnWidth = 390;
            // Melee Stats Section
            subSection.Label("SK_WeaponMastery_MeleeStatsSectionTitle".Translate());
            subSection.Gap();
            // Bad dropdown API usage
            Widgets.Dropdown(subSection.GetRect(30), null, null, (string s) => GenerateMeleeStatsOptions(), selectedMeleeDef.defName);
            subSection.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionLabel".Translate());
            // Percentage stats use values from 0 -> 1 which maps to 0% -> 100%
            if (!isMeleeStatPercentage)
                meleeStatOffset = Widgets.HorizontalSlider(subSection.GetRect(22f), meleeStatOffset, MIN_STAT_BONUS, MAX_STAT_BONUS, false, ((float)System.Math.Round(meleeStatOffset, 2)).ToString(), null, null, 0.1f);
            else
                meleeStatOffset = Widgets.HorizontalSlider(subSection.GetRect(22f), meleeStatOffset, -1f, 1f, false, ((float)System.Math.Round(meleeStatOffset, 2)).ToString(), null, null, 0.01f);
            if (isMeleeStatEnabled)
            {
                bool disableStatButtonClicked = subSection.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionDisableButton".Translate());
                if (disableStatButtonClicked)
                    OnDisableStat(true);
            }
            else
            {
                bool enableStatButtonClicked = subSection.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionEnableButton".Translate());
                if (enableStatButtonClicked)
                    OnEnableStat(true);
            }
            subSection.End();
            list.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionNote".Translate());
            list.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionWarning".Translate());
            list.Gap();
            list.GapLine();
        }

        private static void DrawModOptionsSection(Listing_Standard list)
        {
            Listing_Standard row = new Listing_Standard();
            row.Begin(list.GetRect(50));
            row.ColumnWidth = 300;
            row.Label("SK_WeaponMastery_ModSettingsSectionTitle".Translate());
            row.NewColumn();
            row.ColumnWidth = 470;
            row.Label("SK_WeaponMastery_ModSettingsSectionNote".Translate());
            row.End();
            Listing_Standard row1 = new Listing_Standard();
            row1.Begin(list.GetRect(30));
            row1.ColumnWidth = 300;
            row1.Label("SK_WeaponMastery_ModSettingsSectionWeaponNameChanceLabel".Translate(), -1, "SK_WeaponMastery_ModSettingsSectionWeaponNameChanceTooltip".Translate());
            row1.NewColumn();
            row1.ColumnWidth = 470;
            ModSettings.chanceToNameWeapon = Widgets.HorizontalSlider(row1.GetRect(30f), ModSettings.chanceToNameWeapon, 0.0f, 1f, false, ModSettings.chanceToNameWeapon.ToStringPercent(), null, null, 0.01f);
            row1.End();
            Listing_Standard row2 = new Listing_Standard();
            row2.Begin(list.GetRect(30));
            row2.ColumnWidth = 300;
            if (ModsConfig.RoyaltyActive)
                row2.Label("SK_WeaponMastery_ModSettingsSectionBondedWeaponExperienceMultiplierLabel".Translate(), -1);
            row2.NewColumn();
            row2.ColumnWidth = 470;
            if (ModsConfig.RoyaltyActive)
                ModSettings.bondedWeaponExperienceMultipier = Widgets.HorizontalSlider(row2.GetRect(30f), ModSettings.bondedWeaponExperienceMultipier, MIN_BONDED_WEAPON_MULTIPLIER, MAX_BONDED_WEAPON_MULTIPLIER, false, ModSettings.bondedWeaponExperienceMultipier.ToString("F1") + "x", null, null, 0.01f);
            row2.End();
            Listing_Standard row3 = new Listing_Standard();
            row3.Begin(list.GetRect(30));
            row3.ColumnWidth = 300;
            if (ModsConfig.IdeologyActive)
                row3.Label("SK_WeaponMastery_ModSettingsSectionRelicBonusStatsNumberLabel".Translate(), -1, "SK_WeaponMastery_ModSettingsSectionRelicBonusStatsNumberTooltip".Translate());
            row3.NewColumn();
            row3.ColumnWidth = 470;
            if (ModsConfig.IdeologyActive)
                ModSettings.numberOfRelicBonusStats = (int)Widgets.HorizontalSlider(row3.GetRect(30f), ModSettings.numberOfRelicBonusStats, MIN_RELIC_BONUS_STATS, MAX_RELIC_BONUS_STATS, false, ModSettings.numberOfRelicBonusStats.ToString(), null, null, 1f);
            row3.End();

            Rect subSectionRect1 = list.GetRect(150);
            Listing_Standard subSection1 = new Listing_Standard();
            subSection1.Begin(subSectionRect1);
            subSection1.CheckboxLabeled("SK_WeaponMastery_ModSettingsSectionSpecificMasterySystemLabel".Translate(), ref ModSettings.useSpecificMasterySystem, "SK_WeaponMastery_ModSettingsSectionSpecificMasterySystemTooltip".Translate());
            subSection1.CheckboxLabeled("SK_WeaponMastery_ModSettingsSectionGeneralMasterySystemLabel".Translate(), ref ModSettings.useGeneralMasterySystem, "SK_WeaponMastery_ModSettingsSectionGeneralMasterySystemTooltip".Translate());
            subSection1.CheckboxLabeled("SK_WeaponMastery_ModSettingsSectionMasteryOutsiderPawnsLabel".Translate(), ref ModSettings.masteryOnOutsidePawns, "SK_WeaponMastery_ModSettingsSectionMasteryOutsiderTooltip".Translate());
            subSection1.CheckboxLabeled("SK_WeaponMastery_ModSettingsUseMoodsCheckboxLabel".Translate(), ref ModSettings.useMoods, "SK_WeaponMastery_ModSettingsUseMoodsCheckboxTooltip".Translate());
            subSection1.CheckboxLabeled("SK_WeaponMastery_ModSettingsSectionDisplayExperienceLabel".Translate(), ref ModSettings.displayExperience, "SK_WeaponMastery_ModSettingsSectionDisplayExperienceTooltip".Translate());
            subSection1.CheckboxLabeled("SK_WeaponMastery_ModSettingsSectionKeepOriginalNameAndQualityLabel".Translate(), ref ModSettings.KeepOriginalWeaponNameQuality, "SK_WeaponMastery_ModSettingsSectionKeepOriginalNameAndQualityTooltip".Translate());
            subSection1.End();
            Rect subSection2Rect = list.GetRect(ModSettings.masteryOnOutsidePawns ? 70 : 0);
            Listing_Standard subSection2 = new Listing_Standard();
            subSection2.Begin(subSection2Rect);
            subSection2.ColumnWidth = 300;
            subSection2.Label("SK_WeaponMastery_ModSettingsSectionMasteryGroupPercentage".Translate());
            subSection2.Label("SK_WeaponMastery_ModSettingsSectionMasteryWeaponNameChancePerPawn".Translate());
            subSection2.NewColumn();
            subSection2.ColumnWidth = 470;
            ModSettings.masteriesPercentagePerEvent = Widgets.HorizontalSlider(subSection2.GetRect(30f), ModSettings.masteriesPercentagePerEvent, 0.01f, 1.0f, false, ModSettings.masteriesPercentagePerEvent.ToString("F2") + "%", null, null, 0.01f);
            ModSettings.eventWeaponNameChance = Widgets.HorizontalSlider(subSection2.GetRect(30f), ModSettings.eventWeaponNameChance, 0.01f, 1.0f, false, ModSettings.eventWeaponNameChance.ToString("F2") + "%", null, null, 0.01f);
            subSection2.End();
            list.GapLine();
        }

        private static void DrawCustomNamesSection(Listing_Standard list)
        {
            Rect subSectionRect = list.GetRect(ModSettings.useCustomNames ? 130 : 50);
            subSectionRect.width -= 30;
            Listing_Standard subSection = new Listing_Standard();
            subSection.Begin(subSectionRect);
            subSection.Label("SK_WeaponMastery_CustomNamesSectionTitle".Translate());
            subSection.CheckboxLabeled("SK_WeaponMastery_CustomNamesSectionCheckbox".Translate(), ref ModSettings.useCustomNames, "SK_WeaponMastery_CustomNamesSectionCheckboxTooltip".Translate());
            if (ModSettings.useCustomNames)
            {
                Rect addRemoveContainer = new Rect(subSectionRect.x, 50, 620, 100);
                Listing_Standard addRemoveList = new Listing_Standard();
                addRemoveList.Begin(addRemoveContainer);
                addRemoveList.ColumnWidth = 300;
                addRemoveList.Label("SK_WeaponMastery_CustomNamesAddNameLabel".Translate());
                addRemoveList.Label("SK_WeaponMastery_CustomNamesRemoveNameLabel".Translate());
                bool addButtonClicked = addRemoveList.ButtonText("SK_WeaponMastery_CustomNamesAddButton".Translate());
                if (addButtonClicked)
                    OnAddName();
                addRemoveList.NewColumn();
                addRemoveList.ColumnWidth = 300;
                customNameInputStringBuffer = addRemoveList.TextEntry(customNameInputStringBuffer);
                if (ModSettings.customWeaponNamesPool.Count > 0)
                    Widgets.Dropdown(addRemoveList.GetRect(22f), null, null, (string s) => GenerateCustomNameOptions(), selectedCustomName);
                else
                    addRemoveList.None();
                bool removeButtonClicked = addRemoveList.ButtonText("SK_WeaponMastery_CustomNamesRemoveButton".Translate());
                if (removeButtonClicked)
                    OnRemoveName();
                addRemoveList.End();
            }
            subSection.End();
            list.GapLine();
        }

        private static void DrawGeneralMasterySection(Listing_Standard list)
        {
            Rect subSectionRect = list.GetRect(250);
            Listing_Standard subSection = new Listing_Standard();
            subSection.Begin(subSectionRect);
            subSection.ColumnWidth = 250;
            subSection.Label("SK_WeaponMastery_GeneralMasterySectionAddRemoveClassesTitle".Translate());
            subSection.Gap();
            subSection.Label("SK_WeaponMastery_GeneralMasterySectionAddClassLabel".Translate());
            subSection.Gap();
            subSection.Label("SK_WeaponMastery_GeneralMasterySectionRemoveClassLabel".Translate());
            subSection.Gap();
            Rect lblRect = subSection.Label("SK_WeaponMastery_GeneralMasterySectionSelectClassLabel".Translate());
            Rect dropdownRect = new Rect(lblRect.x + 100, lblRect.y, 120, 30);
            Widgets.Dropdown(dropdownRect, null, null, (string s) => GenerateClassOptions(), allClasses[selectedClassIndex]);
            subSection.Gap();
            Rect lblRect1 = subSection.Label("SK_WeaponMastery_GeneralMasterySectionSelectWeaponLabel".Translate());
            Rect dropdownRect1 = new Rect(lblRect1.x + 100, lblRect1.y, 120, 30);
            Widgets.Dropdown(dropdownRect1, null, null, (string s) => GenerateWeaponOptions(), allWeapons[selectedWeaponIndex].LabelCap);
            subSection.NewColumn();
            subSection.ColumnWidth = 250;
            subSection.Label("");
            subSection.Gap();
            addClassInputStringBuffer = subSection.TextEntry(addClassInputStringBuffer);
            subSection.Gap();
            removeClassInputStringBuffer = subSection.TextEntry(removeClassInputStringBuffer);
            subSection.ButtonImage(cachedWeaponTexture, 100, 100);
            subSection.Gap();
            subSection.Label("SK_WeaponMastery_GeneralMasterySectionAssignedClassLabel".Translate(ModSettings.classes.ContainsKey(allWeapons[selectedWeaponIndex]) ? Utils.Capitalize(ModSettings.classes[allWeapons[selectedWeaponIndex]]) : "SK_WeaponMastery_GeneralMasterySectionNoClassLabel".Translate().ToString()));
            subSection.Label("");
            subSection.Gap();
            bool addClassButtonClicked = subSection.ButtonText("SK_WeaponMastery_GeneralMasterySectionAddClassButton".Translate());
            bool removeClassButtonClicked = subSection.ButtonText("SK_WeaponMastery_GeneralMasterySectionRemoveClassButton".Translate());
            subSection.Gap();
            subSection.Gap();
            bool assignClassButtonClicked = subSection.ButtonText("SK_WeaponMastery_GeneralMasterySectionAssignClassButton".Translate());
            bool clearClassButtonClicked = subSection.ButtonText("SK_WeaponMastery_GeneralMasterySectionClearClassButton".Translate());
            subSection.End();
            list.GapLine();

            if (addClassButtonClicked)
                OnClassAdded();

            if (removeClassButtonClicked)
                OnRemoveClass();

            if (assignClassButtonClicked)
                OnAssignClass();

            if (clearClassButtonClicked)
                OnClearClass();
        }

        // Directly updates ModSettings
        private static void OnAddLevel()
        {
            ModSettings.experiencePerLevel.Add(DEFAULT_EXP);
            selectedLevelIndex = ModSettings.experiencePerLevel.Count - 1;
            selectedLevelValue = DEFAULT_EXP;
            selectedLevelStringBuffer = selectedLevelValue.ToString();
            ModSettings.maxLevel += 1;
        }

        // Directly updates ModSettings
        private static void OnRemoveLevel()
        {
            if (selectedLevelIndex == ModSettings.experiencePerLevel.Count - 1)
            {
                selectedLevelIndex -= 1;
                selectedLevelValue = ModSettings.experiencePerLevel[selectedLevelIndex];
                selectedLevelStringBuffer = selectedLevelValue.ToString();
            }
            ModSettings.experiencePerLevel.Pop();
            ModSettings.maxLevel -= 1;
        }

        // Directly updates ModSettings
        private static void OnDisableStat(bool isMelee)
        {
            if (!isMelee)
            {
                ModSettings.RemoveStat(selectedRangedMasteryStat, isMelee);
                isRangedStatEnabled = false;
                Messages.Message("SK_WeaponMastery_MessageDisabledRangedStat".Translate(selectedRangedMasteryStat.GetStat().defName), MessageTypeDefOf.NeutralEvent);
                selectedRangedMasteryStat = null;
            }
            else
            {
                ModSettings.RemoveStat(selectedMeleeMasteryStat, isMelee);
                isMeleeStatEnabled = false;
                Messages.Message("SK_WeaponMastery_MessageDisabledMeleeStat".Translate(selectedMeleeMasteryStat.GetStat().defName), MessageTypeDefOf.NeutralEvent);
                selectedMeleeMasteryStat = null;
            }
        }

        // Directly updates ModSettings
        private static void OnEnableStat(bool isMelee)
        {
            if (!isMelee)
            {
                selectedRangedMasteryStat = new MasteryStat(selectedRangedDef, rangedStatOffset);
                ModSettings.AddStat(selectedRangedMasteryStat, isMelee);
                isRangedStatEnabled = true;
                Messages.Message("SK_WeaponMastery_MessageEnabledRangedStat".Translate(selectedRangedDef.defName), MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                selectedMeleeMasteryStat = new MasteryStat(selectedMeleeDef, meleeStatOffset);
                ModSettings.AddStat(selectedMeleeMasteryStat, isMelee);
                isMeleeStatEnabled = true;
                Messages.Message("SK_WeaponMastery_MessageEnabledMeleeStat".Translate(selectedMeleeDef.defName), MessageTypeDefOf.NeutralEvent);
            }
        }

        private static void OnClassAdded()
        {
            if (addClassInputStringBuffer == null || addClassInputStringBuffer.Length == 0 || allClasses.Contains(addClassInputStringBuffer)) return;
            Messages.Message("SK_WeaponMastery_GeneralMasterySectionAddClassMessage".Translate(addClassInputStringBuffer.CapitalizeFirst()), MessageTypeDefOf.NeutralEvent);
            allClasses.Add(addClassInputStringBuffer);
        }

        private static void OnAssignClass()
        {
            ModSettings.classes[allWeapons[selectedWeaponIndex]] = allClasses[selectedClassIndex];
            ModSettings.overrideClasses[allWeapons[selectedWeaponIndex].defName] = allClasses[selectedClassIndex];
            Messages.Message("SK_WeaponMastery_GeneralMasterySectionClassAssignedMessage".Translate(allWeapons[selectedWeaponIndex].LabelCap, allClasses[selectedClassIndex]), MessageTypeDefOf.NeutralEvent);
        }

        private static void OnClearClass()
        {
            ModSettings.classes.Remove(allWeapons[selectedWeaponIndex]);
            ModSettings.overrideClasses.Remove(allWeapons[selectedWeaponIndex].defName);
            Messages.Message("SK_WeaponMastery_GeneralMasterySectionClassClearedMessage".Translate(), MessageTypeDefOf.NeutralEvent);
        }

        private static void OnRemoveClass()
        {
            if (removeClassInputStringBuffer == null || removeClassInputStringBuffer.Length == 0 || !allClasses.Contains(removeClassInputStringBuffer)) return;
            List<ThingDef> toRemove = new List<ThingDef>();
            List<string> toRemoveOverride = new List<string>();
            foreach (KeyValuePair<ThingDef, string> item in ModSettings.classes)
                if (item.Value == removeClassInputStringBuffer)
                    toRemove.Add(item.Key);
            for (int i = 0; i < toRemove.Count; i++)
                ModSettings.classes.Remove(toRemove[i]);
            foreach (KeyValuePair<string, string> item in ModSettings.overrideClasses)
                if (item.Value == removeClassInputStringBuffer)
                    toRemoveOverride.Add(item.Key);
            for (int i = 0; i < toRemoveOverride.Count; i++)
                ModSettings.overrideClasses.Remove(toRemoveOverride[i]);
            allClasses.Remove(removeClassInputStringBuffer);
            Messages.Message("SK_WeaponMastery_GeneralMasterySectionRemoveClassMessage".Translate(removeClassInputStringBuffer), MessageTypeDefOf.NeutralEvent);
        }

        // Add custom name to ModSettings
        private static void OnAddName()
        {
            if (customNameInputStringBuffer == null || customNameInputStringBuffer.Length == 0) return;
            string trimmedName = customNameInputStringBuffer.Trim();
            if (ModSettings.customWeaponNamesPool.Contains(trimmedName)) return;
            ModSettings.customWeaponNamesPool.Add(trimmedName);
            Messages.Message("SK_WeaponMastery_CustomNamesAddNameMessage".Translate(trimmedName), MessageTypeDefOf.NeutralEvent);
            if (ModSettings.customWeaponNamesPool.Count == 1) selectedCustomName = trimmedName;
        }

        // Remove custom name from ModSettings
        private static void OnRemoveName()
        {
            if (ModSettings.customWeaponNamesPool.Count == 0) return;
            ModSettings.customWeaponNamesPool.Remove(selectedCustomName);
            Messages.Message("SK_WeaponMastery_CustomNamesRemoveNameMessage".Translate(selectedCustomName), MessageTypeDefOf.NeutralEvent);
            if (ModSettings.customWeaponNamesPool.Count > 0) selectedCustomName = ModSettings.customWeaponNamesPool[0];
            else selectedCustomName = null;
        }

        // Signal gc to clean variables by setting them to null (Possibly?)
        // Since these are statics, they'll always reserve memory unless
        // I set them to null hopefully
        public static void Destroy()
        {
            isOpen = false;
            thread = null;
            allStatDefs = null;
            selectedRangedDef = null;
            selectedMeleeDef = null;
            selectedMeleeMasteryStat = null;
            selectedRangedMasteryStat = null;
            selectedCustomName = null;
            allWeapons = null;
            cachedWeaponTexture = null;
            allClasses = null;
        }

        // Checks if StatDef is represented by % or numbers
        private static bool IsStatDefPercentage(StatDef def)
        {
            return def.toStringStyle == ToStringStyle.PercentZero ||
                def.toStringStyle == ToStringStyle.PercentOne ||
                def.toStringStyle == ToStringStyle.PercentTwo;
        }
    }
}
