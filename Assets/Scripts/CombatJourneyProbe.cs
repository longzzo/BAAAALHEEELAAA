using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Valhalla.Campaign
{
    // Opt-in diagnostic, not a player or balance oracle. No direct damage, health refill,
    // teleport, enemy timer changes, or invulnerability override during encounters.
    public sealed class CombatJourneyProbe : MonoBehaviour
    {
        readonly List<string> report = new List<string>();
        Game G => Game.I;
        CampaignDirector D => CampaignDirector.I;
        float nextDecision;
        int attacks, parries, dashes;
        int seed = 1337;
        bool eliteRoute, parryBuild;
        string outputName = "combat-journey-results.txt";
        readonly Dictionary<TattooPower,int> activations = new Dictionary<TattooPower,int>();
        void Activated(TattooPower power) { activations.TryGetValue(power,out int count); activations[power]=count+1; }
        void OnDestroy() { TattooSignals.Activated -= Activated; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoStart()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--combat-journey") < 0) return;
            var host = new GameObject("Normal combat journey probe");
            DontDestroyOnLoad(host); host.AddComponent<CombatJourneyProbe>();
        }
        void Record(string line)
        {
            report.Add(line); Debug.Log(line);
            File.WriteAllLines(Path.Combine(Application.dataPath, "../" + outputName), report);
        }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            eliteRoute = Array.IndexOf(args,"--journey-elite")>=0;
            parryBuild = Array.IndexOf(args,"--journey-parry")>=0;
            int seedIndex=Array.IndexOf(args,"--journey-seed");
            if(seedIndex>=0 && seedIndex+1<args.Length && int.TryParse(args[seedIndex+1],out int parsedSeed))seed=parsedSeed;
            outputName=$"combat-journey-{seed}-{(eliteRoute?"elite":"supply")}-{(parryBuild?"parry":"thunder")}.txt";
            TattooSignals.Activated += Activated;
            CampaignSession.Testing = true;
            CampaignSession.UseStore(new MemoryProfileStore());
            Record("Diagnostic: normal attack methods and live AI; 0.15s decision interval; reads visible enemy state. Not physical keyboard or human playtest.");
            SceneManager.LoadScene("Lobby"); yield return null; yield return null;
            D.EnterVillage(); yield return null; yield return null;
            var economy = CampaignSession.Economy;
            bool blood = economy.BuyTattoo("blood", out _);
            bool other = economy.BuyTattoo(parryBuild?"reprisal":"ram", out _);
            if(parryBuild) { economy.UnequipTattoo("thunder",out _); economy.EquipTattoo("reprisal",out _); }
            Record($"seed={seed} eliteRoute={eliteRoute} purchases={blood&&other} loadout={string.Join(",",economy.Profile.equippedTattoos)} coins={economy.Profile.coins}; starter weapon, no armor or consumables granted.");
            if(!blood || !other) { Record("SETUP FAILURE: starting purchases rejected"); Application.Quit(3); yield break; }
            D.StartExpedition(seed); yield return null; yield return null;
            int cleared = 0;
            for (int part = 0; part < 3; part++)
            {
                for (int room = 0; room < 3; room++)
                {
                    float began = Time.realtimeSinceStartup, initialHp = G.player.hp;
                    int initialAttacks = attacks, initialParries = parries, initialDashes = dashes;
                    while (G.phase == Phase.Combat && !D.roomClear && Time.realtimeSinceStartup - began < 90)
                    {
                        Decide(); yield return null;
                    }
                    Record($"part={part} room={CampaignSession.Run.room} clear={D.roomClear} phase={G.phase} seconds={Time.realtimeSinceStartup-began:F1} hp={initialHp:F1}->{G.player.hp:F1} attacks={attacks-initialAttacks} parryAttempts={parries-initialParries} dashes={dashes-initialDashes} remainingEnemies={G.enemies.Count} severeParts={G.body.States.Count(s=>s.Stage==3)}");
                    if (!D.roomClear) { RecordActivations(); Record("INCOMPLETE: death or encounter timeout; retain result for investigation, do not weaken assertions."); Application.Quit(2); yield break; }
                    cleared++;
                    // Navigation callbacks only AFTER combat. No claim of pathfinding/input coverage.
                    if (room == 0) D.gate.TryLever();
                    if (room < 2) D.Travel(room == 0 ? (eliteRoute?2:1) : 3);
                    yield return null; yield return null;
                }
                D.NextPart(); yield return null; yield return null;
            }
            bool victory = G.phase == Phase.Victory;
            if (victory) { D.ReturnVillage(true); yield return null; yield return null; }
            RecordActivations();
            Record($"FINISHED: rooms={cleared}/9 victory={victory} returnedVillage={G.sceneRole==SceneRole.Village}. One deterministic agent policy and seed; does not prove human difficulty or feel.");
            Application.Quit(victory ? 0 : 2);
        }
        void RecordActivations() => Record("Tattoo activations: "+string.Join(", ",activations.OrderBy(p=>p.Key).Select(p=>$"{p.Key}={p.Value}")));
        void Decide()
        {
            if (Time.unscaledTime < nextDecision || G.Frozen) return;
            nextDecision = Time.unscaledTime + .15f;
            var player = G.player;
            if (player.Attacking || player.Dodging || player.Headbutting || player.Parrying) return;
            var target = G.enemies.Where(e=>e && e.Alive && !e.Captured)
                .OrderBy(e=>Mathf.Abs(e.transform.position.x-player.transform.position.x)).FirstOrDefault();
            if (!target) return;
            float dx = target.transform.position.x-player.transform.position.x;
            float distance = Mathf.Abs(dx);
            bool imminent = G.enemies.Any(e=>e && e.Alive && e.state==1 && e.timer<=.32f && Mathf.Abs(e.transform.position.x-player.transform.position.x)<5);
            if (imminent && player.parryCooldown<=0) { player.Parry(); parries++; return; }
            if (distance>2.2f && player.dashCooldown<=0) { player.Dash(Mathf.Sign(dx)); dashes++; return; }
            if (distance<=2.2f && (target.stun>.2f || target.state==3 || target.state==0)) { player.Attack(true); attacks++; }
        }
    }
}
