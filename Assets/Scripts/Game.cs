using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Valhalla
{
    public enum Phase { Camp, Combat, Victory, Death, Village }

    public sealed class Game : MonoBehaviour
    {
        public static Game I;
        public Campaign.SceneRole sceneRole = Campaign.SceneRole.Legacy;
        public BodyBuild body = new BodyBuild();
        public readonly List<Enemy> enemies = new List<Enemy>();
        public Barbarian player;
        public Camera cam;
        public Phase phase;
        [NonSerialized] public BodyPart? choice;
        public bool paused, help, debug;
        public bool Frozen => Time.timeScale == 0;
        public int wave, kills, parries;
        public float honor = 50, damageTaken, retreatTime, shake, runTime;
        public string noticeTitle = "", noticeText = "";
        public Color noticeColor;
        public float noticeUntil;
        float hitStopUntil, nextWave;
        bool nextWavePending;
        public CombatAudio Audio { get; private set; }
        public TattooWorldFeedback TattooFeedback { get; private set; }

        void Awake()
        {
            I = this; Time.timeScale = 1; Application.targetFrameRate = 60;
            NordicArt.World();
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            cam = cameraObject.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 7.6f;
            cam.backgroundColor = NordicArt.Hex("101C25"); cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(-3, 2.65f, -10); cameraObject.AddComponent<AudioListener>();
            Audio = gameObject.AddComponent<CombatAudio>();
            var hero = new GameObject("Barbarian"); player = hero.AddComponent<Barbarian>(); player.Initialize();
            TattooFeedback = gameObject.AddComponent<TattooWorldFeedback>();
            if (sceneRole == Campaign.SceneRole.Legacy)
            {
                AddTattoo("토르의 오른팔", BodyPart.RightArm, TattooPower.Thunder);
                AddTattoo("피의 룬", BodyPart.RightArm, TattooPower.Blood);
                AddTattoo("질주의 룬", BodyPart.Legs, TattooPower.Ram);
                gameObject.AddComponent<GameHUD>();
                phase = Phase.Camp;
            }
            else
            {
                gameObject.AddComponent<Campaign.CampaignHUD>();
                gameObject.AddComponent<Campaign.CampaignDirector>().Initialize();
            }
        }
        void Start()
        {
            bool smokeTest = Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-test") >= 0;
            // Smoke tests exercise the legacy combat loop only; redirect before starting the coroutine so the
            // coroutine always runs against a Game instance whose sceneRole is already Legacy.
            if (smokeTest && sceneRole != Campaign.SceneRole.Legacy) { SceneManager.LoadScene("AshenThreshold"); return; }
            if (smokeTest) StartCoroutine(SmokeTest());
            else if (Array.IndexOf(Environment.GetCommandLineArgs(), "--capture") >= 0) StartCoroutine(CaptureOnly());
        }
        void AddTattoo(string title, BodyPart part, TattooPower power)
        {
            var tattoo = ScriptableObject.CreateInstance<TattooDefinition>(); tattoo.title = title; tattoo.bodyPart = part; tattoo.power = power; body.tattoos.Add(tattoo);
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) debug = !debug;
            if (Input.GetKeyDown(KeyCode.Tab)) help = !help;
            bool legacy = sceneRole == Campaign.SceneRole.Legacy;
            // Campaign scenes delegate Enter/R/wave transitions and the Esc menu to CampaignDirector,
            // which owns panel state (village stations, routes, journal, pause).
            if (legacy)
            {
                if (!choice.HasValue && Input.GetKeyDown(KeyCode.Escape) && phase == Phase.Combat) { paused = !paused; SyncPause(); }
                if ((phase == Phase.Camp || phase == Phase.Death || phase == Phase.Victory) && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.R))) BeginRun();
                if (Input.GetKeyDown(KeyCode.R) && paused && !choice.HasValue) BeginRun();
            }
            if (debug && phase == Phase.Combat && !Frozen)
            {
                if (Input.GetKeyDown(KeyCode.F6)) player.TakeHit(16, BodyPart.RightArm, null, true);
                if (Input.GetKeyDown(KeyCode.F7)) player.TakeHit(16, BodyPart.Legs, null, true);
                if (Input.GetKeyDown(KeyCode.F8)) player.hp = 110;
            }
            if (!choice.HasValue && !paused && hitStopUntil > 0 && Time.unscaledTime >= hitStopUntil) { hitStopUntil = 0; SyncPause(); }
            if (legacy && phase == Phase.Combat && !Frozen)
            {
                runTime += Time.deltaTime;
                if (enemies.Count == 0 && !nextWavePending) { nextWavePending = true; nextWave = 1.8f; }
                if (nextWavePending)
                {
                    nextWave -= Time.deltaTime;
                    if (nextWave <= 0) { nextWavePending = false; if (wave >= 3) EndRun(true); else SpawnWave(); }
                }
            }
        }
        void LateUpdate()
        {
            if (!cam || !player) return;
            float x = Mathf.Clamp(player.transform.position.x * .6f, -8, 8);
            var target = new Vector3(x, 2.65f, -10);
            cam.transform.position = Vector3.Lerp(cam.transform.position, target, 1 - Mathf.Exp(-4 * Time.unscaledDeltaTime));
            if (shake > 0 && !choice.HasValue && !paused)
            {
                cam.transform.position += (Vector3)(UnityEngine.Random.insideUnitCircle * shake); shake = Mathf.MoveTowards(shake, 0, Time.unscaledDeltaTime * .8f);
            }
        }
        public void BeginRun()
        {
            if(TattooFeedback)TattooFeedback.Clear();
            foreach (var enemy in enemies.ToArray()) if (enemy) Destroy(enemy.gameObject);
            enemies.Clear(); body.Reset(); player.ResetPlayer(); choice = null; paused = false; help = false;
            honor = 50; kills = parries = wave = 0; damageTaken = runTime = retreatTime = 0; hitStopUntil = 0;
            nextWavePending = false; phase = Phase.Combat; SyncPause(); SpawnWave();
        }
        public void SpawnWave()
        {
            wave++;
            if (wave == 1) { Spawn(4, false, false); Notice("01 / 도끼의 무게", "적의 붉은 예고를 보고 C로 패링. 빈틈에 강공격.", NordicArt.Gold, 5); }
            else if (wave == 2) { Spawn(9, false, true); Spawn(-12, false, false); Notice("02 / 몸에 새겨진 대가", "부상은 선택을 연다. 심각한 부상은 문신을 침묵시킨다.", NordicArt.Gold, 5); }
            else { Spawn(10, true, false); Notice("03 / 뼈를 부수는 자", "도끼는 오른팔을, 내려찍기는 다리를 위협한다.", NordicArt.Red, 5); }
        }
        public Enemy Spawn(float x, bool boss, bool charger)
        {
            var o = new GameObject(boss ? "Bonebreaker" : charger ? "Raider" : "Draugr"); o.transform.position = new Vector3(x, -.34f);
            var enemy = o.AddComponent<Enemy>(); enemy.Initialize(boss, charger); return enemy;
        }
        public void SyncPause() { if((paused||choice.HasValue)&&player)player.ClearBufferedInput(); Time.timeScale = choice.HasValue || paused || Time.unscaledTime < hitStopUntil ? 0 : 1; if(Audio)Audio.SetMenuPaused(choice.HasValue || paused); }
        public void HitStop(float seconds) { hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + seconds); SyncPause(); }
        public void EndRun(bool victory)
        {
            if(TattooFeedback)TattooFeedback.Clear();
            if(player)player.ClearBufferedInput();
            phase = victory ? Phase.Victory : Phase.Death; choice = null; paused = false; hitStopUntil = 0; Time.timeScale = 1;
            Notice(victory ? "문이 열렸다" : "아직, 발할라는 아니다", victory ? "오늘의 몸으로 끝까지 싸웠다." : "흉터는 남는다. 다시 도전하라.", victory ? NordicArt.Gold : NordicArt.Red, 10);
        }
        public void TrackHonor(float move, float dt)
        {
            Enemy nearest = null; float distance = float.MaxValue;
            foreach (var e in enemies) if (e && e.Alive && Mathf.Abs(e.transform.position.x - player.transform.position.x) < distance) { nearest = e; distance = Mathf.Abs(e.transform.position.x - player.transform.position.x); }
            bool retreating = nearest && distance > 5 && move * (nearest.transform.position.x - player.transform.position.x) < 0;
            if (retreating) { retreatTime += dt; if (retreatTime > 2.5f) honor = Mathf.Max(0, honor - dt * 3); }
            else retreatTime = Mathf.Max(0, retreatTime - dt * 2);
            var run=Campaign.CampaignSession.Run;
            if(run!=null&&sceneRole>=Campaign.SceneRole.Forest) {
                if(retreating&&retreatTime>2.5f)run.retreatSeconds+=dt;
                if(nearest&&distance<=3.5f&&(nearest.transform.position.x-player.transform.position.x)*player.facing>=0)run.closeSeconds+=dt;
                if(nearest&&distance<=5&&player.hp<=35)run.criticalSeconds+=dt;
            }
        }
        public void Notice(string title, string text, Color color, float duration = 2.8f)
        { noticeTitle = title; noticeText = text; noticeColor = color; noticeUntil = Time.unscaledTime + duration; }
        public void GainHonor(float amount) { honor=Mathf.Min(100,honor+amount*body.HonorGainMultiplier); }
        public static string PartName(BodyPart part) => part==BodyPart.RightArm?"오른팔":part==BodyPart.Legs?"오른다리":part==BodyPart.Head?"머리":part==BodyPart.Chest?"가슴":part==BodyPart.Back?"등":part==BodyPart.LeftArm?"왼팔":"왼다리";
        public void Burst(Vector2 point, Color color, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                var o = NordicArt.Block("Spark", null, point, Vector2.one * UnityEngine.Random.Range(.035f, .12f), color, 60);
                var f = o.gameObject.AddComponent<Fleck>(); f.velocity = UnityEngine.Random.insideUnitCircle * speed + Vector2.up * 2; f.life = UnityEngine.Random.Range(.18f, .55f);
            }
        }
        public void PlaySound(CombatCue cue) { if(Audio)Audio.Play(cue); }
        IEnumerator CaptureOnly()
        {
            yield return new WaitForSecondsRealtime(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath, "../preview.png"));
            yield return new WaitForSecondsRealtime(1); Application.Quit();
        }
        IEnumerator SmokeTest()
        {
            var results = new List<string>();
            yield return null;
            BeginRun();
            // Freeze enemy locomotion, but exercise the real player attack path and hit range.
            var target = enemies[0]; target.transform.position = player.transform.position + Vector3.right * 1.4f; target.Stun(20);
            float before = target.hp; player.Attack(true);
            yield return new WaitForSecondsRealtime(.65f);
            Check(target == null || target.hp < before, "Heavy attack deals damage after windup", results);
            Check(player.hp == 110, "Safe test target did not damage player", results);
            player.TakeHit(30, BodyPart.RightArm, null, true);
            Check(!choice.HasValue && !Frozen, "Automatic stage 2 adaptation never opens a choice or pauses", results);
            Check(body.arm.Stage == 2 && body.arm.adaptation == Adaptation.Fury, "Crossing 30 trauma applies Fury automatically", results);
            float enemyClock = target ? target.stun : 0;
            yield return new WaitForSecondsRealtime(.15f);
            Check(!target || target.stun < enemyClock, "Enemy simulation keeps running after automatic adaptation", results);
            Check(body.Has(TattooPower.Thunder) && body.Has(TattooPower.Blood), "Both right-arm tattoos active before severe injury", results);
            player.TakeHit(25, BodyPart.RightArm, null, true);
            Check(body.arm.Stage == 3 && !body.Has(TattooPower.Thunder) && !body.Has(TattooPower.Blood), "Severe arm injury disables ALL arm tattoos", results);
            Check(body.Has(TattooPower.Ram), "Other body part tattoo remains active", results);
            Check(body.RecoveryMultiplier >= 1.9f && body.HeavyDamage < 56, "Severe arm penalties intensified", results);
            float hpBeforeKill = player.hp;
            if (target) target.TakeHit(1000, 1, .6f);
            Check(Mathf.Approximately(player.hp, hpBeforeKill), "Disabled blood tattoo cannot heal on kill", results);
            BeginRun();
            Check(body.arm.Stage == 0 && body.Has(TattooPower.Thunder) && body.arm.adaptation == Adaptation.None, "New run resets injury, adaptation and restores tattoos", results);
            target = enemies[0]; target.transform.position = player.transform.position + Vector3.right;
            player.Parry(); player.TakeHit(12, BodyPart.RightArm, target);
            Check(player.hp == 110 && target.stun >= 1, "Front parry prevents injury and staggers enemy", results);
            yield return new WaitForSecondsRealtime(.15f);
            player.invulnerability = 0; player.parryTimer = 0; player.dashCooldown = 0;
            player.facing = -1; player.Dash(0);
            yield return new WaitForSecondsRealtime(.05f);
            Check(player.velocity.x > 0, "Dash with no input falls back to facing toward the enemy", results);
            player.dashTimer = 0; player.dashCooldown = 0; player.velocity = Vector2.zero;
            player.transform.position = target.transform.position - Vector3.right;
            player.Dash(-1);
            yield return new WaitForSecondsRealtime(.05f);
            Check(player.velocity.x < 0, "Dash honors opposite horizontal input over facing toward the enemy", results);
            player.dashTimer = 0; player.dashCooldown = 0; player.velocity = Vector2.zero;
            player.transform.position = target.transform.position - Vector3.right;
            player.Dash(1); player.TakeHit(12, BodyPart.Legs, target);
            Check(player.hp == 110 && body.legs.Stage == 0, "Forward dodge avoids hit and trauma", results);
            player.dashTimer = 0;
            player.TakeHit(30, BodyPart.Legs, null, true); player.TakeHit(25, BodyPart.Legs, null, true);
            Check(body.legs.Stage == 3 && !body.Has(TattooPower.Ram) && body.MoveMultiplier == .48f && body.DashSpeed == 10, "Severe leg injury disables charge tattoo and slows movement", results);
            player.TakeHit(200, BodyPart.RightArm, null, true);
            Check(phase == Phase.Death && !choice.HasValue && !Frozen, "Fatal injury produces death without orphaned selection", results);
            var deathPosition = player.transform.position;
            yield return new WaitForSecondsRealtime(.1f);
            Check(player.transform.position == deathPosition, "Player simulation stops after death", results);
            BeginRun(); player.TakeHit(56, BodyPart.RightArm, null, true);
            Check(body.arm.Stage == 3 && !choice.HasValue && !Frozen, "Large nonfatal hit jumps directly to severe without pausing", results);
            Check(!body.Has(TattooPower.Thunder) && body.arm.adaptation == Adaptation.Fury && body.RecoveryMultiplier == 1.9f, "Direct severe crossing still auto-adapts but disables tattoos and applies full penalty", results);
            BeginRun();
            for (int w = 0; w < 3; w++)
            {
                foreach (var enemy in enemies.ToArray()) enemy.TakeHit(999, 1, 1);
                yield return new WaitForSecondsRealtime(2.1f);
            }
            Check(phase == Phase.Victory, "All three encounters lead to victory", results);
            var report = string.Join("\n", results); File.WriteAllText(Path.Combine(Application.dataPath, "../smoke-results.txt"), report);
            Debug.Log(report); Application.Quit(results.Exists(x => x.StartsWith("FAIL")) ? 1 : 0);
        }
        static void Check(bool condition, string name, List<string> results) { results.Add((condition ? "PASS " : "FAIL ") + name); }
        void OnDestroy() { Time.timeScale = 1; if (I == this) I = null; }
    }
}
