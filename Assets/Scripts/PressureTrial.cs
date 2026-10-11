using System.Collections.Generic;
using UnityEngine;

namespace Valhalla.Campaign
{
    // Optional Salvage room puzzle: shove the crate (or a corpse) onto the plate to unlock the vault.
    // All visuals are children of this transform so the root's scene cleanup (Destroy(gameObject)) tears
    // the whole trial down with nothing left dangling. Root owns HUD/E prompt, scene teardown and the
    // Strike/death call sites; this component only tracks state and renders it.
    public sealed class PressureTrial : MonoBehaviour
    {
        const float InitialCrateX = -2f, FloorY = -1.35f, CenterY = -.85f, VaultXConst = 9f;
        const float PushGap = .9f, ShoveDistance = 2.5f, PressThreshold = .75f, ClaimRange = 2f;
        const float MinX = -16f, MaxX = 10f;
        const float LiftHorizontal = 1.6f, LiftVertical = 2f, CarryOffsetX = .9f, CarryOffsetY = .1f, DropCooldown = .25f;
        const float ThrowDuration = .5f, ThrowSpeed = 10f, ThrowArcBase = .8f, ThrowArcHeight = 1.2f;
        const float ThrowHitHorizontal = .7f, ThrowHitVertical = 2f, ThrowDamage = 24f;
        static readonly Color Dark = new Color(.17f, .16f, .15f);

        CampaignDirector owner;
        string material;
        Transform crateVisual;
        SpriteRenderer plateGlow, vaultGlow;
        readonly List<Transform> remains = new List<Transform>();
        float previousPlayerX;
        bool hasAnchor;
        Transform weight;
        int carryDirection;
        float liftCooldown, flightElapsed;

        public bool Pressed { get; private set; }
        public bool Claimed { get; private set; }
        public bool Carrying { get; private set; }
        public bool Flying { get; private set; }
        public float PlateX => 7f;
        public float CrateX => crateVisual ? crateVisual.position.x : InitialCrateX;
        public int RemainsCount => remains.Count;

        public void Initialize(CampaignDirector director, string loot)
        {
            owner = director; material = loot;
            transform.position = Vector3.zero;
            BuildCrate(); BuildPlate(); BuildVault();
            ResetMotionAnchor();
        }

        void BuildCrate()
        {
            var t = new GameObject("Salvage crate").transform; t.SetParent(transform, false);
            t.position = new Vector3(InitialCrateX, CenterY);
            if(PaintedCrateVisual.Attach(t,.78f,13)) { crateVisual=t; return; }
            NordicArt.Block("Crate wood", t, Vector2.zero, new Vector2(.86f, .78f), NordicArt.Hex("5A4630"), 12);
            NordicArt.Block("Crate band", t, new Vector2(0, .18f), new Vector2(.92f, .08f), NordicArt.Hex("8A7656"), 13);
            NordicArt.Block("Crate band", t, new Vector2(0, -.18f), new Vector2(.92f, .08f), NordicArt.Hex("8A7656"), 13);
            crateVisual = t;
        }

        void BuildPlate()
        {
            var t = new GameObject("Pressure plate").transform; t.SetParent(transform, false);
            t.position = new Vector3(PlateX, FloorY);
            NordicArt.Block("Plate frame", t, Vector2.zero, new Vector2(1.6f, .14f), NordicArt.Hex("3A342C"), 6);
            plateGlow = NordicArt.Block("Plate glow", t, Vector2.zero, new Vector2(1.3f, .09f), Dark, 7).GetComponent<SpriteRenderer>();
        }

        void BuildVault()
        {
            var t = new GameObject("Supply vault").transform; t.SetParent(transform, false);
            t.position = new Vector3(VaultXConst, CenterY);
            bool painted=PaintedCrateVisual.Attach(t,1f,12);
            if(!painted) {
                NordicArt.Block("Vault rim", t, Vector2.zero, new Vector2(1.05f, 1.25f), NordicArt.Hex("4A3A28"), 11);
                NordicArt.Block("Vault body", t, Vector2.zero, new Vector2(.95f, 1.15f), NordicArt.Hex("30261B"), 12);
            }
            vaultGlow = NordicArt.Block("Vault seal", t, new Vector2(0,.12f), new Vector2(.12f,.2f), Dark, 13).GetComponent<SpriteRenderer>();
            vaultGlow.transform.localRotation=Quaternion.Euler(0,0,45);
        }

        // Visual-only weight: never a real Enemy and never grants a second drop, bosses/elites are excluded.
        public void AddRemains(Enemy enemy)
        {
            if (!enemy || enemy.Alive || enemy.boss || enemy.elite) return;
            float x = Mathf.Clamp(enemy.transform.position.x, MinX, MaxX);
            remains.Add(BuildRemainsVisual(x));
            while (remains.Count > 3)
            {
                int oldestIndex = 0;
                while (oldestIndex < remains.Count && remains[oldestIndex] == weight) oldestIndex++;
                if (oldestIndex >= remains.Count) break;
                var oldest = remains[oldestIndex]; remains.RemoveAt(oldestIndex);
                DestroyRemainsVisual(oldest);
            }
            RefreshState();
        }

        void DestroyRemainsVisual(Transform t)
        {
            if (!t) return;
            DestroyRemainsSprite(t);
            Destroy(t.gameObject);
        }

        Transform BuildRemainsVisual(float x)
        {
            var t = new GameObject("Fallen remains").transform; t.SetParent(transform, false);
            t.position = new Vector3(x, CenterY);
            NordicArt.Block("Remains heap", t, Vector2.zero, new Vector2(.72f, .34f), NordicArt.Hex("23201C"), 11);
            NordicArt.Block("Remains cloth", t, new Vector2(.05f, .1f), new Vector2(.5f, .14f), NordicArt.Hex("4B413A"), 12);
            var texture=Resources.Load<Texture2D>("Art/Draugr-v1");
            if(texture)
            {
                var body=new GameObject("Fallen draugr");body.transform.SetParent(t,false);
                body.transform.localRotation=Quaternion.Euler(0,0,90);
                var sr=body.AddComponent<SpriteRenderer>();
                sr.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.height/1.6f);
                sr.sortingOrder=16;sr.color=new Color(.65f,.62f,.6f);
            }
            return t;
        }

        public void Strike(Vector2 origin, int facing, float reach, bool heavy)
        {
            if (!heavy || !Game.I || Game.I.Frozen || Game.I.phase != Phase.Combat || Mathf.Abs(facing)!=1) return;
            Shove(crateVisual, origin, facing, reach);
            foreach (var r in remains) Shove(r, origin, facing, reach);
            RefreshState();
        }

        bool HeldOrFlying(Transform t) => t && t == weight && (Carrying || Flying);

        void Shove(Transform t, Vector2 origin, int facing, float reach)
        {
            if (!t || HeldOrFlying(t)) return;
            float dx = (t.position.x - origin.x) * facing;
            if (dx < -.25f || dx > reach || Mathf.Abs(t.position.y - origin.y) >= 2f) return;
            var pos = t.position; pos.x = Mathf.Clamp(pos.x + facing * ShoveDistance, MinX, MaxX); t.position = pos;
        }

        public bool TryClaim()
        {
            var g = Game.I;
            if (g == null || g.phase != Phase.Combat || g.Frozen || Claimed || !Pressed) return false;
            if (g.player == null || !g.player.Grounded) return false;
            if (Mathf.Abs(g.player.transform.position.x - VaultXConst) > ClaimRange) return false;
            Claimed = true;
            owner.Drop(new Vector2(VaultXConst, -.8f), material, 3);
            g.Notice("보급함이 열렸다", "무게 장치로 봉인을 풀었다 · 재료 3개를 회수하세요.", NordicArt.Gold, 4);
            RefreshState();
            return true;
        }

        public bool TryLift()
        {
            var g = Game.I;
            var hero = g ? g.player : null;
            if (hero == null || !hero.CanAct || !hero.Grounded) return false;
            if (hero.Attacking || hero.Dodging || hero.Parrying || hero.Headbutting) return false;
            if (hero.grapple != null && (hero.grapple.Holding || hero.grapple.Thrown)) return false;
            if (g.body.leftArm.Stage >= 3) return false;
            if (Carrying || Flying || liftCooldown > 0) return false;

            Transform best = null; float bestDistance = float.MaxValue;
            ConsiderLiftCandidate(crateVisual, hero.transform.position, ref best, ref bestDistance);
            foreach (var r in remains) ConsiderLiftCandidate(r, hero.transform.position, ref best, ref bestDistance);
            if (!best) return false;

            weight = best; Carrying = true; carryDirection = hero.facing;
            PositionCarried(hero);
            RefreshState();
            return true;
        }

        static void ConsiderLiftCandidate(Transform t, Vector2 heroPos, ref Transform best, ref float bestDistance)
        {
            if (!t) return;
            float dx = Mathf.Abs(t.position.x - heroPos.x);
            if (dx > LiftHorizontal || Mathf.Abs(t.position.y - heroPos.y) >= LiftVertical) return;
            if (dx < bestDistance) { bestDistance = dx; best = t; }
        }

        void PositionCarried(Barbarian hero)
        {
            if (!weight) return;
            var pos = weight.position;
            pos.x = hero.transform.position.x + carryDirection * CarryOffsetX;
            pos.y = hero.transform.position.y + CarryOffsetY;
            weight.position = pos;
        }

        public bool DropHeld()
        {
            var g = Game.I;
            var hero = g ? g.player : null;
            if (hero == null || !hero.CanAct || !Carrying) return false;
            float x = Mathf.Clamp(hero.transform.position.x + carryDirection * CarryOffsetX, MinX, MaxX);
            LandWeight(x);
            liftCooldown = DropCooldown;
            return true;
        }

        public bool ThrowHeld()
        {
            var g = Game.I;
            var hero = g ? g.player : null;
            if (hero == null || !hero.CanAct || !Carrying) return false;
            if (g.body.leftArm.Stage >= 3) return false;
            Carrying = false; Flying = true; flightElapsed = 0;
            g.PlaySound(CombatCue.Throw);
            return true;
        }

        void UpdateFlight(float dt)
        {
            if (!weight) { Flying = false; return; }
            float prevX = weight.position.x;
            dt=Mathf.Min(dt,Mathf.Max(0,ThrowDuration-flightElapsed));
            flightElapsed += dt;
            float t = Mathf.Clamp01(flightElapsed / ThrowDuration);
            float newX = Mathf.Clamp(prevX + carryDirection * ThrowSpeed * dt, MinX, MaxX);
            float newY = CenterY + ThrowArcBase + Mathf.Sin(t * Mathf.PI) * ThrowArcHeight;
            var pos = weight.position; pos.x = newX; pos.y = newY; weight.position = pos;

            Enemy hit = FindFlightHit(prevX, newX);
            bool boundary = newX <= MinX || newX >= MaxX;
            bool expired = flightElapsed >= ThrowDuration;

            if (hit != null)
            {
                int direction = carryDirection;
                LandWeight(hit.transform.position.x);
                hit.TakeHit(ThrowDamage, direction, .6f);
                Game.I.PlaySound(CombatCue.PropImpact);
                return;
            }
            if (expired || boundary) { LandWeight(newX); Game.I.PlaySound(CombatCue.PropImpact); }
        }

        Enemy FindFlightHit(float prevX, float newX)
        {
            float segMin = Mathf.Min(prevX, newX) - ThrowHitHorizontal, segMax = Mathf.Max(prevX, newX) + ThrowHitHorizontal;
            Enemy best = null; float bestAhead = float.MaxValue;
            foreach (var enemy in Game.I.enemies)
            {
                if (!enemy || !enemy.Alive || enemy.Captured) continue;
                float ex = enemy.transform.position.x;
                if (ex < segMin || ex > segMax) continue;
                if (Mathf.Abs(enemy.transform.position.y - weight.position.y) >= ThrowHitVertical) continue;
                float ahead = (ex - prevX) * carryDirection;
                if (ahead < 0) continue;
                if (ahead < bestAhead) { bestAhead = ahead; best = enemy; }
            }
            return best;
        }

        void LandWeight(float x)
        {
            Carrying = false; Flying = false;
            liftCooldown=DropCooldown;
            if (weight)
            {
                var pos = weight.position;
                pos.x = Mathf.Clamp(x, MinX, MaxX); pos.y = CenterY;
                weight.position = pos;
            }
            weight = null;
            RefreshState();
        }

        public void CancelCarry()
        {
            if (!Carrying && !Flying) return;
            LandWeight(weight ? weight.position.x : 0);
        }

        void OnDisable() { CancelCarry(); }
        void OnDestroy()
        {
            CancelCarry();
            foreach (var r in remains) DestroyRemainsSprite(r);
        }

        void DestroyRemainsSprite(Transform t)
        {
            if (!t) return;
            var body = t.Find("Fallen draugr");
            var sr = body ? body.GetComponent<SpriteRenderer>() : null;
            if (sr && sr.sprite) Destroy(sr.sprite);
        }

        public void ResetMotionAnchor()
        {
            var g = Game.I;
            hasAnchor = g && g.player;
            if (g != null && g.player != null) previousPlayerX = g.player.transform.position.x;
        }

        void Update()
        {
            var g = Game.I;
            if (g == null) return;
            if (g.Frozen) { ResetMotionAnchor(); return; }
            if (g.phase != Phase.Combat || g.player == null)
            {
                if (Carrying || Flying) CancelCarry();
                ResetMotionAnchor(); return;
            }
            float dt = Time.deltaTime;
            if (liftCooldown > 0) liftCooldown = Mathf.Max(0, liftCooldown - dt);
            if (g.body.leftArm.Stage >= 3 && (Carrying || Flying)) CancelCarry();
            if (Carrying) PositionCarried(g.player);
            if (Flying) UpdateFlight(dt);
            float playerX = g.player.transform.position.x;
            if (hasAnchor && g.player.Grounded) PushCrate(playerX);
            previousPlayerX = playerX; hasAnchor = true;
            RefreshState();
        }

        // No Rigidbody: comparing this frame's player x against the last one gives a push direction and a
        // swept contact test, so a single large move (dash, teleport) still snaps the crate ahead of the
        // hero instead of letting them pass through it unpushed.
        void PushCrate(float playerX)
        {
            if (!crateVisual || HeldOrFlying(crateVisual)) return;
            float dx = playerX - previousPlayerX;
            if (Mathf.Abs(dx) < 1e-5f) return;
            int dir = dx > 0 ? 1 : -1;
            float contact = crateVisual.position.x - dir * PushGap;
            bool crossed = dir > 0 ? previousPlayerX <= crateVisual.position.x && playerX > contact : previousPlayerX >= crateVisual.position.x && playerX < contact;
            if (!crossed) return;
            var pos = crateVisual.position; pos.x = Mathf.Clamp(playerX + dir * PushGap, MinX, MaxX); crateVisual.position = pos;
        }

        void RefreshState()
        {
            Pressed = !HeldOrFlying(crateVisual) && WithinPlate(crateVisual);
            if (!Pressed) foreach (var r in remains) if (!HeldOrFlying(r) && WithinPlate(r)) { Pressed = true; break; }
            if (plateGlow) plateGlow.color = Pressed ? NordicArt.Gold : Dark;
            if (vaultGlow) vaultGlow.color = Claimed ? NordicArt.Ice : Pressed ? NordicArt.Gold : Dark;
        }

        bool WithinPlate(Transform t) => t && Mathf.Abs(t.position.x - PlateX) <= PressThreshold;
    }
}
