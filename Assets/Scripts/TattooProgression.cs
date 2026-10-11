using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Valhalla.Campaign
{
    public sealed class TattooSpec
    {
        public string id, name, description, unlockHint;
        public BodyPart part;
        public TattooPower power;
        public int area, price;
    }

    public static class TattooCatalog
    {
        public static readonly TattooSpec[] All =
        {
            new TattooSpec
            {
                id = "thunder", name = "천둥의 각인",
                description = "강공격을 휘두르면 전방에 번개 범위 타격이 발생합니다.",
                unlockHint = "", part = BodyPart.RightArm, power = TattooPower.Thunder, area = 1, price = 0
            },
            new TattooSpec
            {
                id = "blood", name = "피의 룬",
                description = "적을 처치하면 생명을 회복합니다.",
                unlockHint = "", part = BodyPart.RightArm, power = TattooPower.Blood, area = 1, price = 35
            },
            new TattooSpec
            {
                id = "ram", name = "질주의 룬",
                description = "대시가 적과 충돌하면 피해를 입힙니다.",
                unlockHint = "", part = BodyPart.Legs, power = TattooPower.Ram, area = 1, price = 40
            },
            new TattooSpec
            {
                id = "reprisal", name = "반격의 각인",
                description = "패링에 성공하면 전방으로 충격파를 보냅니다. 피해 20.",
                unlockHint = "", part = BodyPart.RightArm, power = TattooPower.Reprisal, area = 1, price = 45
            },
            new TattooSpec
            {
                id = "echo", name = "메아리의 각인",
                description = "강공격 0.25초 후 전방에 18 피해의 두 번째 타격을 더합니다. 오른팔이 심각하게 다치면 대기 중인 타격도 사라집니다.",
                unlockHint = "오른팔 흉터 2개 이상 필요", part = BodyPart.RightArm, power = TattooPower.Echo, area = 2, price = 0
            },
            new TattooSpec
            {
                id = "frost", name = "서리 발걸음",
                description = "공격 회피에 성공하면 주변 적에게 피해 8과 0.8초 경직. 대시당 1회.",
                unlockHint = "다리 흉터 2개 이상 필요", part = BodyPart.Legs, power = TattooPower.Frost, area = 1, price = 0
            },
            new TattooSpec
            {
                id = "roar", name = "위압의 각인",
                description = "박치기가 적중하면 반경 3 안의 적에게 0.5초 경직을 추가로 줍니다. 추가 피해는 없습니다. 가격 45.",
                unlockHint = "", part = BodyPart.Head, power = TattooPower.Roar, area = 1, price = 45
            },
            new TattooSpec
            {
                id = "second_wind", name = "최후의 숨",
                description = "체력 35 이하에서 치명적이지 않은 피격을 받으면 주변 적을 밀어내며 피해 6과 경직 0.6초를 줍니다. 재사용 6초, 가격 60.",
                unlockHint = "", part = BodyPart.Chest, power = TattooPower.SecondWind, area = 1, price = 60
            },
            new TattooSpec
            {
                id = "recall", name = "발키리의 맹세",
                description = "명예 70 이상일 때 치명적인 피격에서 체력 10으로 생존하며 명예 30을 소모합니다. 출정당 1회, 가격 90. 등이 심각하게 다치면 사용할 수 없습니다.",
                unlockHint = "", part = BodyPart.Back, power = TattooPower.Recall, area = 1, price = 90
            },
            new TattooSpec
            {
                id = "countermark", name = "응수의 낙인",
                description = "패링에 성공하면 3초 동안 다음 일반 공격에 추가 피해 14를 충전합니다. 가격 50.",
                unlockHint = "", part = BodyPart.LeftArm, power = TattooPower.Countermark, area = 1, price = 50
            },
            new TattooSpec
            {
                id = "landing", name = "대지의 발",
                description = "점프 착지 시 반경 2.5 안의 적에게 피해 8과 경직 0.6초의 충격을 냅니다. 재사용 2초, 가격 45.",
                unlockHint = "", part = BodyPart.LeftLeg, power = TattooPower.Landing, area = 1, price = 45
            },
            new TattooSpec
            {
                id = "ember", name = "재의 반격",
                description = "화염 공격을 성공적으로 회피하면 3초 안의 다음 일반 도끼 공격에 화염 피해 18을 추가로 충전합니다. 가슴이 심각하게 다치면 충전이 사라집니다.",
                unlockHint = "화염 흉터 2개 이상 필요", part = BodyPart.Chest, power = TattooPower.Ember, area = 1, price = 0
            },
            new TattooSpec
            {
                id = "giant_counter", name = "거인을 꺾는 팔",
                description = "거인(보스)의 공격을 패링하면 4초 안의 다음 강공격이 보스·정예 상대에게만 추가 피해 25를 줍니다. 왼팔이 심각하게 다치면 충전이 사라집니다.",
                unlockHint = "거인 흉터 2개 이상 필요", part = BodyPart.LeftArm, power = TattooPower.GiantCounter, area = 1, price = 0
            },
            new TattooSpec
            {
                id = "valor", name = "용맹의 인장",
                description = "체력 35 이하에서 패링에 성공하고 명예 5 이상이면 생명 8을 회복하고 명예 5를 소모합니다. 등이 심각하게 다치면 사용할 수 없습니다.",
                unlockHint = "용맹의 흔적 1개 이상 필요", part = BodyPart.Back, power = TattooPower.Valor, area = 1, price = 0
            },
            new TattooSpec
            {
                id = "pain_echo", name = "고통의 메아리",
                description = "치명적이지 않은 피격을 받으면 피해를 최대 20까지 저장하고, 다음 박치기 명중 시 저장된 피해를 추가 피해로 돌려줍니다. 머리가 심각하게 다치면 저장된 고통이 사라집니다.",
                unlockHint = "생존자의 흔적 1개 이상 필요", part = BodyPart.Head, power = TattooPower.PainEcho, area = 1, price = 0
            },
        };

        public static string ProgressHint(PlayerProfile profile, BodyPart part)
        {
            switch(part)
            {
                case BodyPart.RightArm: return $"메아리 해금 · 오른팔 기억 {profile.armScars}/2";
                case BodyPart.Legs: return $"서리 해금 · 오른다리 기억 {profile.legScars}/2";
                case BodyPart.Head: return $"고통의 메아리 해금 · 생존 심사 {profile.survivorMarks}/1";
                case BodyPart.Chest: return $"재의 반격 해금 · 화염 흉터 {profile.fireScars}/2";
                case BodyPart.LeftArm: return $"거인 대응 해금 · 거인 흉터 {profile.giantScars}/2";
                case BodyPart.Back: return $"용맹의 인장 해금 · 용맹 심사 {profile.valorMarks}/1";
                default: return "점프·착지 능력";
            }
        }

        public static string TriggerHint(TattooPower power)
        {
            switch(power)
            {
                case TattooPower.Thunder: case TattooPower.Echo: return "K · 강공격";
                case TattooPower.Blood: return "적 처치 · 자동 회복";
                case TattooPower.Ram: return "SHIFT · 대시 충돌";
                case TattooPower.Reprisal: return "C · 패링 성공";
                case TattooPower.Frost: return "SHIFT · 공격 회피 성공";
                case TattooPower.Roar: return "Q · 박치기 명중";
                case TattooPower.SecondWind: return "생명 35 이하 · 피격 반응";
                case TattooPower.Recall: return "명예 70 이상 · 치명상 버티기";
                case TattooPower.Countermark: return "C 패링 → J 일반 공격";
                case TattooPower.Landing: return "SPACE · 점프 후 착지";
                case TattooPower.Ember: return "화염 회피 → J 일반 공격";
                case TattooPower.GiantCounter: return "거인 패링 → K 강공격";
                case TattooPower.Valor: return "빈사 상태 C 패링 · 명예 소비";
                case TattooPower.PainEcho: return "피격으로 저장 → Q 박치기";
                default: return "발동 조건 확인";
            }
        }

        public static TattooSpec Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].id == id) return All[i];
            }
            return null;
        }

        public static int Capacity(BodyPart part) => part == BodyPart.RightArm ? 2 : 1;

        public static bool IsUnlocked(PlayerProfile p, string id)
        {
            if (p == null) return false;
            TattooSpec spec = Find(id);
            if (spec == null) return false;
            if (id == "echo") return p.armScars >= 2;
            if (id == "frost") return p.legScars >= 2;
            if (id == "ember") return p.fireScars >= 2;
            if (id == "giant_counter") return p.giantScars >= 2;
            if (id == "valor") return p.valorMarks >= 1;
            if (id == "pain_echo") return p.survivorMarks >= 1;
            return true;
        }

        public static int UsedArea(PlayerProfile p, BodyPart part)
        {
            if (p == null || p.equippedTattoos == null) return 0;
            int total = 0;
            foreach (string id in p.equippedTattoos)
            {
                TattooSpec spec = Find(id);
                if (spec != null && spec.part == part) total += spec.area;
            }
            return total;
        }
    }

    public static class TattooProgressionValidation
    {
        public static List<string> Run()
        {
            List<string> results = new List<string>();

            RunLegacyMigration(results);
            RunIntentionalEmptyLoadoutPersists(results);
            RunOverflowRejectionIsAtomic(results);
            RunUnequipThenEquipAlternative(results);
            RunOwnershipCheck(results);
            RunLockedScarBuyRejected(results);
            RunThresholdUnlockRespectsArea(results);
            RunOtherLimbIndependent(results);
            RunJsonRoundTripFlagsAndLoadout(results);

            RunNewLimbsIndependentAreas(results);
            RunNewTattooJsonRoundTrip(results);
            RunNewTattooEmptyLoadoutPreserved(results);
            RunBodyBuildPartsAndGetMapping(results);
            RunBodyBuildResetAllSeven(results);
            RunBodyBuildNewLimbsIndependentGate(results);

            RunValhallaScarRejectedBeforeUnlock(results);
            RunValhallaScarEquipAfterThreshold(results);

            return results;
        }

        private static void Check(List<string> results, string name, bool condition)
        {
            results.Add((condition ? "PASS: " : "FAIL: ") + name);
        }

        private static string TempPath(string tag)
        {
            return Path.Combine(Path.GetTempPath(), "valhalla_tattoo_" + tag + "_" + Guid.NewGuid().ToString("N") + ".json");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* best-effort cleanup only */ }
        }

        private static void DeleteSaveFiles(string path)
        {
            TryDelete(path);
            TryDelete(path + ".bak");
            TryDelete(path + ".tmp");
        }

        private static void RunLegacyMigration(List<string> results)
        {
            string path = TempPath("legacy");
            try
            {
                string json = "{\"version\":1,\"coins\":123,\"tattoos\":[\"thunder\",\"blood\",\"ram\"]," +
                               "\"items\":[{\"id\":\"raw_hide\",\"count\":5}]}";
                File.WriteAllText(path, json);

                EconomyService service = new EconomyService(new JsonProfileStore(path));
                PlayerProfile p = service.Profile;
                bool equippedAll = p.equippedTattoos.Contains("thunder") && p.equippedTattoos.Contains("blood") && p.equippedTattoos.Contains("ram");
                Check(results, "Version 0 migration preserves coins and inventory",
                    p.coins == 123 && service.Count("raw_hide") == 5);
                Check(results, "Version 0 migration equips owned thunder+blood+ram within area limits",
                    equippedAll && p.tattooLoadoutVersion == 1
                    && TattooCatalog.UsedArea(p, BodyPart.RightArm) == 2
                    && TattooCatalog.UsedArea(p, BodyPart.Legs) == 1);
            }
            finally
            {
                DeleteSaveFiles(path);
            }
        }

        private static void RunIntentionalEmptyLoadoutPersists(List<string> results)
        {
            MemoryProfileStore store = new MemoryProfileStore();
            EconomyService service = new EconomyService(store);
            bool unequipped = service.UnequipTattoo("thunder", out _);
            service.Save();

            PlayerProfile reloaded = store.Load();
            Check(results, "Intentionally emptied loadout survives save/load without auto re-equip",
                unequipped && reloaded.equippedTattoos.Count == 0 && reloaded.tattooLoadoutVersion == 1);
        }

        private static void RunOverflowRejectionIsAtomic(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            service.Profile.coins = 500;
            service.BuyTattoo("blood", out _); // arm now full: thunder + blood = area 2 / capacity 2
            service.BuyTattoo("reprisal", out _); // owned but left unequipped: no area left

            int coinsBefore = service.Profile.coins;
            List<string> loadoutBefore = new List<string>(service.Profile.equippedTattoos);
            bool equipped = service.EquipTattoo("reprisal", out string message);

            Check(results, "Equip rejected when the body part has no remaining area",
                !equipped && service.Profile.coins == coinsBefore
                && new List<string>(service.Profile.equippedTattoos).Count == loadoutBefore.Count
                && !service.Profile.equippedTattoos.Contains("reprisal"));
        }

        private static void RunUnequipThenEquipAlternative(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            service.Profile.coins = 500;
            service.BuyTattoo("blood", out _);
            service.BuyTattoo("reprisal", out _); // owned but not equipped: arm already full

            bool unequipped = service.UnequipTattoo("blood", out _);
            bool equipped = service.EquipTattoo("reprisal", out _);

            Check(results, "Unequipping frees area so an alternative tattoo can be equipped",
                unequipped && equipped
                && !service.Profile.equippedTattoos.Contains("blood")
                && service.Profile.equippedTattoos.Contains("reprisal"));
        }

        private static void RunOwnershipCheck(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            bool equipped = service.EquipTattoo("reprisal", out string message);
            Check(results, "Equipping an unowned tattoo is refused", !equipped && !service.Profile.equippedTattoos.Contains("reprisal"));
        }

        private static void RunLockedScarBuyRejected(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            service.Profile.coins = 500;
            int coinsBefore = service.Profile.coins;
            bool bought = service.BuyTattoo("echo", out string message);
            Check(results, "Buying a scar-locked tattoo before the threshold is refused and leaves coins untouched",
                !bought && service.Profile.coins == coinsBefore && !service.Profile.tattoos.Contains("echo"));
        }

        private static void RunThresholdUnlockRespectsArea(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            service.Profile.coins = 500;
            service.Profile.armScars = 2;
            service.BuyTattoo("blood", out _); // arm full with thunder + blood

            bool boughtEcho = service.BuyTattoo("echo", out _); // unlocked now, but no area for its size-2 cost
            bool echoAutoEquipped = service.Profile.equippedTattoos.Contains("echo");

            service.UnequipTattoo("thunder", out _);
            service.UnequipTattoo("blood", out _);
            bool echoEquipped = service.EquipTattoo("echo", out string message);

            Check(results, "Threshold-unlocked tattoo can be bought without owning area, then equipped once space is freed",
                boughtEcho && !echoAutoEquipped && echoEquipped
                && TattooCatalog.UsedArea(service.Profile, BodyPart.RightArm) == 2);
        }

        private static void RunOtherLimbIndependent(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            service.Profile.coins = 500;
            service.Profile.legScars = 2;
            service.BuyTattoo("ram", out _); // legs full with ram (area 1 / capacity 1)

            bool boughtFrost = service.BuyTattoo("frost", out _);
            bool frostAutoEquipped = service.Profile.equippedTattoos.Contains("frost");
            bool armUnaffectedBefore = service.Profile.equippedTattoos.Contains("thunder");

            service.UnequipTattoo("ram", out _);
            bool frostEquipped = service.EquipTattoo("frost", out _);

            Check(results, "Leg tattoo capacity and unlocks are independent from the arm loadout",
                boughtFrost && !frostAutoEquipped && frostEquipped
                && armUnaffectedBefore && service.Profile.equippedTattoos.Contains("thunder"));
        }

        private static void RunJsonRoundTripFlagsAndLoadout(List<string> results)
        {
            string path = TempPath("roundtrip");
            try
            {
                EconomyService service = new EconomyService(new JsonProfileStore(path));
                service.Profile.coins = 500;
                service.BuyTattoo("blood", out _);
                service.BuyTattoo("ram", out _);
                service.Profile.armScars = 2;
                service.Profile.legScars = 2;
                service.Save();

                EconomyService reloaded = new EconomyService(new JsonProfileStore(path));
                PlayerProfile p = reloaded.Profile;
                Check(results, "JSON round-trip preserves scar counters and tattoo loadout",
                    p.armScars == 2 && p.legScars == 2 && p.tattooLoadoutVersion == 1
                    && p.equippedTattoos.Contains("thunder") && p.equippedTattoos.Contains("blood") && p.equippedTattoos.Contains("ram"));
            }
            finally
            {
                DeleteSaveFiles(path);
            }
        }

        private static void RunNewLimbsIndependentAreas(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            service.Profile.coins = 1000;

            bool boughtRoar = service.BuyTattoo("roar", out _);
            bool boughtWind = service.BuyTattoo("second_wind", out _);
            bool boughtRecall = service.BuyTattoo("recall", out _);
            bool boughtCounter = service.BuyTattoo("countermark", out _);
            bool boughtLanding = service.BuyTattoo("landing", out _);

            bool allAutoEquipped = service.Profile.equippedTattoos.Contains("roar")
                && service.Profile.equippedTattoos.Contains("second_wind")
                && service.Profile.equippedTattoos.Contains("recall")
                && service.Profile.equippedTattoos.Contains("countermark")
                && service.Profile.equippedTattoos.Contains("landing");

            bool unequippedHead = service.UnequipTattoo("roar", out _);
            bool othersStillEquipped = service.Profile.equippedTattoos.Contains("second_wind")
                && service.Profile.equippedTattoos.Contains("recall")
                && service.Profile.equippedTattoos.Contains("countermark")
                && service.Profile.equippedTattoos.Contains("landing");

            bool areasIndependent = TattooCatalog.UsedArea(service.Profile, BodyPart.Head) == 0
                && TattooCatalog.UsedArea(service.Profile, BodyPart.Chest) == 1
                && TattooCatalog.UsedArea(service.Profile, BodyPart.Back) == 1
                && TattooCatalog.UsedArea(service.Profile, BodyPart.LeftArm) == 1
                && TattooCatalog.UsedArea(service.Profile, BodyPart.LeftLeg) == 1;

            Check(results, "New limb tattoos occupy independent per-part areas and unequipping one does not disturb the others",
                boughtRoar && boughtWind && boughtRecall && boughtCounter && boughtLanding
                && allAutoEquipped && unequippedHead && othersStillEquipped && areasIndependent);
        }

        private static void RunNewTattooJsonRoundTrip(List<string> results)
        {
            string path = TempPath("new_tattoos_roundtrip");
            try
            {
                EconomyService service = new EconomyService(new JsonProfileStore(path));
                service.Profile.coins = 1000;
                service.BuyTattoo("roar", out _);
                service.BuyTattoo("second_wind", out _);
                service.BuyTattoo("recall", out _);
                service.BuyTattoo("countermark", out _);
                service.BuyTattoo("landing", out _);
                service.Save();

                EconomyService reloaded = new EconomyService(new JsonProfileStore(path));
                PlayerProfile p = reloaded.Profile;
                bool ownsAll = p.tattoos.Contains("roar") && p.tattoos.Contains("second_wind") && p.tattoos.Contains("recall")
                    && p.tattoos.Contains("countermark") && p.tattoos.Contains("landing");
                bool equippedAll = p.equippedTattoos.Contains("roar") && p.equippedTattoos.Contains("second_wind") && p.equippedTattoos.Contains("recall")
                    && p.equippedTattoos.Contains("countermark") && p.equippedTattoos.Contains("landing");

                Check(results, "New tattoo ownership and equip state for every new limb survive a JSON round-trip",
                    ownsAll && equippedAll && p.tattooLoadoutVersion == 1);
            }
            finally
            {
                DeleteSaveFiles(path);
            }
        }

        private static void RunNewTattooEmptyLoadoutPreserved(List<string> results)
        {
            string path = TempPath("new_tattoos_empty");
            try
            {
                EconomyService service = new EconomyService(new JsonProfileStore(path));
                service.Profile.coins = 1000;
                service.BuyTattoo("roar", out _);
                bool unequipped = service.UnequipTattoo("roar", out _);
                service.Save();

                EconomyService reloaded = new EconomyService(new JsonProfileStore(path));
                PlayerProfile p = reloaded.Profile;

                Check(results, "Intentionally emptied head loadout for a new tattoo survives save/load without auto re-equip",
                    unequipped && !p.equippedTattoos.Contains("roar") && p.tattoos.Contains("roar") && p.tattooLoadoutVersion == 1);
            }
            finally
            {
                DeleteSaveFiles(path);
            }
        }

        private static void RunBodyBuildPartsAndGetMapping(List<string> results)
        {
            BodyBuild body = new BodyBuild();
            bool partsHasSeven = BodyBuild.Parts.Length == 7;

            bool mappingCorrect = body.Get(BodyPart.RightArm) == body.arm
                && body.Get(BodyPart.Legs) == body.legs
                && body.Get(BodyPart.Head) == body.head
                && body.Get(BodyPart.Chest) == body.chest
                && body.Get(BodyPart.Back) == body.back
                && body.Get(BodyPart.LeftArm) == body.leftArm
                && body.Get(BodyPart.LeftLeg) == body.leftLeg;

            int statesCount = 0;
            foreach (BodyState state in body.States) statesCount++;

            bool threw = false;
            try { body.Get((BodyPart)999); }
            catch (ArgumentOutOfRangeException) { threw = true; }

            Check(results, "BodyBuild exposes all seven parts, maps Get explicitly, and rejects an unknown part",
                partsHasSeven && mappingCorrect && statesCount == 7 && threw);
        }

        private static void RunBodyBuildResetAllSeven(List<string> results)
        {
            BodyBuild body = new BodyBuild();
            foreach (BodyState state in body.States) state.AddTrauma(40);
            body.Reset();

            bool allCleared = true;
            foreach (BodyState state in body.States)
            {
                if (state.trauma != 0 || state.adaptation != Adaptation.None || state.Stage != 0) allCleared = false;
            }

            Check(results, "Reset clears trauma and adaptation across all seven body parts", allCleared);
        }

        private static void RunBodyBuildNewLimbsIndependentGate(List<string> results)
        {
            BodyBuild body = new BodyBuild();
            body.head.AddTrauma(60); // severe: tattoos on head disabled
            body.chest.AddTrauma(5); // healthy: tattoos on chest stay active

            TattooDefinition roar = ScriptableObject.CreateInstance<TattooDefinition>();
            roar.bodyPart = BodyPart.Head;
            roar.power = TattooPower.Roar;

            TattooDefinition secondWind = ScriptableObject.CreateInstance<TattooDefinition>();
            secondWind.bodyPart = BodyPart.Chest;
            secondWind.power = TattooPower.SecondWind;

            body.tattoos.Add(roar);
            body.tattoos.Add(secondWind);

            bool headGatedOff = !body.Has(TattooPower.Roar);
            bool chestStaysActive = body.Has(TattooPower.SecondWind);

            UnityEngine.Object.DestroyImmediate(roar);
            UnityEngine.Object.DestroyImmediate(secondWind);

            Check(results, "Severe head trauma gates only the head tattoo while a healthy chest tattoo stays active",
                headGatedOff && chestStaysActive);
        }

        private static void RunValhallaScarRejectedBeforeUnlock(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            service.Profile.coins = 500;

            bool boughtEmber = service.BuyTattoo("ember", out _);
            bool boughtGiant = service.BuyTattoo("giant_counter", out _);
            bool boughtValor = service.BuyTattoo("valor", out _);
            bool boughtPainEcho = service.BuyTattoo("pain_echo", out _);

            Check(results, "Judgment-mark tattoos are refused before their scar/mark thresholds are met",
                !boughtEmber && !boughtGiant && !boughtValor && !boughtPainEcho
                && !service.Profile.tattoos.Contains("ember") && !service.Profile.tattoos.Contains("giant_counter")
                && !service.Profile.tattoos.Contains("valor") && !service.Profile.tattoos.Contains("pain_echo"));
        }

        private static void RunValhallaScarEquipAfterThreshold(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            service.Profile.coins = 500;
            service.Profile.fireScars = 2;
            service.Profile.giantScars = 2;
            service.Profile.valorMarks = 1;
            service.Profile.survivorMarks = 1;

            int coinsBefore = service.Profile.coins;
            int scarLevelBefore = service.Profile.scarLevel;
            int bestPartBefore = service.Profile.bestPart;
            int deathsBefore = service.Profile.deaths;
            int completedRunsBefore = service.Profile.completedRuns;

            bool boughtEmber = service.BuyTattoo("ember", out _);
            bool boughtGiant = service.BuyTattoo("giant_counter", out _);
            bool boughtValor = service.BuyTattoo("valor", out _);
            bool boughtPainEcho = service.BuyTattoo("pain_echo", out _);

            bool allEquipped = service.Profile.equippedTattoos.Contains("ember")
                && service.Profile.equippedTattoos.Contains("giant_counter")
                && service.Profile.equippedTattoos.Contains("valor")
                && service.Profile.equippedTattoos.Contains("pain_echo");

            bool areasAcrossLimbs = TattooCatalog.UsedArea(service.Profile, BodyPart.Chest) == 1
                && TattooCatalog.UsedArea(service.Profile, BodyPart.LeftArm) == 1
                && TattooCatalog.UsedArea(service.Profile, BodyPart.Back) == 1
                && TattooCatalog.UsedArea(service.Profile, BodyPart.Head) == 1;

            // Free (price 0) alternatives must still cost nothing beyond the unlock itself and must not
            // touch any unrelated profile counter, confirming unlocks grant alternatives, not automatic buffs.
            bool noUnrelatedProfileChange = service.Profile.coins == coinsBefore
                && service.Profile.scarLevel == scarLevelBefore
                && service.Profile.bestPart == bestPartBefore
                && service.Profile.deaths == deathsBefore
                && service.Profile.completedRuns == completedRunsBefore;

            Check(results, "Judgment-mark tattoos equip as free alternatives across four different limbs once thresholds are met, with no unrelated profile buff",
                boughtEmber && boughtGiant && boughtValor && boughtPainEcho
                && allEquipped && areasAcrossLimbs && noUnrelatedProfileChange);
        }
    }
}
