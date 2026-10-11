using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Valhalla.Campaign
{
    [Serializable]
    public class ItemStack
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class PlayerProfile
    {
        public int version = 1;
        public int coins = 80;
        public bool tutorialComplete;
        public bool hasViewedInventory, hasProcessed, hasSold, hasReviewedTattoos, hasReviewedEquipment, hasDeparted;
        public int bestPart;
        public int completedRuns;
        public int deaths;
        public int scarLevel;
        public string equippedWeapon = "starter_axe";
        public string equippedArmor = "none";
        public List<string> ownedEquipment = new List<string> { "starter_axe" };
        public List<string> tattoos = new List<string> { "thunder" };
        public List<string> equippedTattoos = new List<string>();
        public int tattooLoadoutVersion;
        public int armScars;
        public int legScars;
        public int headScars, chestScars, backScars, leftArmScars, leftLegScars;
        public int fireScars, giantScars, valorMarks, survivorMarks;
        public string lastJudgment="";
        public int Scars(BodyPart part) => part==BodyPart.RightArm?armScars:part==BodyPart.Legs?legScars:part==BodyPart.Head?headScars:part==BodyPart.Chest?chestScars:part==BodyPart.Back?backScars:part==BodyPart.LeftArm?leftArmScars:leftLegScars;
        public void AddScar(BodyPart part) {
            switch(part){case BodyPart.Head:headScars=Math.Min(99,headScars+1);break;case BodyPart.Chest:chestScars=Math.Min(99,chestScars+1);break;case BodyPart.Back:backScars=Math.Min(99,backScars+1);break;case BodyPart.LeftArm:leftArmScars=Math.Min(99,leftArmScars+1);break;case BodyPart.LeftLeg:leftLegScars=Math.Min(99,leftLegScars+1);break;}
        }
        public List<ItemStack> items = new List<ItemStack>();
        public int lastSeed;
    }

    public sealed class ItemDefinition
    {
        public string id;
        public string name;
        public string description;
        public string category;
        public int sellPrice;
        public int maxStack;
    }

    public sealed class RecipeDefinition
    {
        public string id;
        public string name;
        public string resultId;
        public int resultCount;
        public int fee;
        public ItemStack[] costs;
    }

    public sealed class EquipmentDefinition
    {
        public string id;
        public string name;
        public string description;
        public string slot;
        public int price;
        public float damageMultiplier;
        public float traumaMultiplier;
    }

    public static class EconomyCatalog
    {
        public static readonly ItemDefinition[] Items =
        {
            new ItemDefinition { id = "raw_hide", name = "Raw Hide", description = "Rough hide stripped from a kill.", category = "material", sellPrice = 4, maxStack = 99 },
            new ItemDefinition { id = "bone_shard", name = "Bone Shard", description = "Splintered bone fragment.", category = "material", sellPrice = 3, maxStack = 99 },
            new ItemDefinition { id = "iron_scrap", name = "Iron Scrap", description = "Corroded scrap metal.", category = "material", sellPrice = 5, maxStack = 99 },
            new ItemDefinition { id = "tanned_leather", name = "Tanned Leather", description = "Cured leather ready for binding.", category = "material", sellPrice = 10, maxStack = 99 },
            new ItemDefinition { id = "bone_fitting", name = "Bone Fitting", description = "Carved bone fitting for gear.", category = "material", sellPrice = 8, maxStack = 99 },
            new ItemDefinition { id = "refined_iron", name = "Refined Iron", description = "Smelted, workable iron.", category = "material", sellPrice = 13, maxStack = 99 },
            new ItemDefinition { id = "healing_salve", name = "Healing Salve", description = "A battlefield remedy.", category = "consumable", sellPrice = 8, maxStack = 10 },
        };

        public static readonly RecipeDefinition[] Recipes =
        {
            new RecipeDefinition { id = "tan_hide", name = "Tan Hide", resultId = "tanned_leather", resultCount = 1, fee = 0, costs = new[] { new ItemStack { id = "raw_hide", count = 2 } } },
            new RecipeDefinition { id = "carve_bone", name = "Carve Bone", resultId = "bone_fitting", resultCount = 1, fee = 0, costs = new[] { new ItemStack { id = "bone_shard", count = 2 } } },
            new RecipeDefinition { id = "smelt_iron", name = "Smelt Iron", resultId = "refined_iron", resultCount = 1, fee = 0, costs = new[] { new ItemStack { id = "iron_scrap", count = 2 } } },
            new RecipeDefinition
            {
                id = "forge_axe", name = "Forge Iron Axe", resultId = "iron_axe", resultCount = 1, fee = 10,
                costs = new[] { new ItemStack { id = "refined_iron", count = 2 }, new ItemStack { id = "bone_fitting", count = 1 } }
            },
            new RecipeDefinition
            {
                id = "leather_bracer", name = "Craft Leather Bracer", resultId = "leather_bracer", resultCount = 1, fee = 5,
                costs = new[] { new ItemStack { id = "tanned_leather", count = 2 }, new ItemStack { id = "bone_fitting", count = 1 } }
            },
            new RecipeDefinition
            {
                id = "healing_salve", name = "Brew Healing Salve", resultId = "healing_salve", resultCount = 1, fee = 0,
                costs = new[] { new ItemStack { id = "raw_hide", count = 1 }, new ItemStack { id = "bone_shard", count = 1 } }
            },
        };

        public static readonly EquipmentDefinition[] Equipment =
        {
            new EquipmentDefinition { id = "starter_axe", name = "Starter Axe", description = "A worn but reliable axe.", slot = "weapon", price = 0, damageMultiplier = 1f, traumaMultiplier = 1f },
            new EquipmentDefinition { id = "iron_axe", name = "Iron Axe", description = "A sturdy forged axe.", slot = "weapon", price = 65, damageMultiplier = 1.22f, traumaMultiplier = 1f },
            new EquipmentDefinition { id = "runic_axe", name = "Runic Axe", description = "An axe etched with old runes.", slot = "weapon", price = 140, damageMultiplier = 1.45f, traumaMultiplier = 1f },
            new EquipmentDefinition { id = "leather_bracer", name = "Leather Bracer", description = "Protects the right arm.", slot = "armor", price = 45, damageMultiplier = 1f, traumaMultiplier = 0.75f },
            new EquipmentDefinition { id = "iron_greaves", name = "Iron Greaves", description = "Protects the legs.", slot = "armor", price = 60, damageMultiplier = 1f, traumaMultiplier = 0.75f },
        };

        public static ItemDefinition Item(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].id == id) return Items[i];
            }
            return null;
        }

        public static EquipmentDefinition Gear(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Equipment.Length; i++)
            {
                if (Equipment[i].id == id) return Equipment[i];
            }
            return null;
        }

        public static RecipeDefinition Recipe(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Recipes.Length; i++)
            {
                if (Recipes[i].id == id) return Recipes[i];
            }
            return null;
        }
    }

    public interface IProfileStore
    {
        PlayerProfile Load();
        void Save(PlayerProfile p);
    }

    public sealed class JsonProfileStore : IProfileStore
    {
        private readonly string _path;
        private readonly string _backupPath;

        public JsonProfileStore(string path)
        {
            _path = path;
            _backupPath = path + ".bak";
        }

        public PlayerProfile Load()
        {
            if (!File.Exists(_path))
            {
                return EconomyNormalization.Normalize(new PlayerProfile());
            }

            PlayerProfile loaded = TryReadJson(_path);
            if (loaded != null)
            {
                return EconomyNormalization.Normalize(loaded);
            }

            PlayerProfile fromBackup = TryReadJson(_backupPath);
            if (fromBackup != null)
            {
                return EconomyNormalization.Normalize(fromBackup);
            }

            QuarantineCorruptFile();
            return EconomyNormalization.Normalize(new PlayerProfile());
        }

        public void Save(PlayerProfile p)
        {
            if (p == null) return;
            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(p, true);
            string tempPath = _path + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(_path))
            {
                File.Replace(tempPath, _path, _backupPath);
            }
            else
            {
                File.Move(tempPath, _path);
            }
        }

        private static PlayerProfile TryReadJson(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string text = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(text)) return null;
                PlayerProfile result = JsonUtility.FromJson<PlayerProfile>(text);
                return result;
            }
            catch
            {
                return null;
            }
        }

        private void QuarantineCorruptFile()
        {
            try
            {
                string quarantinePath = _path + ".corrupt-" + DateTime.UtcNow.Ticks;
                File.Move(_path, quarantinePath);
            }
            catch
            {
                // Best effort: leave the unreadable file in place rather than losing data.
            }
        }
    }

    public sealed class MemoryProfileStore : IProfileStore
    {
        private PlayerProfile _saved;

        public MemoryProfileStore()
        {
            _saved = EconomyNormalization.Normalize(new PlayerProfile());
        }

        public PlayerProfile Load()
        {
            return Clone(_saved);
        }

        public void Save(PlayerProfile p)
        {
            _saved = Clone(p);
        }

        private static PlayerProfile Clone(PlayerProfile source)
        {
            if (source == null) return EconomyNormalization.Normalize(new PlayerProfile());
            string json = JsonUtility.ToJson(source);
            PlayerProfile copy = JsonUtility.FromJson<PlayerProfile>(json);
            return EconomyNormalization.Normalize(copy);
        }
    }

    internal static class EconomyNormalization
    {
        public static PlayerProfile Normalize(PlayerProfile p)
        {
            if (p == null) p = new PlayerProfile();
            if (p.version <= 0) p.version = 1;
            if (p.items == null) p.items = new List<ItemStack>();
            if (p.ownedEquipment == null) p.ownedEquipment = new List<string>();
            if (p.tattoos == null) p.tattoos = new List<string>();
            if (p.equippedTattoos == null) p.equippedTattoos = new List<string>();
            if (string.IsNullOrEmpty(p.equippedWeapon)) p.equippedWeapon = "starter_axe";
            if (string.IsNullOrEmpty(p.equippedArmor)) p.equippedArmor = "none";
            if (!p.ownedEquipment.Contains("starter_axe")) p.ownedEquipment.Add("starter_axe");
            if (!p.tattoos.Contains("thunder")) p.tattoos.Add("thunder");

            // Pre-loadout saves (version 0) carried an implicit "every owned tattoo is active" rule.
            // Migrate once by equipping owned tattoos in owned order while they fit their body part's area.
            if (p.tattooLoadoutVersion <= 0)
            {
                MigrateLoadout(p);
                p.tattooLoadoutVersion = 1;
            }

            CleanLoadout(p);
            return p;
        }

        private static void MigrateLoadout(PlayerProfile p)
        {
            p.equippedTattoos.Clear();
            var used = new Dictionary<BodyPart, int>();
            foreach (string id in p.tattoos)
            {
                TattooSpec spec = TattooCatalog.Find(id);
                if (spec == null || !TattooCatalog.IsUnlocked(p, id)) continue;
                used.TryGetValue(spec.part, out int usedArea);
                if (usedArea + spec.area > TattooCatalog.Capacity(spec.part)) continue;
                p.equippedTattoos.Add(id);
                used[spec.part] = usedArea + spec.area;
            }
        }

        // Re-validates the loadout on every load so later ownership/unlock/catalog changes
        // cannot leave a duplicate, unknown, unowned, locked or over-capacity entry equipped.
        private static void CleanLoadout(PlayerProfile p)
        {
            var cleaned = new List<string>();
            var used = new Dictionary<BodyPart, int>();
            foreach (string id in p.equippedTattoos)
            {
                if (string.IsNullOrEmpty(id) || cleaned.Contains(id)) continue;
                TattooSpec spec = TattooCatalog.Find(id);
                if (spec == null || !p.tattoos.Contains(id) || !TattooCatalog.IsUnlocked(p, id)) continue;
                used.TryGetValue(spec.part, out int usedArea);
                if (usedArea + spec.area > TattooCatalog.Capacity(spec.part)) continue;
                cleaned.Add(id);
                used[spec.part] = usedArea + spec.area;
            }
            p.equippedTattoos = cleaned;
        }
    }

    public sealed class EconomyService
    {
        private readonly IProfileStore _store;

        public PlayerProfile Profile { get; private set; }
        public event Action Changed;

        public EconomyService(IProfileStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Profile = EconomyNormalization.Normalize(_store.Load());
        }

        public int Count(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            ItemStack stack = FindStack(id);
            return stack?.count ?? 0;
        }

        public bool Add(string id, int quantity, out string message)
        {
            ItemDefinition def = EconomyCatalog.Item(id);
            if (def == null)
            {
                message = "Unknown item: " + id;
                return false;
            }
            if (quantity <= 0)
            {
                message = "Quantity must be positive.";
                return false;
            }

            long current = Count(id);
            if (current + (long)quantity > def.maxStack)
            {
                message = "Not enough stack capacity for " + def.name + ".";
                return false;
            }

            AddInternal(id, quantity);
            Commit();
            message = "Added " + quantity + " " + def.name + ".";
            return true;
        }

        public bool Consume(string id, int quantity, out string message)
        {
            var definition = EconomyCatalog.Item(id);
            if (definition == null || definition.category != "consumable" || quantity <= 0 || Count(id) < quantity)
            {
                message = "Unavailable consumable quantity.";
                return false;
            }
            RemoveInternal(id, quantity);
            Commit();
            message = "Consumed " + definition.name;
            return true;
        }

        public bool Sell(string id, int quantity, out string message)
        {
            ItemDefinition def = EconomyCatalog.Item(id);
            if (def == null)
            {
                message = "Unknown item: " + id;
                return false;
            }
            if (quantity <= 0)
            {
                message = "Quantity must be positive.";
                return false;
            }

            int current = Count(id);
            if (quantity > current)
            {
                message = "Not enough " + def.name + " to sell.";
                return false;
            }

            long revenue = (long)def.sellPrice * quantity;
            if (revenue > int.MaxValue - (long)Profile.coins)
            {
                message = "Sale would overflow coin total.";
                return false;
            }

            RemoveInternal(id, quantity);
            Profile.coins += (int)revenue;
            Profile.hasSold = true;
            Commit();
            message = "Sold " + quantity + " " + def.name + " for " + revenue + " coins.";
            return true;
        }

        public bool Craft(string recipeId, out string message)
        {
            RecipeDefinition recipe = EconomyCatalog.Recipe(recipeId);
            if (recipe == null)
            {
                message = "Unknown recipe: " + recipeId;
                return false;
            }

            EquipmentDefinition gearResult = EconomyCatalog.Gear(recipe.resultId);
            ItemDefinition itemResult = gearResult == null ? EconomyCatalog.Item(recipe.resultId) : null;
            if (gearResult == null && itemResult == null)
            {
                message = "Recipe result is not defined: " + recipe.resultId;
                return false;
            }

            if (gearResult != null && Profile.ownedEquipment.Contains(gearResult.id))
            {
                message = "Already own " + gearResult.name + ".";
                return false;
            }

            Dictionary<string, int> required = new Dictionary<string, int>();
            if (recipe.costs != null)
            {
                foreach (ItemStack cost in recipe.costs)
                {
                    if (cost == null || string.IsNullOrEmpty(cost.id) || cost.count <= 0)
                    {
                        message = "Recipe has invalid cost data.";
                        return false;
                    }
                    if (EconomyCatalog.Item(cost.id) == null)
                    {
                        message = "Recipe references unknown material: " + cost.id;
                        return false;
                    }
                    required.TryGetValue(cost.id, out int existing);
                    required[cost.id] = existing + cost.count;
                }
            }

            foreach (KeyValuePair<string, int> req in required)
            {
                if (Count(req.Key) < req.Value)
                {
                    message = "Not enough " + req.Key + " to craft " + recipe.name + ".";
                    return false;
                }
            }

            if (Profile.coins < recipe.fee)
            {
                message = "Not enough coins to craft " + recipe.name + ".";
                return false;
            }

            if (itemResult != null)
            {
                long current = Count(itemResult.id);
                if (current + (long)recipe.resultCount > itemResult.maxStack)
                {
                    message = "Not enough stack capacity for " + itemResult.name + ".";
                    return false;
                }
            }

            foreach (KeyValuePair<string, int> req in required)
            {
                RemoveInternal(req.Key, req.Value);
            }
            Profile.coins -= recipe.fee;
            if (recipeId == "tan_hide" || recipeId == "carve_bone" || recipeId == "smelt_iron") Profile.hasProcessed = true;

            if (gearResult != null)
            {
                Profile.ownedEquipment.Add(gearResult.id);
            }
            else
            {
                AddInternal(itemResult.id, recipe.resultCount);
            }

            Commit();
            message = "Crafted " + recipe.name + ".";
            return true;
        }

        public bool BuyEquipment(string id, out string message)
        {
            EquipmentDefinition def = EconomyCatalog.Gear(id);
            if (def == null)
            {
                message = "Unknown equipment: " + id;
                return false;
            }
            if (Profile.ownedEquipment.Contains(id))
            {
                message = "Already own " + def.name + ".";
                return false;
            }
            if (Profile.coins < def.price)
            {
                message = "Not enough coins for " + def.name + ".";
                return false;
            }

            Profile.coins -= def.price;
            Profile.ownedEquipment.Add(id);
            Commit();
            message = "Bought " + def.name + ".";
            return true;
        }

        public bool Equip(string id, out string message)
        {
            EquipmentDefinition def = EconomyCatalog.Gear(id);
            if (def == null)
            {
                message = "Unknown equipment: " + id;
                return false;
            }
            if (!Profile.ownedEquipment.Contains(id))
            {
                message = "Do not own " + def.name + ".";
                return false;
            }

            if (def.slot == "weapon")
            {
                Profile.equippedWeapon = id;
            }
            else if (def.slot == "armor")
            {
                Profile.equippedArmor = id;
            }
            else
            {
                message = "Equipment has an invalid slot: " + def.slot;
                return false;
            }

            Commit();
            message = "Equipped " + def.name + ".";
            return true;
        }

        public bool UnequipArmor(out string message)
        {
            if(Profile.equippedArmor=="none") { message="착용 중인 방어구가 없습니다."; return false; }
            Profile.equippedArmor="none";
            Commit();
            message="방어구를 해제했습니다. 장비는 보유하며 부위 외상이 다시 정상 속도로 쌓입니다.";
            return true;
        }

        public bool BuyTattoo(string id, out string message)
        {
            TattooSpec spec = TattooCatalog.Find(id);
            if (spec == null)
            {
                message = "알 수 없는 문신입니다: " + id;
                return false;
            }
            if (Profile.tattoos.Contains(id))
            {
                message = spec.name + " 문신을 이미 보유하고 있습니다.";
                return false;
            }
            if (!TattooCatalog.IsUnlocked(Profile, id))
            {
                message = spec.name + " 문신은 아직 해금되지 않았습니다." + (string.IsNullOrEmpty(spec.unlockHint) ? "" : " (" + spec.unlockHint + ")");
                return false;
            }
            if (Profile.coins < spec.price)
            {
                message = spec.name + " 문신을 구매할 은화가 부족합니다.";
                return false;
            }

            Profile.coins -= spec.price;
            Profile.tattoos.Add(id);

            int used = TattooCatalog.UsedArea(Profile, spec.part);
            bool autoEquipped = used + spec.area <= TattooCatalog.Capacity(spec.part);
            if (autoEquipped) Profile.equippedTattoos.Add(id);

            Commit();
            message = autoEquipped
                ? spec.name + " 문신을 획득하고 장착했습니다."
                : spec.name + " 문신을 획득했습니다. 장착하려면 같은 부위의 공간을 비우세요.";
            return true;
        }

        public bool EquipTattoo(string id, out string message)
        {
            TattooSpec spec = TattooCatalog.Find(id);
            if (spec == null)
            {
                message = "알 수 없는 문신입니다: " + id;
                return false;
            }
            if (!Profile.tattoos.Contains(id))
            {
                message = spec.name + " 문신을 보유하고 있지 않습니다.";
                return false;
            }
            if (!TattooCatalog.IsUnlocked(Profile, id))
            {
                message = spec.name + " 문신은 아직 해금되지 않았습니다." + (string.IsNullOrEmpty(spec.unlockHint) ? "" : " (" + spec.unlockHint + ")");
                return false;
            }
            if (Profile.equippedTattoos.Contains(id))
            {
                message = spec.name + " 문신은 이미 장착되어 있습니다.";
                return false;
            }
            int used = TattooCatalog.UsedArea(Profile, spec.part);
            if (used + spec.area > TattooCatalog.Capacity(spec.part))
            {
                message = spec.name + " 문신을 장착할 공간이 부족합니다.";
                return false;
            }

            Profile.equippedTattoos.Add(id);
            Commit();
            message = spec.name + " 문신을 장착했습니다.";
            return true;
        }

        public bool UnequipTattoo(string id, out string message)
        {
            TattooSpec spec = TattooCatalog.Find(id);
            if (spec == null)
            {
                message = "알 수 없는 문신입니다: " + id;
                return false;
            }
            if (!Profile.equippedTattoos.Contains(id))
            {
                message = spec.name + " 문신은 장착되어 있지 않습니다.";
                return false;
            }

            Profile.equippedTattoos.Remove(id);
            Commit();
            message = spec.name + " 문신을 해제했습니다.";
            return true;
        }

        public void Save()
        {
            Commit();
        }

        private void Commit()
        {
            _store.Save(Profile);
            Changed?.Invoke();
        }

        private ItemStack FindStack(string id)
        {
            List<ItemStack> items = Profile.items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].id == id) return items[i];
            }
            return null;
        }

        private void AddInternal(string id, int quantity)
        {
            ItemStack stack = FindStack(id);
            if (stack != null)
            {
                stack.count += quantity;
            }
            else
            {
                Profile.items.Add(new ItemStack { id = id, count = quantity });
            }
        }

        private void RemoveInternal(string id, int quantity)
        {
            ItemStack stack = FindStack(id);
            if (stack == null) return;
            stack.count -= quantity;
            if (stack.count <= 0)
            {
                Profile.items.Remove(stack);
            }
        }
    }

    public static class EconomyValidation
    {
        public static List<string> Run()
        {
            List<string> results = new List<string>();

            RunInsufficientMaterialsUnchanged(results);
            RunCraftAndSell(results);
            RunDuplicatePurchase(results);
            RunUnownedEquip(results);
            RunUnknownAndNegativeInputs(results);
            RunMemoryCloneIsolation(results);
            RunJsonRoundTrip(results);
            RunCorruptionRecovery(results);

            return results;
        }

        private static void Check(List<string> results, string name, bool condition)
        {
            results.Add((condition ? "PASS: " : "FAIL: ") + name);
        }

        private static void RunInsufficientMaterialsUnchanged(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            int coinsBefore = service.Profile.coins;
            bool crafted = service.Craft("tan_hide", out string message);
            Check(results, "Craft fails without materials and leaves coins unchanged", !crafted && service.Profile.coins == coinsBefore && service.Count("tanned_leather") == 0);
        }

        private static void RunCraftAndSell(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            service.Add("raw_hide", 2, out _);
            bool crafted = service.Craft("tan_hide", out string craftMessage);
            Check(results, "Craft tan_hide consumes raw_hide and grants tanned_leather", crafted && service.Count("raw_hide") == 0 && service.Count("tanned_leather") == 1);

            int coinsBefore = service.Profile.coins;
            bool sold = service.Sell("tanned_leather", 1, out string sellMessage);
            Check(results, "Sell tanned_leather pays correct coins and clears stack", sold && service.Count("tanned_leather") == 0 && service.Profile.coins == coinsBefore + 10);
        }

        private static void RunDuplicatePurchase(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            service.Profile.coins = 500;
            bool firstBuy = service.BuyEquipment("iron_axe", out _);
            bool secondBuy = service.BuyEquipment("iron_axe", out string secondMessage);
            Check(results, "Duplicate equipment purchase is refused", firstBuy && !secondBuy);
        }

        private static void RunUnownedEquip(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            bool equipped = service.Equip("runic_axe", out string message);
            Check(results, "Equipping unowned gear is refused", !equipped);
        }

        private static void RunUnknownAndNegativeInputs(List<string> results)
        {
            EconomyService service = new EconomyService(new MemoryProfileStore());
            bool unknownAdd = service.Add("bogus_item", 1, out _);
            bool negativeAdd = service.Add("raw_hide", -1, out _);
            bool zeroSell = service.Sell("raw_hide", 0, out _);
            Check(results, "Unknown item, negative quantity and zero quantity are all refused", !unknownAdd && !negativeAdd && !zeroSell);
        }

        private static void RunMemoryCloneIsolation(List<string> results)
        {
            MemoryProfileStore store = new MemoryProfileStore();
            EconomyService service = new EconomyService(store);
            service.Profile.coins = 12345;
            PlayerProfile reloaded = store.Load();
            Check(results, "Memory store isolates unsaved mutations via deep clone", reloaded.coins != 12345);
        }

        private static void RunJsonRoundTrip(List<string> results)
        {
            string path = Path.Combine(Path.GetTempPath(), "valhalla_economy_test_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                EconomyService service = new EconomyService(new JsonProfileStore(path));
                service.Add("iron_scrap", 4, out _);
                service.Craft("smelt_iron", out _);
                service.Save();

                EconomyService reloadedService = new EconomyService(new JsonProfileStore(path));
                Check(results, "JSON store round-trips crafted inventory and processing guidance", reloadedService.Count("refined_iron") == 1 && reloadedService.Count("iron_scrap") == 2 && reloadedService.Profile.hasProcessed);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
                TryDelete(path + ".tmp");
            }
        }

        private static void RunCorruptionRecovery(List<string> results)
        {
            string path = Path.Combine(Path.GetTempPath(), "valhalla_economy_corrupt_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(path, "{ this is not valid json ");
                EconomyService service = new EconomyService(new JsonProfileStore(path));
                Check(results, "Corrupted save recovers to a usable default profile", service.Profile.coins == 80 && service.Profile.ownedEquipment.Contains("starter_axe"));
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
                TryDelete(path + ".tmp");
                DeleteQuarantineFiles(path);
            }
        }

        private static void DeleteQuarantineFiles(string originalPath)
        {
            try
            {
                string directory = Path.GetDirectoryName(originalPath);
                string prefix = Path.GetFileName(originalPath) + ".corrupt-";
                if (string.IsNullOrEmpty(directory)) return;
                foreach (string file in Directory.GetFiles(directory, Path.GetFileName(originalPath) + ".corrupt-*"))
                {
                    TryDelete(file);
                }
            }
            catch
            {
                // Best effort cleanup only.
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Best effort cleanup only.
            }
        }
    }
}
