using UnityEngine;

namespace Valhalla
{
    // Code-native placeholder art. Replace the visual children without changing combat logic.
    public static class NordicArt
    {
        static Sprite square;
        public static readonly Color Gold = Hex("D8B16B");
        public static readonly Color Ice = Hex("7DC7D1");
        public static readonly Color Red = Hex("BD5149");
        public static Color Hex(string code) { ColorUtility.TryParseHtmlString("#" + code, out var c); return c; }
        public static Sprite Square
        {
            get
            {
                if (square != null) return square;
                var t = new Texture2D(2, 2); t.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); t.Apply();
                t.filterMode = FilterMode.Point;
                return square = Sprite.Create(t, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 2);
            }
        }
        public static Transform Block(string name, Transform parent, Vector2 pos, Vector2 size, Color color, int order = 0, float angle = 0)
        {
            var o = new GameObject(name); o.transform.SetParent(parent, false); o.transform.localPosition = pos;
            o.transform.localScale = new Vector3(size.x, size.y, 1); o.transform.localRotation = Quaternion.Euler(0, 0, angle);
            var sr = o.AddComponent<SpriteRenderer>(); sr.sprite = Square; sr.color = color; sr.sortingOrder = order; return o.transform;
        }
        static void Polygon(string name, Vector2[] points, Color color, int order)
        {
            var o = new GameObject(name); var mesh = new Mesh(); var vertices = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++) vertices[i] = points[i];
            var tri = new int[(points.Length - 2) * 3];
            for (int i = 0; i < points.Length - 2; i++) { tri[i * 3] = 0; tri[i * 3 + 1] = i + 1; tri[i * 3 + 2] = i + 2; }
            mesh.vertices = vertices; mesh.triangles = tri; mesh.RecalculateBounds();
            o.AddComponent<MeshFilter>().mesh = mesh;
            var mr = o.AddComponent<MeshRenderer>(); mr.sharedMaterial = new Material(Shader.Find("Sprites/Default")); mr.sharedMaterial.color = color; mr.sortingOrder = order;
        }
        public static void World()
        {
            var role = Game.I ? Game.I.sceneRole : Campaign.SceneRole.Legacy;
            // Forest/mine have no painted plate yet, so they always get a distinct procedural dressing.
            if (role == Campaign.SceneRole.Forest) { if (!Backdrop("Art/Forest-v1")) ForestFloor(); return; }
            if (role == Campaign.SceneRole.Mine) { if (!Backdrop("Art/Mine-v1",.535f)) MineFloor(); return; }
            // Village gets its own plate when present; Legacy, Lobby and Sanctum reuse the old fjord backdrop.
            string plate = role == Campaign.SceneRole.Village ? "Art/Village-v1" : "Art/AshenThreshold-v1";
            if (Backdrop(plate,role == Campaign.SceneRole.Village ? .70f : .68f)) return;
            FjordFloor();
        }
        static bool Backdrop(string resourceName,float floorFromTop=.68f)
        {
            var painting = Resources.Load<Texture2D>(resourceName);
            if (!painting) return false;
            var backdrop = new GameObject("Painted environment");
            var renderer = backdrop.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(painting, new Rect(0, 0, painting.width, painting.height), new Vector2(.5f, .5f), 100);
            renderer.sortingOrder = -100;
            float height = 44f * painting.height / painting.width;
            backdrop.transform.localScale = Vector3.one * (4400f / painting.width);
            // The generated plate's floor line sits at about 68% from its top.
            backdrop.transform.position = new Vector3(0, -1.43f + (floorFromTop-.5f) * height, 0);
            return true;
        }
        static void FjordFloor()
        {
            Block("Night", null, new Vector2(0, 5), new Vector2(150, 60), Hex("101C25"), -100);
            Block("Moon", null, new Vector2(-8, 7.8f), new Vector2(2.1f, 2.1f), Hex("8BACA7"), -95, 45);
            Block("Moon veil", null, new Vector2(-7.5f, 8.2f), new Vector2(2, 2), Hex("101C25"), -94, 45);
            for (int i = -5; i < 7; i++)
            {
                float x = i * 9;
                Polygon("Fjord", new[] { new Vector2(x - 8, -2), new Vector2(x, 8 + (i % 3)), new Vector2(x + 9, -2) }, Hex("1B2B35"), -90);
                Block("Mist", null, new Vector2(x, 1.3f), new Vector2(12, 1.2f), new Color(.23f, .35f, .4f, .12f), -75);
            }
            for (int i = -9; i < 10; i++)
            {
                float x = i * 3.6f;
                Block("Dead pine", null, new Vector2(x, 1.4f), new Vector2(.17f, 6), Hex("15232A"), -60);
                for (int j = 0; j < 4; j++)
                {
                    Block("Branch", null, new Vector2(x - .42f, 2 + j * .6f), new Vector2(1.25f - j * .15f, .1f), Hex("15232A"), -60, 28);
                    Block("Branch", null, new Vector2(x + .42f, 2 + j * .6f), new Vector2(1.25f - j * .15f, .1f), Hex("15232A"), -60, -28);
                }
            }
            Block("Ground", null, new Vector2(0, -2.4f), new Vector2(44, 2), Hex("1C2226"), -10);
            Block("Moss edge", null, new Vector2(0, -1.43f), new Vector2(44, .15f), Hex("52635B"), -9);
            for (int i = -40; i < 41; i++)
            {
                Block("Stone", null, new Vector2(i * .52f, -1.75f - Mathf.Abs(i % 4) * .15f), new Vector2(.4f, .11f), Hex("303B3D"), -8, i % 7);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 17;
                Block("Gate pillar", null, new Vector2(x, 1.25f), new Vector2(1.1f, 5.4f), Hex("354448"), -25);
                Block("Pillar inset", null, new Vector2(x, 1.4f), new Vector2(.65f, 4.8f), Hex("223139"), -24);
                Block("Rune slash", null, new Vector2(x, 1.6f), new Vector2(.08f, 1.15f), Ice, -23, 20);
                Block("Rune slash", null, new Vector2(x + .14f, 1.8f), new Vector2(.5f, .08f), Ice, -23, -30);
            }
            for (int i = -2; i <= 2; i++)
            {
                float x = i * 6.5f;
                Block("Brazier post", null, new Vector2(x, -.6f), new Vector2(.13f, 1.65f), Hex("514338"), -5);
                Block("Brazier bowl", null, new Vector2(x, .12f), new Vector2(.65f, .2f), Hex("736451"), -4);
                var flame = Block("Fire", null, new Vector2(x, .5f), new Vector2(.32f, .7f), Gold, -3, 12);
                flame.gameObject.AddComponent<Flame>();
            }
        }
        static void ForestFloor()
        {
            Block("Canopy dark", null, new Vector2(0, 5), new Vector2(150, 60), Hex("0B140F"), -100);
            for (int i = -5; i < 7; i++)
            {
                float x = i * 9;
                Polygon("Ridge", new[] { new Vector2(x - 8, -2), new Vector2(x, 7 + (i % 3)), new Vector2(x + 9, -2) }, Hex("132018"), -90);
                Block("Fog", null, new Vector2(x, 1.1f), new Vector2(12, 1.1f), new Color(.15f, .25f, .18f, .14f), -75);
            }
            for (int i = -10; i < 11; i++)
            {
                float x = i * 3.2f;
                Block("Trunk", null, new Vector2(x, 1.6f), new Vector2(.42f, 6.4f), Hex("1A2A1C"), -60);
                for (int j = 0; j < 5; j++)
                {
                    Block("Bough", null, new Vector2(x - .55f, 2.2f + j * .55f), new Vector2(1.5f - j * .18f, .14f), Hex("16241A"), -60, 22);
                    Block("Bough", null, new Vector2(x + .55f, 2.2f + j * .55f), new Vector2(1.5f - j * .18f, .14f), Hex("16241A"), -60, -22);
                }
            }
            Block("Ground", null, new Vector2(0, -2.4f), new Vector2(44, 2), Hex("141A14"), -10);
            Block("Moss edge", null, new Vector2(0, -1.43f), new Vector2(44, .15f), Hex("3C4A34"), -9);
            for (int i = -40; i < 41; i++)
                Block("Root", null, new Vector2(i * .5f, -1.78f - Mathf.Abs(i % 5) * .12f), new Vector2(.5f, .1f), Hex("23301F"), -8, i % 9);
            for (int i = -2; i <= 2; i++)
            {
                float x = i * 6.5f;
                Block("Torch post", null, new Vector2(x, -.55f), new Vector2(.12f, 1.55f), Hex("2A2118"), -5);
                var flame = Block("Fire", null, new Vector2(x, .35f), new Vector2(.28f, .62f), Hex("8FB36A"), -3, 10);
                flame.gameObject.AddComponent<Flame>();
            }
        }
        static void MineFloor()
        {
            Block("Cave dark", null, new Vector2(0, 5), new Vector2(150, 60), Hex("0E0C0C"), -100);
            for (int i = -5; i < 7; i++)
            {
                float x = i * 9;
                Polygon("Rock wall", new[] { new Vector2(x - 8, -2), new Vector2(x, 7 + (i % 2)), new Vector2(x + 9, -2) }, Hex("23201D"), -90);
                Block("Dust haze", null, new Vector2(x, 1.0f), new Vector2(12, 1), new Color(.3f, .26f, .2f, .12f), -75);
            }
            for (int i = -9; i < 10; i++)
            {
                float x = i * 3.6f;
                Block("Stalactite", null, new Vector2(x, 4.6f), new Vector2(.5f, 2.4f), Hex("30291F"), -60);
                Block("Support beam", null, new Vector2(x, 1.4f), new Vector2(.2f, 5.6f), Hex("3A2E1E"), -60);
            }
            Block("Ground", null, new Vector2(0, -2.4f), new Vector2(44, 2), Hex("17120F"), -10);
            Block("Gravel edge", null, new Vector2(0, -1.43f), new Vector2(44, .15f), Hex("4A3A28"), -9);
            for (int i = -40; i < 41; i++)
                Block("Rubble", null, new Vector2(i * .52f, -1.75f - Mathf.Abs(i % 4) * .15f), new Vector2(.4f, .11f), Hex("2E2419"), -8, i % 7);
            for (int i = -2; i <= 2; i++)
            {
                float x = i * 6.5f;
                Block("Ore vein", null, new Vector2(x, -.1f), new Vector2(.16f, 1.1f), Ice, -5, 15);
                var flame = Block("Lantern", null, new Vector2(x, .55f), new Vector2(.24f, .5f), Gold, -3);
                flame.gameObject.AddComponent<Flame>();
            }
        }
        public static Transform Warrior(Transform parent, bool enemy, bool boss, out Transform weapon)
        {
            var root = new GameObject("Visual").transform; root.SetParent(parent, false);
            Color skin = enemy ? Hex("83978F") : Hex("C5A68A");
            Color cloth = enemy ? Hex("48575B") : Hex("785143");
            Block("Back leg", root, new Vector2(-.21f, -.64f), new Vector2(.28f, .6f), Hex("383F42"), 10);
            Block("Front leg", root, new Vector2(.21f, -.64f), new Vector2(.31f, .6f), Hex("55544B"), 12);
            Block("Boot", root, new Vector2(.28f, -.94f), new Vector2(.47f, .2f), Hex("272C2F"), 13);
            Block("Boot", root, new Vector2(-.18f, -.94f), new Vector2(.39f, .2f), Hex("272C2F"), 13);
            Block("Torso", root, new Vector2(0, -.01f), new Vector2(.88f, .86f), skin, 14);
            Block("Tunic", root, new Vector2(0, -.43f), new Vector2(.87f, .37f), cloth, 16);
            Block("Belt", root, new Vector2(0, -.3f), new Vector2(.94f, .12f), Hex("302C2B"), 17);
            Block("Buckle", root, new Vector2(.08f, -.3f), new Vector2(.17f, .16f), Gold, 18);
            Block("Fur shoulders", root, new Vector2(-.1f, .38f), new Vector2(1.14f, .3f), Hex("545956"), 18, -5);
            Block("Head", root, new Vector2(.03f, .67f), new Vector2(.48f, .5f), skin, 19);
            Block("Hair", root, new Vector2(-.08f, .94f), new Vector2(.51f, .17f), Hex("433A32"), 20);
            Block("Beard", root, new Vector2(.13f, .44f), new Vector2(.42f, .32f), enemy ? Hex("414E4C") : Hex("784C34"), 22, -8);
            Block("Eye", root, new Vector2(.22f, .74f), new Vector2(.09f, .06f), enemy ? Red : Ice, 24);
            Block("Arm", root, new Vector2(.48f, .03f), new Vector2(.29f, .64f), skin, 21, 12);
            Block("Tattoo", root, new Vector2(.5f, .11f), new Vector2(.08f, .31f), enemy ? Red : Ice, 23, -12);
            weapon = new GameObject("Axe pivot").transform; weapon.SetParent(root, false); weapon.localPosition = new Vector2(.46f, .05f);
            Block("Haft", weapon, new Vector2(.22f, .25f), new Vector2(.12f, 1.55f), Hex("735E45"), 26, -20);
            Block("Steel head", weapon, new Vector2(.61f, .8f), new Vector2(.73f, .53f), enemy ? Hex("8B9291") : Hex("A4B7B7"), 27, -20);
            Block("Edge", weapon, new Vector2(.89f, .7f), new Vector2(.14f, .62f), Hex("D8DECF"), 28, -20);
            if (boss)
            {
                Block("Crown", root, new Vector2(.02f, 1.04f), new Vector2(.7f, .2f), Gold, 25);
                Block("Antler", root, new Vector2(-.37f, 1.32f), new Vector2(.13f, .7f), Hex("BFB59A"), 24, 35);
                Block("Antler", root, new Vector2(.37f, 1.32f), new Vector2(.13f, .7f), Hex("BFB59A"), 24, -35);
            }
            return root;
        }
    }
    public sealed class Flame : MonoBehaviour
    {
        Vector3 initial;
        void Start() { initial = transform.localScale; }
        void Update() { transform.localScale = initial * (1 + Mathf.Sin(Time.time * 11 + transform.position.x) * .13f); }
    }
    public sealed class Fleck : MonoBehaviour
    {
        public Vector2 velocity;
        public float life = .45f;
        float initial;
        SpriteRenderer sr;
        void Start() { initial = life; sr = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            life -= Time.deltaTime; transform.position += (Vector3)(velocity * Time.deltaTime); velocity += Vector2.down * (9 * Time.deltaTime);
            if (sr) { var c = sr.color; c.a = Mathf.Clamp01(life / initial); sr.color = c; }
            if (life <= 0) Destroy(gameObject);
        }
    }
}
