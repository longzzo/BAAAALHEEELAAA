using UnityEngine;

namespace Valhalla.Campaign
{
    // Locked entry gate: lever opens quietly once enemies are cleared, smashing it open is faster
    // but calls reinforcements. Visuals are children of this transform, so Destroy(gameObject)
    // (done by CampaignDirector.ClearWorld) tears everything down with no timers to cancel.
    public sealed class DungeonGate : MonoBehaviour
    {
        CampaignDirector director;
        bool isOpen, noiseFired;
        int hitsRemaining;
        Transform gateVisual;
        SpriteRenderer leverGlow;
        const float AnchorY = -1.35f;

        public bool IsOpen => isOpen;
        public int HitsRemaining => hitsRemaining;
        public float LeverX => -10f;
        public float GateX => 12f;

        public void Initialize(CampaignDirector owner)
        {
            director = owner; isOpen = false; noiseFired = false; hitsRemaining = 2;
            transform.position = Vector3.zero;
            BuildGate(); BuildLever();
        }

        void BuildGate()
        {
            gateVisual = new GameObject("Dungeon gate").transform; gateVisual.SetParent(transform, false);
            gateVisual.localPosition = new Vector2(GateX, AnchorY + 1.9f);
            NordicArt.Block("Shadow", gateVisual, Vector2.zero, new Vector2(1.65f, 3.8f), NordicArt.Hex("161C1A"), 15);
            for(int i=0;i<6;i++) {
                float x=-.65f+i*.26f;
                NordicArt.Block("Weathered plank", gateVisual, new Vector2(x,0), new Vector2(.235f,3.7f), NordicArt.Hex(i%2==0?"42372C":"514433"),16);
                NordicArt.Block("Wood grain", gateVisual, new Vector2(x+.06f,.2f*(i%3-1)), new Vector2(.025f,3.2f), NordicArt.Hex("302D25"),17);
            }
            foreach(float y in new[]{-1.2f,1.2f}) {
                NordicArt.Block("Iron brace", gateVisual,new Vector2(0,y),new Vector2(1.7f,.22f),NordicArt.Hex("505D58"),18);
                foreach(float x in new[]{-.63f,0,.63f})NordicArt.Block("Iron rivet",gateVisual,new Vector2(x,y),new Vector2(.075f,.075f),NordicArt.Hex("A09D7E"),19);
            }
            foreach(float x in new[]{-.95f,.95f})NordicArt.Block("Stone post",gateVisual,new Vector2(x,0),new Vector2(.25f,3.8f),NordicArt.Hex("424D46"),18);
            NordicArt.Block("Lintel",gateVisual,new Vector2(0,1.8f),new Vector2(2.25f,.24f),NordicArt.Hex("637065"),19);
            NordicArt.Block("Rune",gateVisual,new Vector2(0,.2f),new Vector2(.06f,.65f),NordicArt.Gold,20);
            NordicArt.Block("Rune branch",gateVisual,new Vector2(.12f,.35f),new Vector2(.32f,.06f),NordicArt.Gold,20,35);
        }

        void BuildLever()
        {
            var leverRoot = new GameObject("Gate lever").transform; leverRoot.SetParent(transform, false);
            leverRoot.localPosition = new Vector2(LeverX, AnchorY + .65f);
            NordicArt.Block("Lever post", leverRoot, Vector2.zero, new Vector2(.18f, 1.3f), NordicArt.Hex("4A3A28"), 15);
            leverGlow = NordicArt.Block("Lever handle", leverRoot, new Vector2(.26f, .5f), new Vector2(.55f, .14f), NordicArt.Gold, 16, 20).GetComponent<SpriteRenderer>();
        }

        public bool TryLever()
        {
            var g = Game.I;
            if (!g || g.phase != Phase.Combat || isOpen) return false;
            if (g.enemies.Count > 0)
            {
                director.message = "적을 먼저 제압해야 지레를 당길 수 있다.";
                return false;
            }
            Open(false);
            g.PlaySound(CombatCue.GateOpen);
            return true;
        }

        public bool Strike(bool heavy)
        {
            var g = Game.I;
            if (!g || g.phase != Phase.Combat || isOpen || !heavy) return false;
            if (g.enemies.Count > 0) return false;
            hitsRemaining = Mathf.Max(0, hitsRemaining - 1);
            g.Burst((Vector2)transform.position + new Vector2(GateX, AnchorY + 1.2f), NordicArt.Gold, 16, 6);
            g.PlaySound(CombatCue.GateBreak);
            if (hitsRemaining > 0)
            {
                director.message = "관문이 흔들린다 · 강공격 한 번 더 필요하다.";
                return true;
            }
            Open(true);
            return true;
        }

        void Open(bool noisy)
        {
            isOpen = true;
            if (gateVisual) gateVisual.gameObject.SetActive(false);
            if (leverGlow) { var c = leverGlow.color; c.a = .35f; leverGlow.color = c; }
            if (noisy)
            {
                if (!noiseFired)
                {
                    noiseFired = true;
                    director.GateBroken();
                    director.message = "문을 부수고 들어갔다 · 소음이 지원군을 불렀다. 방 완료 보상은 한 번만 지급된다.";
                    Game.I.Notice("관문이 부서졌다", "소음이 적을 불렀다. 지레를 썼다면 조용했을 것이다.", NordicArt.Red, 4);
                }
            }
            else
            {
                director.message = "지레를 당겨 관문을 조용히 열었다.";
                Game.I.Notice("관문이 열렸다", "소음 없이 길을 열었다.", NordicArt.Ice, 3);
            }
        }
    }
}

