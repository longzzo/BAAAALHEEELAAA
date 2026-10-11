using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valhalla.Campaign
{
    public enum SceneRole { Legacy, Lobby, Village, Forest, Mine, Sanctum }
    public enum RoomKind { Approach, Salvage, Elite, Guardian }
    [Serializable] public sealed class SpawnSpec { public string monster; public float x; }
    [Serializable] public sealed class RoomPlan
    {
        public int id;
        public RoomKind kind;
        public string title;
        public int[] exits;
        public SpawnSpec[] enemies;
        public float[] hazards, crates;
        public int reward;
    }
    [Serializable] public sealed class DungeonPart
    {
        public string name, subtitle, material;
        public SceneRole scene;
        public RoomPlan[] rooms;
    }
    public interface IDungeonGenerator { DungeonPart[] Generate(int seed); }

    // Constrained graph generation: entry -> one of two authored encounter grammars -> guardian.
    // Randomness has a private seed and cannot be changed by audio or particle random calls.
    public sealed class SeededDungeonGenerator : IDungeonGenerator
    {
        public DungeonPart[] Generate(int seed)
        {
            var random = new System.Random(seed);
            string[] names = { "잿빛 숲", "부러진 광산", "망자의 성소" };
            string[] subtitles = { "가죽과 뼈가 남은 사냥터", "철을 캐던 자들의 무덤", "발할라의 문 앞에서" };
            string[] materials = { "raw_hide", "iron_scrap", "bone_shard" };
            var parts = new DungeonPart[3];
            for (int p = 0; p < 3; p++)
            {
                var part = new DungeonPart { name = names[p], subtitle = subtitles[p], material = materials[p], scene = (SceneRole)(p + 3), rooms = new RoomPlan[4] };
                for (int r = 0; r < 4; r++)
                {
                    int count = r == 1 ? 1 : r == 2 ? 3 : r == 3 ? (p == 2 ? 1 : 2) : 2;
                    var spawn = new SpawnSpec[count];
                    for (int e = 0; e < count; e++)
                        spawn[e] = new SpawnSpec { monster = r == 3 && p == 2 ? "bonebreaker" : r == 2 && e == 0 ? "iron_sentinel" : random.Next(2) == 0 ? "draugr" : "raider", x = 1 + e * 4.5f + (float)random.NextDouble() * 2 };
                    part.rooms[r] = new RoomPlan
                    {
                        id = r, kind = (RoomKind)r, title = r == 0 ? "문턱의 적들" : r == 1 ? "버려진 보급로" : r == 2 ? "피의 사냥길" : "수호자의 관문",
                        exits = r == 0 ? new[] { 1, 2 } : r < 3 ? new[] { 3 } : Array.Empty<int>(), enemies = spawn,
                        hazards = r == 1 ? Array.Empty<float>() : new[] { -3f + (float)random.NextDouble() * 3, 7f + (float)random.NextDouble() * 3 },
                        crates = new[] { -8f + (float)random.NextDouble() * 3, 13f + (float)random.NextDouble() * 2 }, reward = r == 2 ? 3 : r == 1 ? 2 : 1
                    };
                }
                parts[p] = part;
            }
            return parts;
        }
    }

    public interface ILootRule { string Item(string monster, string regionMaterial); int Quantity(string monster); }
    public sealed class MonsterLootRule : ILootRule
    {
        public string Item(string monster, string regionMaterial) => monster == "raider" ? "raw_hide" : monster == "iron_sentinel" ? "iron_scrap" : regionMaterial;
        public int Quantity(string monster) => monster == "bonebreaker" ? 5 : monster == "iron_sentinel" ? 3 : 1;
    }
    public sealed class ExpeditionState
    {
        public int seed, part, room, kills, parries;
        public float hp = 110, honor = 50;
        public bool settled;
        public bool recallUsed;
        public int dodges, eliteKills, bossKills;
        public float closeSeconds, retreatSeconds, criticalSeconds;
        public DamageKind fatalKind;
        public readonly HashSet<DamageKind> severeKinds=new HashSet<DamageKind>();
        public BodyPart? fatalPart;
        public string fatalSource;
        public BodyBuild body;
        public DungeonPart[] plan;
        public readonly List<ItemStack> loot = new List<ItemStack>();
        public RoomPlan Room => plan[part].rooms[room];
        public void AddLoot(string id, int count)
        {
            var stack = loot.Find(x => x.id == id);
            if (stack == null) loot.Add(new ItemStack { id = id, count = count }); else stack.count += count;
        }
    }
    public static class CampaignSession
    {
        public static EconomyService Economy { get; private set; }
        public static ExpeditionState Run;
        public static string lastReport = "잿빛 항구에 도착했다. 훈련장에서 첫 출정을 준비하라.";
        public static readonly IDungeonGenerator Generator = new SeededDungeonGenerator();
        public static readonly ILootRule Loot = new MonsterLootRule();
        public static bool Testing;
        public static void Ensure()
        {
            if (Economy != null) return;
            Economy = new EconomyService(new JsonProfileStore(System.IO.Path.Combine(Application.persistentDataPath, "campaign-v1.json")));
        }
        public static void UseStore(IProfileStore store) { Economy = new EconomyService(store); Run = null; lastReport="잿빛 항구에 도착했다. 훈련장에서 첫 출정을 준비하라."; }
        public static void NewExpedition(int? chosenSeed = null)
        {
            Ensure(); int seed = chosenSeed ?? unchecked(Environment.TickCount ^ (Economy.Profile.completedRuns * 397));
            Run = new ExpeditionState { seed = seed, plan = Generator.Generate(seed), body = BuildBody() };
            Economy.Profile.lastSeed = seed; Economy.Save();
        }
        public static BodyBuild BuildBody()
        {
            Ensure(); var body = new BodyBuild();
            foreach (string id in Economy.Profile.equippedTattoos)
            {
                var spec=TattooCatalog.Find(id);
                if(spec==null||!Economy.Profile.tattoos.Contains(id)||!TattooCatalog.IsUnlocked(Economy.Profile,id))continue;
                var d = ScriptableObject.CreateInstance<TattooDefinition>();
                d.power=spec.power; d.bodyPart=spec.part; d.title=id; body.tattoos.Add(d);
            }
            return body;
        }
        public static float WeaponMultiplier => EconomyCatalog.Gear(Economy.Profile.equippedWeapon)?.damageMultiplier ?? 1;
        public static float TraumaMultiplier(BodyPart part)
        {
            string gear = Economy.Profile.equippedArmor;
            bool protectedPart = gear == "leather_bracer" && part == BodyPart.RightArm || gear == "iron_greaves" && part == BodyPart.Legs;
            return protectedPart ? EconomyCatalog.Gear(gear).traumaMultiplier : 1;
        }
        public static void Settle(bool survived)
        {
            if (Run == null || Run.settled) return;
            Run.settled = true; var profile = Economy.Profile; int kept = 0, lost = 0, overflowCoins = 0;
            foreach (var stack in Run.loot)
            {
                int amount = survived ? stack.count : stack.count / 2; lost += stack.count - amount;
                var definition = EconomyCatalog.Item(stack.id); if (definition == null) continue;
                int fits = Mathf.Min(amount, definition.maxStack - Economy.Count(stack.id));
                if (fits > 0) { Economy.Add(stack.id, fits, out _); kept += fits; }
                int overflow = amount - fits; overflowCoins += overflow * definition.sellPrice;
            }
            int reward = survived ? 15 + Run.part * 15 + Mathf.RoundToInt(Run.honor * .2f) : Mathf.RoundToInt(Run.honor * .1f);
            profile.coins += reward + overflowCoins;
            profile.bestPart = Mathf.Max(profile.bestPart, survived ? Run.part + 1 : Run.part);
            if (survived && Run.part == 2 && Run.room == 3) profile.completedRuns++;
            if (!survived) { profile.deaths++; if (Run.body.arm.Stage >= 2 || Run.body.legs.Stage >= 2) profile.scarLevel = Mathf.Min(3, profile.scarLevel + 1); }
            bool armScar=Run.body.arm.Stage==3||!survived&&Run.fatalPart==BodyPart.RightArm;
            bool legScar=Run.body.legs.Stage==3||!survived&&Run.fatalPart==BodyPart.Legs;
            if(armScar)profile.armScars=Mathf.Min(99,profile.armScars+1);
            if(legScar)profile.legScars=Mathf.Min(99,profile.legScars+1);
            lastReport = (survived ? "귀환 완료" : "발키리의 심사") + $" · 재료 {kept}개 보관 · 은화 +{reward + overflowCoins}" + (lost > 0 ? $" · 미보관 재료 {lost}개 상실" : "") + (profile.scarLevel > 0 ? $" · 흉터 기억 {profile.scarLevel}" : "");
            if(armScar)lastReport+=$" · 팔의 기억 {profile.armScars}/2"+(profile.armScars==2?" — 메아리 문신 해금":"");
            if(legScar)lastReport+=$" · 다리의 기억 {profile.legScars}/2"+(profile.legScars==2?" — 서리 발걸음 해금":"");
            foreach(var part in BodyBuild.Parts)if(part!=BodyPart.RightArm&&part!=BodyPart.Legs&&(Run.body.Get(part).Stage==3||!survived&&Run.fatalPart==part)) {
                profile.AddScar(part);lastReport+=$" · {Game.PartName(part)}의 기억 {profile.Scars(part)}";
            }
            if(Run.severeKinds.Contains(DamageKind.Fire)||!survived&&Run.fatalKind==DamageKind.Fire)profile.fireScars=Mathf.Min(99,profile.fireScars+1);
            if(Run.severeKinds.Contains(DamageKind.Giant)||!survived&&Run.fatalKind==DamageKind.Giant)profile.giantScars=Mathf.Min(99,profile.giantScars+1);
            var judgment=Judge(survived);
            if(judgment.valorCandidate)profile.valorMarks=Mathf.Min(99,profile.valorMarks+1);
            if(judgment.survivorCandidate)profile.survivorMarks=Mathf.Min(99,profile.survivorMarks+1);
            profile.lastJudgment=judgment.title+" · "+judgment.reason;
            lastReport+=" · "+judgment.title;
            Economy.Save();
        }
        public static JudgmentResult Judge(bool survived,bool live=false)
        {
            if(Run==null)return JudgmentRules.Evaluate(new JudgmentEvidence());
            int severe=0;foreach(var part in BodyBuild.Parts)if(Run.body.Get(part).Stage==3)severe++;
            return JudgmentRules.Evaluate(new JudgmentEvidence{parries=Run.parries+(live&&Game.I?Game.I.parries:0),dodges=Run.dodges,eliteKills=Run.eliteKills,bossKills=Run.bossKills,severeLimbs=severe,closeSeconds=Run.closeSeconds,retreatSeconds=Run.retreatSeconds,criticalSeconds=Run.criticalSeconds,honor=live&&Game.I?Game.I.honor:Run.honor,survived=survived});
        }
    }
}
