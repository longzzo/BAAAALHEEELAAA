using UnityEngine;

namespace Valhalla
{
    // View-only world feedback for the three core tattoo powers. Reads TattooSignals + a position/facing/velocity
    // snapshot off Game.I.player; never touches combat state, timers or BodyBuild. No gameplay Random: every
    // shape below is a fixed deterministic pattern, only re-centered on the hero's current position/direction.
    public sealed class TattooWorldFeedback : MonoBehaviour
    {
        const int PoolSize = 6;
        const float EffectLife = .22f;
        const float EffectWidth = .06f;

        sealed class Slot
        {
            public LineRenderer line;
            public float remaining;
        }

        readonly Slot[] pool = new Slot[PoolSize];
        int nextSlot;
        Material lineMaterial;

        public int ActiveCount { get; private set; }
        public TattooPower LastPower { get; private set; }
        public Vector2 LastDirection { get; private set; }

        void Awake()
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            for (int i = 0; i < PoolSize; i++)
            {
                var o = new GameObject("TattooFeedback" + i);
                o.transform.SetParent(transform, false);
                var line = o.AddComponent<LineRenderer>();
                line.sharedMaterial = lineMaterial;
                line.useWorldSpace = true;
                line.textureMode = LineTextureMode.Stretch;
                line.numCapVertices = 0;
                line.numCornerVertices = 0;
                line.sortingOrder = 50;
                line.startWidth = line.endWidth = EffectWidth;
                line.enabled = false;
                pool[i] = new Slot { line = line, remaining = 0 };
            }
        }

        void OnEnable() { TattooSignals.Activated += OnActivated; }
        void OnDisable() { TattooSignals.Activated -= OnActivated; Clear(); }
        void OnDestroy() { foreach(var slot in pool)if(slot?.line)Destroy(slot.line.gameObject); if (lineMaterial) Destroy(lineMaterial); }

        // Called by room-change integration to drop any effects left over from the previous room.
        public void Clear()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                pool[i].remaining = 0;
                pool[i].line.positionCount = 0;
                pool[i].line.enabled = false;
            }
            ActiveCount = 0;
        }

        void OnActivated(TattooPower power)
        {
            if (power != TattooPower.Thunder && power != TattooPower.Ram && power != TattooPower.Reprisal) return;
            var g = Game.I;
            if (!g || g.phase != Phase.Combat || !g.player) return;
            if (!g.body.Has(power)) return;
            var player = g.player;
            Vector2 origin = player.transform.position;
            int facing = player.facing;
            switch (power)
            {
                case TattooPower.Thunder: SpawnThunder(origin, facing); break;
                case TattooPower.Ram: SpawnRam(origin, player.velocity, facing); break;
                case TattooPower.Reprisal: SpawnReprisal(origin, facing); break;
            }
        }

        // Jagged lightning bolt at the hero's front, independent of dash/parry geometry below.
        void SpawnThunder(Vector2 origin, int facing)
        {
            float x = origin.x + facing * 1.9f;
            float topY = origin.y + .95f;
            Vector3[] points =
            {
                new Vector3(x, topY),
                new Vector3(x + facing * .14f, topY - .32f),
                new Vector3(x - facing * .18f, topY - .6f),
                new Vector3(x + facing * .16f, topY - .9f),
                new Vector3(x - facing * .1f, topY - 1.16f),
            };
            Configure(NextSlot(), points, NordicArt.Ice);
            LastPower = TattooPower.Thunder;
            LastDirection = new Vector2(facing, 0);
        }

        // Two forward chevrons aligned with the dash's current velocity direction (input direction), not facing.
        void SpawnRam(Vector2 origin, Vector2 velocity, int facing)
        {
            Vector2 forward = new Vector2(Mathf.Abs(velocity.x)>.001f?Mathf.Sign(velocity.x):facing,0);
            Vector2 perp = new Vector2(-forward.y, forward.x);
            for (int i = 0; i < 2; i++)
            {
                Vector2 center = origin + forward * (.7f + i * .42f);
                Vector2 tip = center + forward * .4f;
                Vector3[] points = { center - perp * .3f, tip, center + perp * .3f };
                Configure(NextSlot(), points, NordicArt.Gold);
            }
            LastPower = TattooPower.Ram;
            LastDirection = forward;
        }

        // Outward shield arc bulging toward facing, distinct from the lightning bolt and the chevrons above.
        void SpawnReprisal(Vector2 origin, int facing)
        {
            const int segments = 7;
            Vector3[] points = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                float angle = Mathf.Lerp(-55f, 55f, t) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle) * facing, Mathf.Sin(angle));
                points[i] = origin + dir * 1.1f;
            }
            Configure(NextSlot(), points, NordicArt.Ice);
            LastPower = TattooPower.Reprisal;
            LastDirection = new Vector2(facing, 0);
        }

        Slot NextSlot()
        {
            var slot = pool[nextSlot];
            nextSlot = (nextSlot + 1) % PoolSize;
            return slot;
        }

        void Configure(Slot slot, Vector3[] points, Color color)
        {
            if(slot.remaining<=0)ActiveCount++;
            slot.line.positionCount = points.Length;
            slot.line.SetPositions(points);
            slot.line.startColor = slot.line.endColor = color;
            slot.line.enabled = true;
            slot.remaining = EffectLife;
        }

        void Update()
        {
            int active = 0;
            for (int i = 0; i < PoolSize; i++)
            {
                var slot = pool[i];
                if (slot.remaining <= 0) continue;
                // Scaled dt: a menu pause or hitstop (Time.timeScale == 0) freezes this geometry in place too.
                slot.remaining -= Time.deltaTime;
                if (slot.remaining <= 0)
                {
                    slot.remaining = 0;
                    slot.line.enabled = false;
                    continue;
                }
                active++;
                float t = Mathf.Clamp01(slot.remaining / EffectLife);
                var c = slot.line.startColor; c.a = t;
                slot.line.startColor = slot.line.endColor = c;
            }
            ActiveCount = active;
        }
    }
}
