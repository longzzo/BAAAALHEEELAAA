using System;
using System.Linq;
using UnityEngine;

namespace Valhalla.Campaign
{
    // Presentation adapter: all transactions are delegated to the domain service.
    public sealed class CampaignHUD : MonoBehaviour
    {
        GUIStyle label, button;
        Texture2D portrait;
        readonly Color ink = NordicArt.Hex("101D25"), pale = NordicArt.Hex("EBE4D5"), muted = NordicArt.Hex("A8BAB7");
        Game G => Game.I;
        CampaignDirector D => CampaignDirector.I;
        EconomyService E => CampaignSession.Economy;
        string sellId = "raw_hide";
        int sellQuantity = 1;
        bool confirmSale;
        readonly float[] lastActivation=Enumerable.Repeat(-100f,Enum.GetValues(typeof(TattooPower)).Length).ToArray();
        BodyPart tattooPart=BodyPart.RightArm;
        bool CompactCombat => G.phase==Phase.Combat&&!D.training&&D.panel==VillagePanel.None&&!G.help;
        bool CompactVillage => D.IsVillage&&!D.training&&D.panel==VillagePanel.None&&!G.help;
        static readonly Rect InventoryButton=new Rect(1190,28,170,52), JournalButton=new Rect(1380,28,185,52), TrainingExitButton=new Rect(1345,265,220,42);
        // Compute from current state before Update; OnGUI runs too late to consume raw mouse-down.
        public static bool BlocksCombatPointer(Vector2 screenPosition)
        {
            var director=CampaignDirector.I;
            if(!director || !Game.I || Game.I.sceneRole==SceneRole.Legacy)return false;
            if(director.panel!=VillagePanel.None || Game.I.paused || Game.I.choice.HasValue)return true;
            var point=new Vector2(screenPosition.x*1600f/Screen.width,(Screen.height-screenPosition.y)*900f/Screen.height);
            return InventoryButton.Contains(point)||JournalButton.Contains(point)||(director.training&&TrainingExitButton.Contains(point));
        }
        void OnEnable() { TattooSignals.Activated+=OnTattooActivated; }
        void OnDisable() { TattooSignals.Activated-=OnTattooActivated; }
        void OnTattooActivated(TattooPower power) { lastActivation[(int)power]=Time.unscaledTime; }
        void Styles()
        {
            if (label != null) return;
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 22);
            label = new GUIStyle(GUI.skin.label) { font = font, wordWrap = true, padding = new RectOffset(0,0,0,0) };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 18, wordWrap = true, padding = new RectOffset(12,12,8,8) };
            portrait = Resources.Load<Texture2D>("Art/BarbarianConcept-v1");
        }
        void Box(float x,float y,float w,float h,Color c) { var old=GUI.color;GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=old; }
        void Text(float x,float y,float w,float h,string s,int size=18,Color? color=null,TextAnchor align=TextAnchor.UpperLeft)
        { label.fontSize=size;label.normal.textColor=color??pale;label.alignment=align;GUI.Label(new Rect(x,y,w,h),s,label); }
        bool Button(float x,float y,float w,float h,string s,bool enabled=true)
        {
            Box(x,y,w,h,enabled?NordicArt.Hex("2A4249"):NordicArt.Hex("243036"));Box(x,y,3,h,enabled?NordicArt.Gold:muted);
            var bg=GUI.backgroundColor;GUI.backgroundColor=Color.clear;bool prev=GUI.enabled;GUI.enabled=prev&&enabled;
            button.normal.textColor=pale;button.hover.textColor=NordicArt.Gold;bool clicked=GUI.Button(new Rect(x,y,w,h),s,button);
            GUI.enabled=prev;GUI.backgroundColor=bg;return clicked;
        }
        bool Button(Rect rect,string text,bool enabled=true) => Button(rect.x,rect.y,rect.width,rect.height,text,enabled);
        void Bar(float x,float y,float w,float ratio,Color color) { Box(x,y,w,7,NordicArt.Hex("334147"));Box(x,y,w*Mathf.Clamp01(ratio),7,color); }
        public static string Name(string id)
        {
            switch(id) {
                case "raw_hide":return "거친 가죽";case "bone_shard":return "뼛조각";case "iron_scrap":return "녹슨 쇳조각";
                case "tanned_leather":return "무두질 가죽";case "bone_fitting":return "뼈 보강재";case "refined_iron":return "정제 철";
                case "healing_salve":return "회복 연고";case "starter_axe":return "낡은 도끼";case "iron_axe":return "단조 도끼";
                case "runic_axe":return "룬 도끼";case "leather_bracer":return "가죽 손목 보호대";case "iron_greaves":return "철제 정강이 보호대";
                case "thunder":return "천둥의 각인";case "blood":return "피의 룬";case "ram":return "질주의 룬";default:return id;
            }
        }
        void OnGUI()
        {
            if(!G||!D||E==null)return;Styles();
            var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/900f,1));
            if(G.sceneRole==SceneRole.Lobby) { Lobby();GUI.matrix=old;return; }
            Header();WorldLabels();
            if(CompactCombat) {
                Box(0,858,1600,42,new Color(.035f,.065f,.085f,.86f));
                Text(30,869,370,24,"K · "+CombatStatus(G.player,Barbarian.CombatInput.Heavy),15,NordicArt.Gold);
                Text(420,869,340,24,"C · "+CombatStatus(G.player,Barbarian.CombatInput.Parry),15,NordicArt.Ice);
                Text(790,869,420,24,"SHIFT · "+CombatStatus(G.player,Barbarian.CombatInput.Dash),15,NordicArt.Ice);
                Text(1290,869,280,24,"TAB · 문신 / 신체 / 조작",14,muted);
                if(G.player.HasBufferedInput)Text(520,829,560,24,"입력 예약 · "+(G.player.BufferedInput==Barbarian.CombatInput.Parry?"패링":G.player.BufferedInput==Barbarian.CombatInput.Dash?"대시":G.player.BufferedInput==Barbarian.CombatInput.Heavy?"강공격":"일반 공격"),14,NordicArt.Gold,TextAnchor.MiddleCenter);
                if(G.player.Holding)Text(440,790,720,28,G.player.CarryingProp?"V 던지기 · E 내려놓기":"V 던지기 · 방패 유지 "+G.player.grapple.HoldRemaining.ToString("0.0")+"초",18,NordicArt.Gold,TextAnchor.MiddleCenter);
            } else if(CompactVillage) {
                Box(0,858,1600,42,new Color(.035f,.065f,.085f,.86f));
                Text(30,869,1540,24,"A / D 이동     E 시설 이용     I 소지품     TAB 준비 상태 / 상세 안내     ESC 메뉴",16,muted,TextAnchor.MiddleCenter);
            } else {
            Box(0,822,1600,78,new Color(.035f,.065f,.085f,.97f));
            Text(30,837,1540,26,"A D 이동   SPACE 점프   J 일반   K 강공격   SHIFT 회피   C 패링   Q 박치기   V 잡기 / 투척   E 상호작용   I 소지품",17,null,TextAnchor.MiddleCenter);
            string queued=G.player.BufferedInput==Barbarian.CombatInput.Heavy?"K 강공격":G.player.BufferedInput==Barbarian.CombatInput.Light?"J 일반 공격":G.player.BufferedInput==Barbarian.CombatInput.Parry?"C 패링":"SHIFT 대시";
            Text(30,872,1540,22,G.player.HasBufferedInput?"입력 보관 · "+queued+" · 동작이 끝나면 실행 / 너무 이르면 만료":"전투 중 자동으로 적을 바라봅니다. 대시는 이동 입력 우선, 무입력은 바라보는 쪽.   강공격: 즉시 준비 → 타격   ·   ESC 메뉴",13,G.player.HasBufferedInput?NordicArt.Gold:muted,TextAnchor.MiddleCenter);
            if(G.help)Text(1240,790,330,24,"TAB · 상세 정보 접기",14,NordicArt.Gold);
            }
            if(D.training) {
                Box(385,135,830,100,ink);Text(410,147,780,24,"훈련 / "+(D.tutorialStep+1)+" 단계 · 천둥·질주 임시 대여 / 종료 시 원래 장착 복원",15,NordicArt.Gold);
                Text(410,181,780,46,D.TutorialInstruction,20);
                if(Button(TrainingExitButton,"훈련 나가기"))D.FinishTraining(false);
            } else if(D.panel==VillagePanel.None&&!G.choice.HasValue) {
                var guide=D.IsVillage?VillageGuide.Current(E.Profile):null;
                if(guide!=null) {
                    string direction=guide.targetX.HasValue?(System.Math.Abs(guide.targetX.Value-G.player.transform.position.x)<2.5f?"E · 가까운 시설 이용":guide.targetX.Value>G.player.transform.position.x?"오른쪽으로 →":"← 왼쪽으로"):"I · 소지품 열기";
                    if(CompactVillage) {
                        string destination=guide.targetX.HasValue?D.stations.FirstOrDefault(s=>Mathf.Abs(s.x-guide.targetX.Value)<.1f)?.name??"출정 관문":"소지품";
                        Text(32,112,850,30,$"첫 출정 {guide.step}/7 · {guide.title}",18,NordicArt.Gold);
                        Text(32,146,880,26,destination+" · "+direction+"     TAB 자세히 / 자유 순서",14,muted);
                    } else {
                    Box(385,135,830,150,ink);Text(410,147,780,27,$"첫 출정 준비 {guide.step} / 7 · {guide.title}",20,NordicArt.Gold);
                    Text(410,185,780,55,guide.instruction,18);
                    Text(410,253,780,25,direction+"   ·   순서와 관계없이 자유롭게 진행할 수 있습니다.",14,muted);
                    }
                }
                else if(Time.unscaledTime<G.noticeUntil) { Text(390,CompactCombat?106:135,820,40,G.noticeTitle,CompactCombat?19:26,NordicArt.Gold,TextAnchor.MiddleCenter);if(!CompactCombat)Text(390,180,820,56,G.noticeText,17,null,TextAnchor.UpperCenter); }
                if(!string.IsNullOrEmpty(D.interactPrompt)) { float y=CompactVillage?783:720;Box(390,y,820,CompactVillage?58:78,ink);Text(405,y+9,790,CompactVillage?44:60,D.interactPrompt,CompactVillage?18:21,NordicArt.Gold,TextAnchor.MiddleCenter); }
                if(D.IsVillage&&!string.IsNullOrEmpty(D.message)&&!CompactVillage) { Box(390,305,820,90,ink);Text(408,316,785,72,D.message,15); }
            }
            if(G.phase==Phase.Death||G.phase==Phase.Victory)Result();
            
            else if(D.panel!=VillagePanel.None)Panel();
            GUI.matrix=old;
        }
        void Header()
        {
            if(CompactCombat) { CompactHeader();return; }
            if(CompactVillage) {
                Box(0,0,1600,94,new Color(.035f,.065f,.085f,.82f));
                Text(30,20,310,30,"잿빛 항구",22,NordicArt.Gold);Text(30,54,310,22,"가공 · 제작 · 문신 · 출정 준비",14,muted);
                Text(370,27,220,30,$"은화 {E.Profile.coins}",22,NordicArt.Gold);
                Text(640,20,490,28,Name(E.Profile.equippedWeapon),18);Text(640,53,490,24,$"장착 문신 {E.Profile.equippedTattoos.Count}개 · TAB 상세",14,muted);
                if(Button(InventoryButton,"소지품 / I"))D.Toggle(VillagePanel.Inventory);
                if(Button(JournalButton,"메뉴 / ESC"))D.Toggle(VillagePanel.Journal);
                if(!string.IsNullOrEmpty(D.message))Text(1020,112,535,28,"최근 결과 · TAB으로 확인",14,NordicArt.Gold,TextAnchor.UpperRight);
                return;
            }
            Box(0,0,1600,116,new Color(.035f,.065f,.085f,.96f));
            string place=D.IsVillage?"잿빛 항구":CampaignSession.Run?.plan[CampaignSession.Run.part].name??"출정";
            Text(30,18,310,35,place,27,NordicArt.Gold);Text(30,66,315,28,D.IsVillage?"대장간 · 문신소 · 상인 · 훈련장":$"{CampaignSession.Run.Room.title}  |  시드 {CampaignSession.Run.seed}",14,muted);
            Text(370,20,280,30,$"생명 {G.player.hp:0} / 110",19);Bar(370,62,250,G.player.hp/110f,NordicArt.Gold);
            Text(660,20,230,30,$"명예 {G.honor:0}",19);Bar(660,62,180,G.honor/100f,NordicArt.Ice);
            Text(905,20,215,28,$"은화 {E.Profile.coins}",21,NordicArt.Gold);Text(905,60,270,38,Name(E.Profile.equippedWeapon),15,muted);
            if(G.phase==Phase.Combat) {
                Text(370,86,280,24,"K · "+CombatStatus(G.player,Barbarian.CombatInput.Heavy),14,NordicArt.Gold);
                Text(660,86,230,24,"C · "+CombatStatus(G.player,Barbarian.CombatInput.Parry),14,NordicArt.Ice);
                Text(905,86,275,24,"SHIFT · "+CombatStatus(G.player,Barbarian.CombatInput.Dash),14,NordicArt.Ice);
            }
            bool controls=!G.choice.HasValue&&G.phase!=Phase.Death&&G.phase!=Phase.Victory;
            if(Button(InventoryButton,"소지품 / I",controls))D.Toggle(VillagePanel.Inventory);
            if(Button(JournalButton,"기록 / 메뉴",controls))D.Toggle(VillagePanel.Journal);
            TattoosStatus();
            Box(30,453,275,214,ink);Text(44,462,245,25,"신체 · 외상 / 55",17,NordicArt.Gold);
            int bodyRow=0;foreach(var part in BodyBuild.Parts){var b=G.body.Get(part);float y=491+bodyRow++*24;var c=b.Stage==3?NordicArt.Red:b.Stage==2?NordicArt.Gold:muted;
                Text(44,y,245,22,$"{Game.PartName(part)} · {b.trauma:0} · "+new[]{"정상","경고","자동 적응","문신 침묵"}[b.Stage],13,c);
            }
            if(G.phase==Phase.Combat){
                Box(30,678,275,102,ink);
                string status=G.player.Headbutting?"진행 중":G.player.Holding?"운반 중 제한":G.player.hp<=4?"생명 부족":G.player.headbuttCooldown>0?$"{G.player.headbuttCooldown:0.0}초":"사용 가능";
                Text(44,690,245,25,"Q · 박치기 · "+status,17,NordicArt.Gold);
                Text(44,725,245,46,"생명 4 소모 · 근접 적 행동 중단\n빗나가도 소모 · 무적 없음",13,muted);
                Box(1240,135,325,113,ink);
                string grip=G.player.CarryingProp?"물체 운반 · V 투척 / E 놓기":G.player.Holding?$"V · 투척 · {G.player.grapple.HoldRemaining:0.0}초 남음":G.body.Get(BodyPart.LeftArm).Stage==3?"잡기 불가 · 왼팔 심각":G.player.grapple.Cooldown>0?$"V · 잡기 · {G.player.grapple.Cooldown:0.0}초":"V · 잡기 / 인간 방패";
                Text(1254,147,297,28,grip,18,NordicArt.Gold);
                Text(1254,182,297,57,G.player.CarryingProp?"이동 55% · 공격과 점프 제한\n방패 효과 없음 · 피격 시 떨어뜨립니다.":G.player.Holding?"앞에서 오는 공격 한 번을 대신 맞습니다.\nV로 적을 향해 던지세요.":"작은 적은 약화·경직 시 잡기\n보급로 상자·시체도 V로 들어 옮깁니다.",14,muted);
            }
        }
        void CompactHeader()
        {
            Box(0,0,1600,94,new Color(.035f,.065f,.085f,.82f));
            Text(30,20,310,28,CampaignSession.Run?.plan[CampaignSession.Run.part].name??"출정",22,NordicArt.Gold);
            Text(30,54,310,22,CampaignSession.Run?.Room.title??"",14,muted);
            Text(370,20,300,27,$"생명 {G.player.hp:0} / 110",20);Bar(370,60,340,G.player.hp/110f,NordicArt.Gold);
            Text(755,24,205,26,$"명예 {G.honor:0}",18,NordicArt.Ice);
            if(Button(InventoryButton,"소지품 / I"))D.Toggle(VillagePanel.Inventory);
            if(Button(JournalButton,"메뉴 / ESC"))D.Toggle(VillagePanel.Journal);
            var injured=G.body.States.Where(b=>b.Stage>=2).ToArray();
            if(injured.Length>0) {
                string status=string.Join(" · ",injured.Select(b=>Game.PartName(b.part)+(b.Stage==3?" 침묵":" 적응")));
                Text(30,110,320,60,status,15,injured.Any(b=>b.Stage==3)?NordicArt.Red:NordicArt.Gold);
            }
            var active=G.body.tattoos.Where(t=>G.body.Has(t.power)&&Time.unscaledTime-lastActivation[(int)t.power]<.8f).Take(2);
            int row=0;foreach(var tattoo in active)Text(30,180+row++*26,300,26,(TattooCatalog.All.FirstOrDefault(t=>t.power==tattoo.power)?.name??tattoo.title)+" · 발동",15,NordicArt.Ice);
        }
        // Read-only action feedback: report the actual gate instead of implying that cooldown alone means ready.
        static string CombatStatus(Barbarian player,Barbarian.CombatInput action)
        {
            if(Game.I.paused)return "메뉴 정지";
            if(player.Holding)return "운반 중 제한";
            if(player.Attacking) {
                if(action!=Barbarian.CombatInput.Heavy&&player.CanExitRecovery)return "방어 전환 가능";
                return player.AttackWindupRemaining>0?$"공격 준비 {player.AttackWindupRemaining:0.0}초":$"공격 후딜 {player.attackTimer:0.0}초";
            }
            if(player.Headbutting)return "박치기 동작 중";
            if(player.Dodging)return "대시 중";
            if(action==Barbarian.CombatInput.Parry && player.Parrying)return $"방어 판정 {player.parryTimer:0.0}초";
            float cooldown=action==Barbarian.CombatInput.Parry?player.parryCooldown:action==Barbarian.CombatInput.Dash?player.dashCooldown:0;
            if(cooldown>0)return $"재사용 {cooldown:0.0}초";
            if(player.Parrying && action!=Barbarian.CombatInput.Dash)return "패링 동작 중";
            return "준비됨";
        }
        void TattoosStatus()
        {
            Box(30,137,275,300,new Color(.05f,.09f,.12f,.94f));
            Text(44,150,245,28,"새긴 문신",20,NordicArt.Gold);
            var tattoos=G.body.tattoos;
            if(tattoos.Count==0)Text(44,202,245,110,"장착한 문신이 없습니다.\n마을 문신소에서 몸에 새길 능력을 구성하세요.",16,muted);
            for(int i=0;i<tattoos.Count&&i<8;i++) {
                var tattoo=tattoos[i];var spec=TattooCatalog.All.FirstOrDefault(t=>t.power==tattoo.power);
                bool active=G.body.Has(tattoo.power);float y=184+i*31;
                bool triggered=active&&Time.unscaledTime-lastActivation[(int)tattoo.power]<.8f;
                var color=triggered?NordicArt.Gold:active?NordicArt.Ice:NordicArt.Red;
                Box(44,y+3,4,23,color);
                Text(56,y,239,17,(spec?.name??tattoo.title)+(triggered?" · 발동!":""),13,color);
                Text(56,y+16,239,15,active?G.player.tattooEffects.StateHint(tattoo.power):Game.PartName(tattoo.bodyPart)+" 심각 · 문신 침묵",11,active?muted:NordicArt.Red);
                if(Event.current.type==EventType.Repaint&&new Rect(44,y,250,29).Contains(Event.current.mousePosition)) {
                    Box(320,y,475,74,ink);Text(334,y+8,447,60,active?spec?.description??tattoo.title:"심각한 부상 · 해당 부위 문신 전체 침묵 · 다음 출정에 복구",15);
                }
            }
        }
        void Body(float x,float y,BodyState b)
        {
            var color=b.Stage==3?NordicArt.Red:b.Stage==2?NordicArt.Gold:NordicArt.Ice;
            Box(x,y,275,100,new Color(.05f,.09f,.12f,.9f));Text(x+14,y+10,248,25,Game.PartName(b.part)+" · "+new[]{"정상","손상 경고","부상","심각한 부상"}[b.Stage],17,color);
            Bar(x+14,y+42,245,b.trauma/55f,color);
            Text(x+14,y+55,250,38,b.Stage==3?(b.part==BodyPart.RightArm?"문신 침묵 · 후딜 ×1.9 / 피해 ×0.7":"문신 침묵 · 이동 ×0.48 / 대시 제한"):b.adaptation==Adaptation.Fury?(b.part==BodyPart.RightArm?"강공격 +60% · 후딜 +40%\n이 부위 피격 피해 +20%":"이동 -30% · 대시 강화\n이 부위 피격 피해 +20%"):b.adaptation==Adaptation.Endure?"인내 적응 · 기능 저하 완화":b.Stage==1?"기능은 유지 · 외상 30부터 자동 적응":$"외상 {b.trauma:0} / 55 · 30에서 자동 적응",13,muted);
        }
        void WorldLabels()
        {
            if(G.phase==Phase.Combat && D.panel==VillagePanel.None && !G.player.Holding && !G.enemies.Any(e=>e&&e.Alive&&!e.Captured&&Vector2.Distance(e.transform.position,G.player.transform.position)<4)) {
                var nearCache=D.caches.Where(c=>c&&Vector2.Distance(c.transform.position,G.player.transform.position)<2.5f)
                    .OrderBy(c=>Vector2.Distance(c.transform.position,G.player.transform.position)).FirstOrDefault();
                var trial=D.pressureTrial;
                float crateDistance=trial&&!trial.Carrying&&!trial.Flying?Mathf.Abs(trial.CrateX-G.player.transform.position.x):float.MaxValue;
                bool showWeight=crateDistance<2.5f && (nearCache==null || crateDistance<Vector2.Distance(nearCache.transform.position,G.player.transform.position));
                if(showWeight || nearCache) {
                    var world=showWeight?new Vector3(trial.CrateX,2f):nearCache.transform.position+Vector3.up*2.85f;
                    var screen=G.cam.WorldToViewportPoint(world);float x=Mathf.Clamp(screen.x*1600,440,1430),y=(1-screen.y)*900;
                    if(trial && Mathf.Abs(world.x-9)<3)y-=62; // Leave room for the vault's persistent E label.
                    Box(x-120,y-12,240,46,ink);
                    Text(x-114,y-7,228,38,showWeight?"운반 상자 · V 들기\nK 밀기 · 압력판에 놓기":"재료 상자 · J / K 파괴\n부산물 획득",13,showWeight?NordicArt.Ice:NordicArt.Gold,TextAnchor.MiddleCenter);
                }
            }
            if(D.pressureTrial)
            {
                var trial=D.pressureTrial;
                Box(1240,260,325,105,ink);
                Text(1254,272,297,27,trial.Claimed?"보급함 · 회수 완료":trial.Pressed?"압력판 · 활성":"압력판 · 무게 필요",18,trial.Pressed?NordicArt.Ice:NordicArt.Gold);
                Text(1254,306,297,51,trial.Flying?"투척 중 · 첫 적 명중 피해 24\n착지한 물체는 다시 옮길 수 있습니다.":"V 들기·투척 / E 내려놓기 / K 밀치기\n압력판에 놓고 보급함에서 E",14,muted);
                var p=G.cam.WorldToViewportPoint(new Vector3(trial.PlateX,-.5f));
                Text(p.x*1600-65,(1-p.y)*900-35,130,25,"압력판",14,NordicArt.Gold,TextAnchor.MiddleCenter);
                p=G.cam.WorldToViewportPoint(new Vector3(9,1.2f));
                Text(p.x*1600-90,(1-p.y)*900-30,180,25,trial.Claimed?"회수 완료":"E · 봉인 보급함",14,pale,TextAnchor.MiddleCenter);
            }
            if(D.IsVillage&&!D.training) foreach(var station in D.stations) {
                var p=G.cam.WorldToViewportPoint(new Vector3(station.x,2.4f));float x=p.x*1600,y=(1-p.y)*900;
                if(x<330||x>1500)continue;
                Box(x-95,y,190,54,new Color(.03f,.06f,.08f,.88f));Text(x-90,y+6,180,24,station.name,16,NordicArt.Gold,TextAnchor.MiddleCenter);Text(x-90,y+32,180,20,station.subtitle,12,muted,TextAnchor.MiddleCenter);
            }
            var placedLabels=new System.Collections.Generic.List<Vector2>();
            foreach(var e in G.enemies) { if(!e||!e.Alive)continue;var p=G.cam.WorldToViewportPoint(e.transform.position+Vector3.up*(e.boss?3.1f:1.9f));float x=p.x*1600,y=(1-p.y)*900;
                string edge=x<435?"← ":x>1470?"→ ":"";
                x=Mathf.Clamp(x,435,1470);
                while(placedLabels.Any(pos=>Mathf.Abs(pos.x-x)<250&&Mathf.Abs(pos.y-y)<62))y-=62;
                placedLabels.Add(new Vector2(x,y));
                Bar(x-48,y,96,e.hp/e.maxHp,e.boss?NordicArt.Gold:NordicArt.Red);
                bool parryCue=e.SecondsUntilThreat(G.player)<=Mathf.Min(.32f,G.body.ParryWindow-.07f);
                string title=e.Captured?"붙잡힘 / 투척":parryCue?"지금 C · 패링!":e.state==1?"예고 · "+Game.PartName(e.target):!e.boss&&!e.elite&&(e.hp<=e.maxHp*.5f||e.stun>0)?"V · 잡기 가능":e.boss?"뼈를 부수는 자":e.elite?"철의 파수꾼":e.charger?"약탈자":"망자";
                if(parryCue)Box(x-122,y-44,244,39,new Color(.06f,.08f,.08f,.94f));
                Text(x-115,y-35,230,30,edge+title,parryCue?20:16,parryCue?NordicArt.Gold:e.state==1?NordicArt.Red:pale,TextAnchor.MiddleCenter);
                if(e.state==1&&!e.Captured)Bar(x-48,y+11,96,1-Mathf.Clamp01(e.timer/e.WindupDuration),parryCue?NordicArt.Gold:NordicArt.Red);
            }
        }
        void Lobby()
        {
            Box(0,0,1600,900,new Color(.015f,.035f,.05f,.76f));
            if(portrait)GUI.DrawTexture(new Rect(925,110,620,720),portrait,ScaleMode.ScaleToFit,true);
            Text(110,115,900,35,"BAAAALHEEELAAA  /  THE ASHEN COAST",18,NordicArt.Gold);
            Text(104,195,1000,160,"발할라는,\n아직 멀었다.",64);
            Text(110,403,760,110,"한 방의 무게. 문신의 힘.\n마을에서 준비하고, 세 개의 어둠을 건너라.",25,muted);
            if(Button(110,575,440,70,"잿빛 항구로    /    ENTER"))D.EnterVillage();
            if(Button(110,672,210,52,"게임 종료"))Application.Quit();
            Text(110,778,1100,60,$"저장된 여정  ·  은화 {E.Profile.coins}  /  완주 {E.Profile.completedRuns}  /  팔의 기억 {E.Profile.armScars} · 다리의 기억 {E.Profile.legScars}\nPC 프로토타입 · 문신 능력 / 묵직한 전투 / 마을과 제작",16,muted);
        }
        void Shade(string title,string subtitle)
        {
            Box(0,116,1600,706,new Color(.01f,.025f,.04f,.76f));Box(325,140,1230,660,ink);Box(325,140,1230,3,NordicArt.Gold);
            Text(355,163,960,46,title,31,NordicArt.Gold);Text(355,215,1110,55,subtitle,16,muted);
        }
        void Panel()
        {
            string title=D.panel==VillagePanel.Forge?"대장간 · 가공과 제작":D.panel==VillagePanel.Trader?"부산물 상인":D.panel==VillagePanel.Tattoos?"푸른 문신소":D.panel==VillagePanel.Equipment?"장비 상점":D.panel==VillagePanel.Inventory?"전사의 소지품":D.panel==VillagePanel.Routes?"다음 길을 선택하라":D.panel==VillagePanel.Training?"훈련장":"전사의 기록";
            Shade(title,"시간이 멈췄습니다. 준비를 마치면 여정을 계속하세요.");
            if(Button(1380,165,140,42,"닫기 / ESC")){confirmSale=false;D.ClosePanel();return;}
            switch(D.panel) {
                case VillagePanel.Forge:Forge();break;case VillagePanel.Trader:Trader();break;case VillagePanel.Equipment:Equipment();break;
                case VillagePanel.Tattoos:Tattoos();break;case VillagePanel.Inventory:Inventory();break;case VillagePanel.Routes:Routes();break;
                case VillagePanel.Training:Training();break;case VillagePanel.Journal:Journal();break;
            }
            Text(355,750,1140,40,D.message,16,NordicArt.Gold);
        }
        void Forge()
        {
            int row=0;
            foreach(var r in EconomyCatalog.Recipes) {
                float y=280+row++*73;string cost=string.Join(" + ",r.costs.Select(c=>Name(c.id)+$" {c.count} (보유 {E.Count(c.id)})"));
                Text(355,y,300,30,Name(r.resultId),21);Text(680,y,560,50,cost+$"\n제작비 {r.fee} 은화",15,muted);
                if(Button(1310,y,205,51,"제작")) {bool ok=E.Craft(r.id,out _);D.message=ok?Name(r.resultId)+" 제작 완료":"재료·은화·보유 장비 또는 보관 한도를 확인하세요.";}
            }
        }
        void Trader()
        {
            int row=0;foreach(var item in EconomyCatalog.Items) {
                float y=277+row++*52;Text(355,y,290,30,Name(item.id),19);Text(650,y,270,30,$"보유 {E.Count(item.id)} · 개당 {item.sellPrice} 은화",16,muted);
                if(Button(1010,y,180,38,"판매 수량 선택",E.Count(item.id)>0)){sellId=item.id;sellQuantity=1;confirmSale=true;}
            }
            if(!confirmSale){Text(355,672,1120,45,"아이템을 고른 뒤 수량과 총액을 확인하고 판매합니다.",17,muted);return;}
            int max=E.Count(sellId);sellQuantity=Mathf.Clamp(sellQuantity,1,Mathf.Max(1,max));
            Text(355,669,390,38,Name(sellId)+$"  {sellQuantity}개 / {sellQuantity*EconomyCatalog.Item(sellId).sellPrice} 은화",18,NordicArt.Gold);
            if(Button(770,666,50,40,"−"))sellQuantity=Mathf.Max(1,sellQuantity-1);
            if(Button(833,666,50,40,"+"))sellQuantity=Mathf.Min(max,sellQuantity+1);
            if(Button(896,666,110,40,"전부"))sellQuantity=max;
            if(Button(1030,666,220,40,"확인 · 판매",max>0)){bool ok=E.Sell(sellId,sellQuantity,out _);D.message=ok?"판매 완료 · 재료와 은화를 저장했습니다.":"판매할 수량을 확인하세요.";confirmSale=false;}
            if(Button(1270,666,130,40,"취소"))confirmSale=false;
        }
        void Equipment()
        {
            int row=0;foreach(var gear in EconomyCatalog.Equipment) {
                float y=280+row++*83;bool owned=E.Profile.ownedEquipment.Contains(gear.id);bool equipped=E.Profile.equippedArmor==gear.id||E.Profile.equippedWeapon==gear.id;
                Text(355,y,360,33,Name(gear.id),22);Text(355,y+35,780,34,gear.slot=="weapon"?$"도끼 피해 ×{gear.damageMultiplier:0.00}":$"{(gear.id=="leather_bracer"?"오른팔":"다리")} 외상 누적 ×{gear.traumaMultiplier:0.00} · 부위와 문신을 더 오래 보호",15,muted);
                if(Button(1220,y,295,55,equipped?"장착 중":owned?"장착":$"구매 · {gear.price} 은화",!equipped)) {
                    bool ok=owned?E.Equip(gear.id,out _):E.BuyEquipment(gear.id,out _);D.message=ok?(owned?"장착 완료":"구매 완료 · 장착 버튼으로 적용하세요."):"은화 또는 소유 상태를 확인하세요.";
                }
            }
            if(Button(1220,697,295,42,"방어구 해제 · 부위 노출",E.Profile.equippedArmor!="none")) { E.UnequipArmor(out var result);D.message=result; }
            Text(355,700,830,49,"방어구는 해당 부위의 외상 누적을 늦춥니다. 해제하면 보호도 사라지고 부상 적응에도 빨리 도달합니다. 생명이나 쌓인 외상은 바뀌지 않습니다.",15,muted);
        }
        void Tattoos()
        {
            int partIndex=0;foreach(var part in BodyBuild.Parts)if(Button(355+partIndex++*163,270,151,42,Game.PartName(part)+$" {TattooCatalog.UsedArea(E.Profile,part)}/{TattooCatalog.Capacity(part)}"))tattooPart=part;
            Text(355,321,1130,25,$"{Game.PartName(tattooPart)} · 면적 {TattooCatalog.UsedArea(E.Profile,tattooPart)} / {TattooCatalog.Capacity(tattooPart)} · {TattooCatalog.ProgressHint(E.Profile,tattooPart)}",18,NordicArt.Gold);
            string penalty=tattooPart==BodyPart.Head?"머리: 적응 시 박치기 강화 / 심각 시 피해 6 · 재사용 ×2":tattooPart==BodyPart.Chest?"가슴: 적응 시 최후의 숨 강화 / 심각 시 가슴 피격 피해 ×1.5":tattooPart==BodyPart.Back?"등: 적응 시 맹세 생명 20 / 심각 시 명예 획득 절반":tattooPart==BodyPart.LeftArm?"왼팔: 적응 시 응수 강화 / 심각 시 패링 유효 시간 0.25초":tattooPart==BodyPart.LeftLeg?"왼다리: 적응 시 착지 강화 / 심각 시 점프·이동 저하":tattooPart==BodyPart.RightArm?"오른팔: 적응 시 강공격 강화 / 심각 시 후딜 ×1.9 · 피해 저하":"오른다리: 적응 시 대시 강화 / 심각 시 이동 ×0.48 · 대시 제한";
            Text(355,348,1130,22,penalty,14,muted);
            var choices=TattooCatalog.All.Where(t=>t.part==tattooPart).ToArray();
            for(int i=0;i<choices.Length;i++) {
                var t=choices[i];float y=379+i*78;
                bool owned=E.Profile.tattoos.Contains(t.id),equipped=E.Profile.equippedTattoos.Contains(t.id),unlocked=TattooCatalog.IsUnlocked(E.Profile,t.id);
                Text(355,y,285,27,t.name+(equipped?" · 장착":owned?" · 소유":""),18,equipped?NordicArt.Ice:pale);
                Text(355,y+29,285,39,TattooCatalog.TriggerHint(t.power)+$"\n면적 {t.area}",13,NordicArt.Gold);
                Text(660,y,540,72,t.description,14,muted);
                bool fits=TattooCatalog.UsedArea(E.Profile,t.part)+t.area<=TattooCatalog.Capacity(t.part);
                string action=!unlocked?"미해금":equipped?"해제":owned?(fits?"장착":"면적 부족"):$"{(fits?"획득·장착":"보관 획득")} · {t.price} 은화";
                if(Button(1230,y,285,45,action,unlocked&&(!owned||equipped||fits))) {
                    string result;bool ok=equipped?E.UnequipTattoo(t.id,out result):owned?E.EquipTattoo(t.id,out result):E.BuyTattoo(t.id,out result);
                    D.message=result;if(ok)G.body=CampaignSession.BuildBody();
                }
                if(!unlocked)Text(1230,y+48,285,25,t.unlockHint,12,NordicArt.Red);
                else if(owned&&!equipped&&!fits)Text(1230,y+48,285,25,"같은 부위의 문신을 먼저 해제하세요.",12,muted);
            }
            Text(355,700,1130,60,$"공간이 있으면 획득 즉시 장착됩니다. 장착한 문신만 발동하며 교체는 무료입니다.\n심각한 부상은 해당 부위 문신 전체를 잠재웁니다. 화염 {E.Profile.fireScars}/2 · 거인 {E.Profile.giantScars}/2 · 용맹 {E.Profile.valorMarks} · 생존 {E.Profile.survivorMarks}",14,NordicArt.Red);
        }
        void Inventory()
        {
            Text(355,276,390,33,"아이템",19,NordicArt.Gold);Text(820,276,220,33,"마을 보관",19,NordicArt.Gold);Text(1130,276,330,33,"이번 출정 미보관",19,NordicArt.Gold);
            int row=0;foreach(var item in EconomyCatalog.Items) {
                float y=325+row++*44;int loot=D.IsDungeon?CampaignSession.Run.loot.Find(s=>s.id==item.id)?.count??0:0;
                Text(355,y,420,30,Name(item.id),18);Text(830,y,200,30,$"{E.Count(item.id)} / {item.maxStack}",18,muted);Text(1170,y,180,30,loot.ToString(),18,NordicArt.Gold);
            }
            Text(355,655,1100,75,$"무기: {Name(E.Profile.equippedWeapon)}   방어구: {(E.Profile.equippedArmor=="none"?"없음":Name(E.Profile.equippedArmor))}\n미보관 재료는 안전 귀환 시 전부, 사망 시 종류별 절반만 보관됩니다. 초과분은 판매가로 정산합니다.",17,muted);
            if(D.IsDungeon&&Button(1270,590,245,45,"연고 사용 · 생명 +35",E.Count("healing_salve")>0&&G.player.hp<110))D.UseSalve();
        }
        void Routes()
        {
            var run=CampaignSession.Run;if(run==null||!D.roomClear){Text(355,290,1000,50,"먼저 이 구역의 적을 모두 쓰러뜨리세요.",22);return;}
            if(D.gate&&!D.gate.IsOpen){
                Text(355,280,1120,150,"관문이 잠겨 있습니다.\n왼쪽 지레를 E로 당기면 조용히 열립니다.\n관문을 강공격 두 번으로 부수면 지원군 2명이 나타납니다.",22);
                if(Button(355,470,760,64,"돌아가서 관문 열기"))D.ClosePanel();
                if(Button(355,650,760,64,"여기서 안전 귀환 · 전리품 전부 보관"))D.ReturnVillage(true);
                return;
            }
            Text(355,280,1120,65,$"{run.plan[run.part].name} · {run.Room.title} 완료\n체력과 부상은 다음 방에도 이어집니다.",22);
            int row=0;foreach(int next in run.Room.exits) {
                var room=run.plan[run.part].rooms[next];float y=390+row++*100;
                if(Button(355,y,760,75,room.title+(room.kind==RoomKind.Salvage?" · 적 1 / 함정 없음 / 압력판 보급함":room.kind==RoomKind.Elite?" · 정예 포함 적 3 / 더 많은 보상":" · 수호자 전투")))D.Travel(next);
            }
            if(run.room==3&&Button(355,390,760,75,run.part==2?"여정 완수 · 발키리의 심사":"다음 지역 · "+run.plan[run.part+1].name))D.NextPart();
            if(Button(355,650,760,64,"여기서 안전 귀환 · 전리품 전부 보관"))D.ReturnVillage(true);
        }
        void Training()
        {
            Text(355,290,1060,280,"도끼를 들기 전에 몸으로 배워라.\n\n이동과 점프 → 강공격 명중 → 패링 → 전진 회피\n→ 자동 부상 확인 → 심각한 부상과 문신 침묵\n\n추가 연습: Q 박치기 / 경직된 작은 적을 V로 잡고 다시 V로 투척.\n잡은 적은 앞에서 오는 공격을 한 번 대신 맞습니다.\n훈련에서는 사망하지 않습니다. 첫 완료 시 제작용 재료를 지급합니다.",23);
            if(Button(355,590,620,70,E.Profile.tutorialComplete?"훈련 다시 하기":"훈련 시작"))D.BeginTraining();
        }
        void Journal()
        {
            Text(355,285,1050,130,$"완주 {E.Profile.completedRuns}회 · 사망 {E.Profile.deaths}회 · 최고 도달 {E.Profile.bestPart} / 3\n팔의 기억 {E.Profile.armScars}/2 · 다리의 기억 {E.Profile.legScars}/2 · 튜토리얼 {(E.Profile.tutorialComplete?"완료":"미완료")}\n\n마을 준비 → 잿빛 숲 → 부러진 광산 → 망자의 성소 → 귀환",21);
            Text(355,428,1090,184,$"화염 흉터 {E.Profile.fireScars}/2 · 거인 흉터 {E.Profile.giantScars}/2 · 용맹 {E.Profile.valorMarks} · 생존 {E.Profile.survivorMarks}\n최근 심사: {E.Profile.lastJudgment}\n\n흉터와 심사는 문신소의 선택지를 해금합니다. 자동 능력치 보너스는 없습니다.\n출정 중 종료하면 미보관 전리품은 남지 않습니다.",17,muted);
            if(Button(355,630,350,62,"계속하기"))D.ClosePanel();
            if(D.IsVillage&&Button(740,630,350,62,"로비로"))D.ToLobby();
            if(Button(1120,630,390,62,"저장된 준비 유지 · 게임 종료")){E.Save();Application.Quit();}
        }
        void Result()
        {
            bool won=G.phase==Phase.Victory;Shade(won?"오늘은 자격을 증명했다.":"아직, 도끼를 놓을 때가 아니다.","VALKYRIE'S VERDICT");
            var run=CampaignSession.Run;
            string cause=!won&&run!=null&&run.fatalPart.HasValue?$"최후의 상처: {Game.PartName(run.fatalPart.Value)} · {(run.fatalKind==DamageKind.Fire?"화염":run.fatalKind==DamageKind.Giant?"거인":run.fatalSource=="raider"?"약탈자":run.fatalSource=="iron_sentinel"?"철갑 파수꾼":run.fatalSource=="draugr"?"망자":"환경 / 함정")}":"";
            var judgment=CampaignSession.Judge(won,true);
            Text(355,290,1120,100,$"명예 {G.honor:0} · 처치 {(run?.kills??0)+G.kills} · 패링 {(run?.parries??0)+G.parries} · 회피 {run?.dodges??0}\n{cause}",23);
            Text(355,403,1120,36,judgment.title,27,NordicArt.Gold);
            Text(355,449,1120,80,judgment.reason,18);
            Text(355,536,1120,44,(judgment.valorCandidate?"용맹의 인장 후보 해금 · ":"")+(judgment.survivorCandidate?"고통의 메아리 후보 해금":"문신소에서 흉터와 후보를 확인하세요."),18,NordicArt.Ice);
            Text(355,592,1110,45,won?"전리품 전부 보관. 마을에서 정산하고 다음 여정을 준비하세요.":"이번 전리품은 종류별 절반 보관. 마을 소유품은 유지됩니다.",18,muted);
            if(Button(355,660,720,65,"마을로 돌아가 정산    /    ENTER"))D.ReturnVillage(won);
        }
    }
}
