using UnityEngine;

namespace Valhalla
{
    // Grab a weakened/stunned foe, carry it as a frontal human shield, or throw it into another enemy.
    // Depends on Enemy.Captured and Barbarian.grapple, both owned by Codex's Fighters.cs integration.
    public sealed class Grapple : MonoBehaviour
    {
        const float HoldDuration = 3f, GrabCooldown = .8f, ThrowCooldown = .8f, FlightDuration = .6f, FlightSpeed = 12f;
        const float GroundY = -.34f, CarryOffsetX = 1f, CarryOffsetY = .25f, FlightY = .25f, CollisionRadius = .8f;

        Barbarian hero;
        Enemy held, thrown;
        float holdRemaining, cooldown, flightTime;
        int holdDirection, throwDirection;

        public bool Holding => held&&held.Alive&&held.Captured;
        public Enemy Held => held;
        public Enemy Thrown => thrown;
        public float HoldRemaining => holdRemaining;
        public float Cooldown => cooldown;

        void Awake() { hero = GetComponent<Barbarian>(); }
        void OnDisable() { Cancel(); }
        void OnDestroy() { Cancel(); }

        public bool TryGrab()
        {
            if (!hero || !hero.CanAct) return false;
            if (hero.CarryingProp) return false;
            if (held || thrown) return false;
            if (hero.Attacking || hero.Dodging || hero.Parrying || hero.Headbutting) return false;
            if (cooldown > 0) return false;
            if (Game.I.body.leftArm.Stage >= 3) return false;
            hero.FaceEnemy();
            Enemy best = null; float bestDistance = float.MaxValue;
            foreach (var enemy in Game.I.enemies)
            {
                if (!enemy || !enemy.Alive || enemy.boss || enemy.elite || enemy.Captured) continue;
                float forward = (enemy.transform.position.x - hero.transform.position.x) * hero.facing;
                if (forward < 0 || forward > 1.6f) continue;
                if (Mathf.Abs(enemy.transform.position.y - hero.transform.position.y) >= 1.6f) continue;
                if (!(enemy.hp <= enemy.maxHp * .5f || enemy.stun > 0)) continue;
                if (forward < bestDistance) { bestDistance = forward; best = enemy; }
            }
            if (!best) return false;
            held = best; held.Captured = true;
            holdRemaining = HoldDuration; holdDirection = hero.facing; cooldown = GrabCooldown;
            held.transform.position=hero.transform.position+new Vector3(holdDirection*CarryOffsetX,CarryOffsetY,0);
            Game.I.Notice("사로잡음", "V로 던지거나, 정면 피해를 한 번 대신 막는다.", NordicArt.Gold);
            return true;
        }

        public bool Throw()
        {
            if (!hero || !hero.CanAct) return false;
            if (!Holding) return false;
            if (Game.I.body.leftArm.Stage >= 3) return false;
            var actor = held; held = null; holdRemaining = 0;
            thrown = actor; throwDirection = holdDirection; flightTime = FlightDuration; cooldown = ThrowCooldown;
            var pos = actor.transform.position; pos.y = FlightY; actor.transform.position = pos;
            Game.I.PlaySound(CombatCue.Throw);
            return true;
        }

        public bool AbsorbHit(float damage, Enemy source)
        {
            if (!hero || !hero.CanAct) return false;
            if (!Holding) return false;
            if(Game.I.body.leftArm.Stage>=3){Cancel();return false;}
            if (!source || !source.Alive || source == held) return false;
            float forward = (source.transform.position.x - hero.transform.position.x) * holdDirection;
            if (forward < 0) return false;
            var actor = held; held = null; holdRemaining = 0;
            DropToGround(actor);
            actor.TakeHit(damage, holdDirection, .2f);
            return true;
        }

        public void Cancel()
        {
            if (held) DropToGround(held);
            held = null; holdRemaining = 0;
            if (thrown) DropToGround(thrown);
            thrown = null; flightTime = 0;
        }

        public void ResetState()
        {
            Cancel();
            cooldown = 0;
        }

        void Update()
        {
            if (!Game.I || Game.I.Frozen) return;
            float dt = Time.deltaTime;
            if (cooldown > 0) cooldown = Mathf.Max(0, cooldown - dt);
            if (Game.I.phase != Phase.Combat || Game.I.body.leftArm.Stage >= 3)
            {
                if (held || thrown) Cancel();
                return;
            }
            if (held)
            {
                if (!held.Alive) { held = null; holdRemaining = 0; }
                else
                {
                    holdRemaining -= dt;
                    if (holdRemaining <= 0) Release();
                    else
                    {
                        var pos = hero.transform.position + new Vector3(holdDirection * CarryOffsetX, CarryOffsetY, 0);
                        held.transform.position = pos;
                        hero.facing = holdDirection;
                    }
                }
            }
            if (thrown)
            {
                if (!thrown.Alive) { thrown = null; flightTime = 0; }
                else UpdateFlight(dt);
            }
        }

        void Release()
        {
            var actor = held; held = null; holdRemaining = 0;
            if (!actor) return;
            DropToGround(actor);
            actor.Stun(.5f);
        }

        void UpdateFlight(float dt)
        {
            float step=Mathf.Min(dt,Mathf.Max(0,flightTime));flightTime -= dt;
            float prevX = thrown.transform.position.x;
            float newX = Mathf.Clamp(prevX + throwDirection * FlightSpeed * step, -18f, 18f);
            var pos = thrown.transform.position; pos.x = newX; pos.y = FlightY; thrown.transform.position = pos;
            bool boundary = newX <= -18f || newX >= 18f;
            Enemy hit = FindFlightCollision(prevX, newX);
            if (hit != null || flightTime <= 0 || boundary) EndFlight(hit);
        }

        Enemy FindFlightCollision(float prevX, float newX)
        {
            float segMin = Mathf.Min(prevX, newX) - CollisionRadius, segMax = Mathf.Max(prevX, newX) + CollisionRadius;
            Enemy best = null; float bestAhead = float.MaxValue;
            foreach (var enemy in Game.I.enemies)
            {
                if (!enemy || enemy == thrown || !enemy.Alive || enemy.Captured) continue;
                float ex = enemy.transform.position.x;
                if (ex < segMin || ex > segMax) continue;
                if (Mathf.Abs(enemy.transform.position.y - FlightY) >= 1.6f) continue;
                float ahead = (ex - prevX) * throwDirection;
                if (ahead < 0) continue;
                if (ahead < bestAhead) { bestAhead = ahead; best = enemy; }
            }
            return best;
        }

        void EndFlight(Enemy other)
        {
            var actor = thrown; thrown = null; flightTime = 0;
            if (!actor) return;
            DropToGround(actor);
            if (other && other.Alive)
            {
                other.TakeHit(24, throwDirection, .6f);
                Game.I.Burst(other.transform.position, NordicArt.Gold, 10, 4);
                Game.I.PlaySound(CombatCue.PropImpact);
            }
            if (actor.Alive) actor.TakeHit(20, throwDirection, .8f);
        }

        static void DropToGround(Enemy enemy)
        {
            if (!enemy) return;
            enemy.Captured = false;
            if(enemy.Alive)enemy.Stun(.5f);
            var pos = enemy.transform.position; pos.y = GroundY; pos.x = Mathf.Clamp(pos.x, -18f, 18f); enemy.transform.position = pos;
        }
    }
}

