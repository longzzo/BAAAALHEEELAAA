using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Valhalla.Campaign
{
    public enum VillagePanel { None, Trader, Forge, Tattoos, Equipment, Inventory, Routes, Journal, Training }
    public sealed class Station
    {
        public string name, subtitle;
        public float x;
        public VillagePanel panel;
    }
    // Scene-level coordinator; economy and generation are replaceable domain services.
    public sealed class CampaignDirector : MonoBehaviour
    {
        public static CampaignDirector I;
        public VillagePanel panel;
        public string message = "", interactPrompt = "";
        public int tutorialStep = -1;
        public bool roomClear, training;
        public DungeonGate gate;
        public PressureTrial pressureTrial;
        bool roomRewardGranted;
        public float tutorialWalk;
        public int cratesBroken, pickedUp;
        public readonly List<WorldPickup> pickups = new List<WorldPickup>();
        public readonly List<BreakableCache> caches = new List<BreakableCache>();
        public readonly List<GroundHazard> hazards = new List<GroundHazard>();
        public readonly Station[] stations = {
            new Station { name="작업대 · 대장간",subtitle="가공 / 제작",x=-14,panel=VillagePanel.Forge },
            new Station { name="푸른 문신소",subtitle="룬 각인",x=-7,panel=VillagePanel.Tattoos },
            new Station { name="부산물 상인",subtitle="재료 판매",x=0,panel=VillagePanel.Trader },
            new Station { name="장비 상점",subtitle="구매 / 장착",x=7,panel=VillagePanel.Equipment },
            new Station { name="훈련장",subtitle="배우기 / 다시 배우기",x=12,panel=VillagePanel.Training }
        };
        Game G => Game.I;
        public bool IsVillage => G.sceneRole == SceneRole.Village;
        public bool IsDungeon => G.sceneRole >= SceneRole.Forest;
        public string TutorialInstruction => tutorialStep == 0 ? "A / D로 움직이고 SPACE로 한 번 점프하세요."
            : tutorialStep == 1 ? "허수아비를 향해 우클릭 / K. 강공격을 명중시키고 장착한 문신의 효과를 확인하세요."
            : tutorialStep == 2 ? "붉은 예고 뒤 공격이 닿기 직전에 C로 패링하세요."
            : tutorialStep == 3 ? "적의 공격이 닿는 순간 SHIFT로 전진 회피하세요."
            : tutorialStep == 4 ? "오른팔 외상 30 돌파 · 분노 적응이 자동으로 적용되었다. E를 눌러 심각한 부상까지 훈련을 계속하세요."
            : "심각한 부상: 오른팔 문신 전체 비활성화. E를 눌러 훈련을 마치세요.";
        bool jumped;
        Transform roomMarker;
        float previousX;
        BodyBuild trainingBody;

        public void Initialize()
        {
            I = this; CampaignSession.Ensure();
            G.player.gameObject.AddComponent<PaintedCombatVisual>().Initialize(false);
            if (IsVillage)
            {
                G.body = CampaignSession.BuildBody();
                G.phase = Phase.Village; G.player.transform.position = new Vector3(-3, -.34f);
                message = CampaignSession.lastReport;
                foreach (var s in stations) Marker(s.x, NordicArt.Gold);
                Marker(18, NordicArt.Ice);
            }
            else if (IsDungeon)
            {
                if (CampaignSession.Run == null) CampaignSession.NewExpedition(1337);
                CampaignSession.Run.part = (int)G.sceneRole - 3;
                G.body = CampaignSession.Run.body;
                G.player.hp = CampaignSession.Run.hp;
                G.honor = CampaignSession.Run.honor;
                LoadRoom();
            }
            else { G.phase = Phase.Camp; G.player.gameObject.SetActive(false); }
        }
        void Marker(float x, Color c)
        {
            NordicArt.Block("Interaction marker", null, new Vector2(x,-1.38f),new Vector2(1.7f,.06f),c,5);
        }
        void Update()
        {
            if (!G) return;
            if (G.sceneRole == SceneRole.Lobby)
            {
                if (Input.GetKeyDown(KeyCode.Return)) EnterVillage(); return;
            }
            if (G.phase == Phase.Death || G.phase == Phase.Victory)
            {
                if (Input.GetKeyDown(KeyCode.Return)) ReturnVillage(G.phase == Phase.Victory); return;
            }
            if (Input.GetKeyDown(KeyCode.I) && !G.choice.HasValue) Toggle(VillagePanel.Inventory);
            if (Input.GetKeyDown(KeyCode.Escape) && !G.choice.HasValue)
            {
                if (panel != VillagePanel.None) ClosePanel(); else Open(VillagePanel.Journal);
                return;
            }
            if (G.Frozen) return;
            if (training) { UpdateTraining(); return; }
            interactPrompt = "";
            if (IsVillage)
            {
                foreach (var s in stations)
                    if (Mathf.Abs(G.player.transform.position.x - s.x) < 2.5f)
                    {
                        interactPrompt = "E · " + s.name;
                        if(s.panel==VillagePanel.Training&&!CampaignSession.Economy.Profile.tutorialComplete)interactPrompt="E · 훈련장에서 전투를 배우기";
                        if (Input.GetKeyDown(KeyCode.E)) Open(s.panel); return;
                    }
                if (G.player.transform.position.x > 15.5f)
                {
                    interactPrompt = "E · 던전 출정";
                    if (Input.GetKeyDown(KeyCode.E)) StartExpedition();
                }
            }
            else if (IsDungeon)
            {
                foreach (var p in pickups.ToArray()) if (p && Vector2.Distance(p.transform.position, G.player.transform.position) < 1.8f) p.Collect();
                if (!roomClear && G.enemies.Count == 0)
                {
                    roomClear = true;
                    if (!roomRewardGranted) { roomRewardGranted = true; CampaignSession.Run.AddLoot(CampaignSession.Run.plan[CampaignSession.Run.part].material, CampaignSession.Run.Room.reward); }
                    G.Notice("적을 제압했다", gate && !gate.IsOpen ? "왼쪽 지레를 E로 당기거나, 오른쪽 관문을 강공격으로 부수세요." : "남은 전리품을 모으고 오른쪽 출구에서 E로 이동하세요.", NordicArt.Gold,5);
                }
                if (gate != null && !gate.IsOpen)
                {
                    if (G.player.transform.position.x > 11f)
                    {
                        var pos = G.player.transform.position; pos.x = 11f; G.player.transform.position = pos;
                        if (G.player.velocity.x > 0) G.player.velocity.x = 0;
                    }
                    if (roomClear && Mathf.Abs(G.player.transform.position.x - gate.LeverX) < 2f)
                    {
                        interactPrompt = "E · 지레를 당겨 관문 열기 (소음 없음)";
                        if (Input.GetKeyDown(KeyCode.E)) gate.TryLever();
                    }
                    else if (Mathf.Abs(G.player.transform.position.x - gate.GateX) < 3f)
                    {
                        interactPrompt = roomClear ? $"K · 관문 파괴까지 강공격 {gate.HitsRemaining}회 · 지원군 2명\n← 지레를 E로 당기면 소음 없이 열림" : "먼저 주변의 적을 제압하라";
                    }
                }
                if(pressureTrial&&pressureTrial.Carrying)
                {
                    interactPrompt="V · 물체 투척 / E · 내려놓기 · 운반 중 이동 55%, 전투 행동 제한";
                    if(Input.GetKeyDown(KeyCode.E))pressureTrial.DropHeld();
                    return;
                }
                if (pressureTrial && !pressureTrial.Claimed && G.player.transform.position.x > -5 && G.player.transform.position.x < 12)
                {
                    interactPrompt = pressureTrial.Pressed ? "압력판 활성 · 오른쪽 보급함 근처에서 E로 재료 3개 회수" : "V로 상자·시체 들기 → E로 내려놓기 / V로 투척 · 금빛 압력판 위에 놓으세요";
                    if (Input.GetKeyDown(KeyCode.E)) pressureTrial.TryClaim();
                }
                if (roomClear && (gate == null || gate.IsOpen) && G.player.transform.position.x > 14)
                {
                    interactPrompt = "E · 다음 길 / 안전 귀환";
                    if (Input.GetKeyDown(KeyCode.E)) Open(VillagePanel.Routes);
                }
            }
        }
        public void Open(VillagePanel value)
        {
            if (G.choice.HasValue || training && value != VillagePanel.Inventory && value != VillagePanel.Journal) return;
            panel = value; G.paused = true; G.SyncPause(); message = "";
            var profile=CampaignSession.Economy.Profile;
            if(value==VillagePanel.Inventory)profile.hasViewedInventory=true;
            if(value==VillagePanel.Tattoos)profile.hasReviewedTattoos=true;
            if(value==VillagePanel.Equipment)profile.hasReviewedEquipment=true;
            CampaignSession.Economy.Save();
        }
        public void Toggle(VillagePanel value) { if (panel == value) ClosePanel(); else Open(value); }
        public bool UseSalve()
        {
            if (!IsDungeon || G.phase != Phase.Combat || G.choice.HasValue || G.player.hp >= 110) return false;
            if (!CampaignSession.Economy.Consume("healing_salve", 1, out _)) { message="회복 연고가 없습니다. 마을 대장간에서 제작하세요."; return false; }
            G.player.hp=Mathf.Min(110,G.player.hp+35);message="생명 35 회복 · 부상 외상과 문신 상태는 유지됩니다.";return true;
        }
        public void ClosePanel() { panel = VillagePanel.None; G.paused = false; G.SyncPause(); }
        public void EnterVillage() { Time.timeScale = 1; SceneManager.LoadScene("MainVillage"); }
        public void ToLobby() { CampaignSession.Economy.Save(); Time.timeScale = 1; SceneManager.LoadScene("Lobby"); }
        public void StartExpedition(int? seed = null)
        {
            CampaignSession.Economy.Profile.hasDeparted=true;
            ClosePanel(); CampaignSession.NewExpedition(seed); SceneManager.LoadScene("DungeonForest");
        }
        public void ReturnVillage(bool survived)
        {
            if (IsDungeon)
            {
                if (survived && !roomClear && G.phase != Phase.Victory) return;
                foreach (var p in pickups.ToArray()) if (p && roomClear && survived) p.Collect();
                CaptureRun(); CampaignSession.Settle(survived);
            }
            EnterVillage();
        }
        public void CaptureRun()
        {
            if (CampaignSession.Run == null) return;
            CampaignSession.Run.hp = G.player.hp; CampaignSession.Run.honor = G.honor; CampaignSession.Run.kills += G.kills; CampaignSession.Run.parries += G.parries;
            G.kills = G.parries = 0;
        }
        public void Travel(int nextRoom)
        {
            if (!roomClear) return;
            if (gate != null && !gate.IsOpen) return;
            var run = CampaignSession.Run;
            if (Array.IndexOf(run.Room.exits, nextRoom) < 0) return;
            foreach (var p in pickups.ToArray()) if (p) p.Collect();
            CaptureRun(); run.room = nextRoom; ClosePanel(); LoadRoom();
        }
        public void NextPart()
        {
            var run = CampaignSession.Run;
            if (!roomClear || run.room != 3) return;
            foreach (var p in pickups.ToArray()) if (p) p.Collect();
            CaptureRun();
            if (run.part == 2) { ClosePanel(); G.EndRun(true); return; }
            run.part++; run.room = 0;
            ClosePanel(); SceneManager.LoadScene(run.part == 1 ? "DungeonMine" : "DungeonSanctum");
        }
        public void LoadRoom()
        {
            ClearWorld(); var run = CampaignSession.Run; G.body = run.body; G.phase = Phase.Combat;
            G.player.ClearDelayedAttacks();
            G.player.transform.position = new Vector3(-14,-.34f); G.player.velocity = Vector2.zero; G.player.hp = run.hp;
            G.player.attackTimer = G.player.parryTimer = G.player.dashTimer = 0; G.player.invulnerability = .5f;
            G.wave = run.part + 1; roomClear = false; roomRewardGranted = false;
            foreach (var spec in run.Room.enemies)
            {
                var e = G.Spawn(spec.x, spec.monster == "bonebreaker", spec.monster == "raider");
                e.archetype = spec.monster;
                if (spec.monster == "iron_sentinel") { e.maxHp = e.hp = 150; e.elite = true; }
                e.gameObject.AddComponent<PaintedCombatVisual>().Initialize(true);
            }
            foreach (float x in run.Room.crates)
            {
                var obj = new GameObject("Salvage cache"); obj.transform.position = new Vector3(x,-.85f);
                var cache = obj.AddComponent<BreakableCache>(); cache.Initialize(this, run.plan[run.part].material); caches.Add(cache);
            }
            foreach (float x in run.Room.hazards)
            {
                var obj = new GameObject("Telegraphed ground hazard"); obj.transform.position = new Vector3(x,-1.35f);
                var hazard = obj.AddComponent<GroundHazard>(); hazard.Initialize(this, run.part); hazards.Add(hazard);
            }
            if (run.Room.kind == RoomKind.Approach)
            {
                var anchor = new GameObject("Dungeon gate anchor");
                gate = anchor.AddComponent<DungeonGate>(); gate.Initialize(this);
            }
            if (run.Room.kind == RoomKind.Salvage)
            {
                var puzzle = new GameObject("Pressure plate supplies");
                pressureTrial = puzzle.AddComponent<PressureTrial>();
                pressureTrial.Initialize(this, run.plan[run.part].material);
            }
            roomMarker = NordicArt.Block("Room exit", null, new Vector2(17,-1.38f),new Vector2(1.7f,.06f),NordicArt.Ice,5);
            G.Notice(run.plan[run.part].name + " / " + run.Room.title, "적을 무너뜨리면 다음 관문이 열립니다. I 소지품 · 재료 상자는 파괴 / 운반 상자는 압력판에 활용.", NordicArt.Gold,5);
        }
        void ClearWorld()
        {
            if(G.TattooFeedback)G.TattooFeedback.Clear();
            if(roomMarker) Destroy(roomMarker.gameObject);
            foreach (var e in G.enemies.ToArray()) if(e) Destroy(e.gameObject); G.enemies.Clear();
            foreach(var p in pickups.ToArray()) if(p) Destroy(p.gameObject); pickups.Clear();
            foreach(var c in caches.ToArray()) if(c) Destroy(c.gameObject); caches.Clear();
            foreach(var h in hazards.ToArray()) if(h) Destroy(h.gameObject); hazards.Clear();
            if(gate) Destroy(gate.gameObject); gate = null;
            if(pressureTrial) Destroy(pressureTrial.gameObject); pressureTrial = null;
        }
        public void GateBroken()
        {
            roomClear = false;
            foreach (float x in new[] { 7f, 10f })
            {
                var e = G.Spawn(x, false, true); e.archetype = "raider";
                e.gameObject.AddComponent<PaintedCombatVisual>().Initialize(true);
            }
        }
        public void EnemyDefeated(Enemy enemy)
        {
            if (training) return;
            if (!IsDungeon) return;
            if (pressureTrial) pressureTrial.AddRemains(enemy);
            string item = CampaignSession.Loot.Item(enemy.archetype,CampaignSession.Run.plan[CampaignSession.Run.part].material);
            Drop(enemy.transform.position,item,CampaignSession.Loot.Quantity(enemy.archetype));
        }
        public void Drop(Vector2 position,string item,int amount)
        {
            var o=new GameObject("Loot " + item); o.transform.position = new Vector3(position.x,-.8f);
            var p=o.AddComponent<WorldPickup>(); p.Initialize(this,item,amount); pickups.Add(p);
        }
        public void AttackProps(Vector2 origin,int facing,float reach,bool heavy=false)
        {
            if (pressureTrial) pressureTrial.Strike(origin, facing, reach, heavy);
            foreach(var c in caches.ToArray()) if(c && (c.transform.position.x-origin.x)*facing>=-.25f && (c.transform.position.x-origin.x)*facing<reach && Mathf.Abs(origin.y-c.transform.position.y)<2) c.Break();
            if(gate != null && !gate.IsOpen && (gate.GateX-origin.x)*facing>=-.25f && (gate.GateX-origin.x)*facing<reach && Mathf.Abs(origin.y+.34f)<2) gate.Strike(heavy);
        }
        public void BeginTraining()
        {
            if (training || !IsVillage) return;
            ClosePanel(); ClearWorld(); training = true; tutorialStep=0; tutorialWalk=0; jumped=false;
            trainingBody=new BodyBuild();
            foreach(var id in new[]{"thunder","ram"})
            {
                var spec=TattooCatalog.Find(id);
                var tattoo=ScriptableObject.CreateInstance<TattooDefinition>();
                tattoo.title=id;tattoo.power=spec.power;tattoo.bodyPart=spec.part;trainingBody.tattoos.Add(tattoo);
            }
            G.phase=Phase.Combat; G.body=trainingBody; G.player.ResetPlayer(); G.player.transform.position=new Vector3(-6,-.34f); previousX=-6;
            message="훈련용 천둥·질주 문신을 잠시 빌립니다. 소유·장착 상태는 바뀌지 않습니다. 훈련 중 사망하지 않습니다.";
        }
        void UpdateTraining()
        {
            if(tutorialStep==0)
            {
                tutorialWalk+=Mathf.Abs(G.player.transform.position.x-previousX); previousX=G.player.transform.position.x;
                if(G.player.transform.position.y>.5f) jumped=true;
                if(tutorialWalk>4 && jumped) { tutorialStep=1; TrainingEnemy(true); }
            }
            if(tutorialStep==1) foreach(var e in G.enemies) if(e) e.Stun(100);
            if(tutorialStep==4 && Input.GetKeyDown(KeyCode.E)) TrainSevere();
            else if(tutorialStep==5 && Input.GetKeyDown(KeyCode.E)) FinishTraining(true);
            G.player.hp=Mathf.Max(G.player.hp,60);
        }
        // Severe injury is a scripted lesson step, not an automatic trauma threshold: the player advances it with E.
        public void TrainSevere()
        {
            tutorialStep=5; G.player.TakeHit(25,BodyPart.RightArm,null,true);
            G.Notice("문신이 침묵했다", "심각한 부상은 강화가 아닌 손실입니다. E로 훈련 마무리.",NordicArt.Red,15);
        }
        void TrainingEnemy(bool dummy)
        {
            foreach(var e in G.enemies.ToArray()) if(e) Destroy(e.gameObject); G.enemies.Clear();
            G.player.transform.position=new Vector3(-3,-.34f); G.player.facing=1;
            var enemy=G.Spawn(-1.5f,false,false); enemy.hp=enemy.maxHp=9999; if(dummy) enemy.Stun(100);
            enemy.gameObject.AddComponent<PaintedCombatVisual>().Initialize(true);
        }
        public void Action(string action)
        {
            if(!training) return;
            if(tutorialStep==1 && action=="heavy-hit") { tutorialStep=2; TrainingEnemy(false); }
            else if(tutorialStep==2 && action=="parry") { tutorialStep=3; foreach(var e in G.enemies) { e.stun=.2f; e.timer=.8f; } }
            else if(tutorialStep==3 && action=="dodge")
            {
                // Automatic: TakeHit crosses the 30-trauma threshold and BodyState.AddTrauma applies Fury on its own, no pause.
                tutorialStep=4; foreach(var e in G.enemies.ToArray()) if(e) Destroy(e.gameObject); G.enemies.Clear();
                G.body.Reset(); G.player.hp=110; G.player.TakeHit(30,BodyPart.RightArm,null,true);
            }
        }
        public void FinishTraining(bool completed)
        {
            if(!training)return;
            bool firstCompletion=completed&&!CampaignSession.Economy.Profile.tutorialComplete;
            if(firstCompletion)
            {
                CampaignSession.Economy.Profile.tutorialComplete=true;
                CampaignSession.Economy.Add("raw_hide",4,out _);CampaignSession.Economy.Add("bone_shard",4,out _);CampaignSession.Economy.Add("iron_scrap",4,out _);
                CampaignSession.Economy.Save(); CampaignSession.lastReport="훈련 완료 · 제작 연습용 재료를 받았습니다. I로 확인하고 대장간에서 가공하세요.";
            }
            training=false; tutorialStep=-1; ClearWorld(); ReleaseTrainingBody(); G.body=CampaignSession.BuildBody(); G.player.ResetPlayer(); G.phase=Phase.Village; G.choice=null; ClosePanel();
            message=!completed?"훈련을 종료하고 원래 장착 상태로 돌아왔습니다. 다시 도전할 수 있습니다.":firstCompletion?CampaignSession.lastReport+" 원래 장착 상태로 돌아왔습니다.":"복습 완료 · 원래 장착 상태로 돌아왔습니다. 재료 보상은 첫 완료 때만 지급됩니다.";
        }
        void ReleaseTrainingBody()
        {
            if(trainingBody==null)return;
            foreach(var tattoo in trainingBody.tattoos)if(tattoo)Destroy(tattoo);
            trainingBody=null;
        }
        void OnDestroy() { ReleaseTrainingBody(); if(I==this) I=null; }
    }
    public sealed class WorldPickup:MonoBehaviour
    {
        public string item; public int amount; CampaignDirector owner; bool collected; Transform visual;
        public void Initialize(CampaignDirector director,string id,int count)
        {
            owner=director;item=id;amount=count;
            visual=NordicArt.Block("Loot",transform,Vector2.zero,new Vector2(.32f,.32f),NordicArt.Gold,35,45);
            NordicArt.Block("Glow",transform,Vector2.zero,new Vector2(.6f,.04f),NordicArt.Ice,34);
        }
        void Update() { if(visual) visual.localPosition=Vector3.up*(Mathf.Sin(Time.time*4+transform.position.x)*.08f); }
        public void Collect()
        {
            if(collected||CampaignSession.Run==null)return;collected=true;CampaignSession.Run.AddLoot(item,amount);owner.pickedUp+=amount;
            owner.pickups.Remove(this);Game.I.PlaySound(CombatCue.Pickup);Destroy(gameObject);
        }
    }
    public sealed class BreakableCache:MonoBehaviour
    {
        CampaignDirector owner; string material; bool broken;
        public void Initialize(CampaignDirector director,string item)
        {
            owner=director;material=item;
            if(PaintedCrateVisual.Attach(transform,.85f,13))return;
            NordicArt.Block("Wood",transform,Vector2.zero,new Vector2(.8f,.85f),NordicArt.Hex("68503C"),12);
            NordicArt.Block("Iron band",transform,new Vector2(0,.2f),new Vector2(.9f,.08f),NordicArt.Gold,13);
            NordicArt.Block("Iron band",transform,new Vector2(0,-.2f),new Vector2(.9f,.08f),NordicArt.Gold,13);
        }
        public void Break()
        {
            if(broken)return;broken=true;owner.cratesBroken++;owner.Drop(transform.position,material,2);Game.I.Burst(transform.position,NordicArt.Gold,12,4);owner.caches.Remove(this);Destroy(gameObject);
        }
    }
    public sealed class GroundHazard:MonoBehaviour
    {
        int region; float cycle,hitCooldown; SpriteRenderer sr;
        public bool Active=>cycle>2.5f;
        public bool IsFire=>region==1;
        public void Initialize(CampaignDirector owner,int part)
        {
            region=part;cycle=Mathf.Abs(transform.position.x)%2;
            sr=NordicArt.Block("Danger strip",transform,Vector2.zero,new Vector2(1.65f,.16f),NordicArt.Red,5).GetComponent<SpriteRenderer>();
        }
        void Update()
        {
            var g=Game.I;if(!g||g.Frozen||g.phase!=Phase.Combat)return;
            cycle=(cycle+Time.deltaTime)%3.5f;hitCooldown-=Time.deltaTime;
            sr.color=Active?(IsFire?new Color(1,.35f,.08f):NordicArt.Red):cycle>1.7f?NordicArt.Gold:new Color(.35f,.2f,.18f,.55f);
            if(Active&&hitCooldown<=0&&Mathf.Abs(g.player.transform.position.x-transform.position.x)<.85f&&g.player.Grounded)
            {g.player.TakeHit(region==2?9:7,BodyPart.Legs,null,false,IsFire?DamageKind.Fire:DamageKind.Physical);hitCooldown=.9f;}
        }
    }
}

