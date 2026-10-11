using UnityEngine;

namespace Valhalla
{
    public sealed class GameHUD : MonoBehaviour
    {
        Font font;
        GUIStyle label, button;
        Texture2D portrait;
        readonly Color ink = NordicArt.Hex("111D25"), pale = NordicArt.Hex("E5E0D1"), muted = NordicArt.Hex("99AAA9");
        Game G => Game.I;
        void EnsureStyles()
        {
            if (label != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 24);
            portrait = Resources.Load<Texture2D>("Art/BarbarianConcept-v1");
            label = new GUIStyle(GUI.skin.label) { font = font, wordWrap = true, richText = false, padding = new RectOffset(0, 0, 0, 0) };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 19, wordWrap = true, padding = new RectOffset(18, 18, 10, 10) };
        }
        void Fill(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        void Text(float x, float y, float w, float h, string text, int size, Color color, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            label.fontSize = size; label.normal.textColor = color; label.alignment = alignment; GUI.Label(new Rect(x, y, w, h), text, label);
        }
        bool Button(Rect r, string title)
        {
            Fill(r, NordicArt.Hex("304147")); Fill(new Rect(r.x, r.y, 3, r.height), NordicArt.Gold);
            var old = GUI.backgroundColor; GUI.backgroundColor = Color.clear;
            button.normal.textColor = pale; button.hover.textColor = NordicArt.Gold;
            bool result = GUI.Button(r, title, button); GUI.backgroundColor = old; return result;
        }
        void Bar(float x, float y, float w, float h, float proportion, Color color)
        { Fill(new Rect(x, y, w, h), NordicArt.Hex("253239")); Fill(new Rect(x, y, w * Mathf.Clamp01(proportion), h), color); }
        void OnGUI()
        {
            if (!G || !G.player) return;
            EnsureStyles();
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1600f, Screen.height / 900f, 1));
            Fill(new Rect(0, 0, 1600, 125), new Color(.04f, .075f, .10f, .94f));
            Text(38, 22, 380, 30, "BAAAALHEEELAAA", 25, pale);
            Text(40, 59, 380, 25, "전진하라. 몸에 대가를 새겨라.", 15, muted);
            Text(40, 94, 390, 20, "COMBAT PROTOTYPE   /   0.1", 11, NordicArt.Gold);
            Text(470, 24, 340, 26, "생명  /  " + HealthName(), 18, pale);
            Bar(470, 61, 305, 9, G.player.hp / 110f, G.player.hp <= 30 ? NordicArt.Red : NordicArt.Gold);
            Text(470, 84, 345, 24, G.debug ? $"HP {G.player.hp:0}/110   ·   피해 기록 {G.damageTaken:0}" : "위험을 감수하되, 쓰러지지는 마라.", 13, muted);
            Text(835, 24, 270, 26, "명예  /  " + Mathf.RoundToInt(G.honor), 18, pale);
            Bar(835, 61, 190, 9, G.honor / 100f, NordicArt.Ice);
            Text(835, 84, 340, 25, G.retreatTime > 2.5f ? "장시간 도주 · 명예 감소 중" : "패링과 전진 회피로 증명하라.", 13, G.retreatTime > 2.5f ? NordicArt.Red : muted);
            Text(1210, 25, 350, 32, G.phase == Phase.Camp ? "잿빛 문턱" : $"시련  {G.wave:00} / 03", 22, NordicArt.Gold, TextAnchor.UpperRight);
            Text(1210, 69, 350, 25, $"처치 {G.kills:00}    패링 {G.parries:00}    {Mathf.FloorToInt(G.runTime / 60):00}:{Mathf.FloorToInt(G.runTime % 60):00}", 15, muted, TextAnchor.UpperRight);

            BodyPanel(38, 157, G.body.arm);
            BodyPanel(38, 282, G.body.legs);
            DrawEnemies();
            if (G.phase == Phase.Combat && !G.choice.HasValue && Time.unscaledTime < G.noticeUntil)
            {
                Text(410, 153, 760, 42, G.noticeTitle, 25, G.noticeColor, TextAnchor.MiddleCenter);
                Text(400, 198, 800, 55, G.noticeText, 16, pale, TextAnchor.UpperCenter);
            }
            Fill(new Rect(0, 821, 1600, 79), new Color(.04f, .075f, .10f, .95f));
            Text(38, 838, 1520, 30, "A D 이동    SPACE 점프    좌클릭 / J 일반 공격    우클릭 / K 강공격    SHIFT 전진 회피    C 패링", 18, pale, TextAnchor.UpperCenter);
            Text(40, 875, 1520, 22, G.debug ? "개발 도구  ·  F6 오른팔 피격   F7 다리 피격   F8 체력 회복   |   F1 닫기" : "TAB 도움말    ESC 일시정지    F1 개발 도구     ·     강공격은 누르는 순간 준비 동작을 시작합니다.", 12, muted, TextAnchor.UpperCenter);
            if (G.phase == Phase.Camp) Camp();
            else if (G.phase == Phase.Death || G.phase == Phase.Victory) Result();
            
            else if (G.paused) Pause();
            else if (G.help) Help();
        }
        string HealthName() => G.player.hp > 85 ? "정상" : G.player.hp > 60 ? "경상" : G.player.hp > 35 ? "중상" : G.player.hp > 15 ? "치명상" : "빈사";
        void BodyPanel(float x, float y, BodyState state)
        {
            Fill(new Rect(x, y, 290, 111), new Color(.055f, .095f, .12f, .91f));
            Color color = state.Stage == 3 ? NordicArt.Red : state.Stage == 2 ? NordicArt.Gold : NordicArt.Ice;
            Fill(new Rect(x, y, 3, 111), color);
            string stage = state.Stage == 0 ? "정상" : state.Stage == 1 ? "손상" : state.Stage == 2 ? "부상 / 적응" : "심각 / 문신 침묵";
            Text(x + 15, y + 10, 265, 25, Game.PartName(state.part) + "   ·   " + stage, 17, color);
            for (int i = 0; i < 3; i++) Fill(new Rect(x + 15 + i * 85, y + 43, 76, 5), state.Stage > i ? color : NordicArt.Hex("334147"));
            string tattoo = state.part == BodyPart.RightArm ? "번개 강공격 · 처치 회복" : "돌진 충돌";
            Text(x + 15, y + 58, 260, 24, state.TattoosEnabled ? tattoo : "모든 문신 / 융합 효과 꺼짐", 13, state.TattoosEnabled ? pale : NordicArt.Red);
            string detail = state.Stage == 3 ? (state.part == BodyPart.RightArm ? "후딜 ×1.9 · 강공격 피해 ×0.7" : "이동 ×0.48 · 돌진 속도 제한") : state.adaptation == Adaptation.Fury ? "위험 적응 · 해당 부위 피격 피해 +20%" : state.adaptation == Adaptation.Endure ? "인내 적응 · 기능 저하 완화" : "14 손상 → 30 자동 적응 → 55 심각";
            Text(x + 15, y + 83, 270, 24, G.debug ? $"외상 {state.trauma:0}  ·  {detail}" : detail, 11, muted);
        }
        void DrawEnemies()
        {
            foreach (var e in G.enemies)
            {
                if (!e || !e.Alive) continue;
                var world = G.cam.WorldToViewportPoint(e.transform.position + Vector3.up * (e.boss ? 3.25f : 1.75f));
                float x = world.x * 1600, y = (1 - world.y) * 900;
                if (x < 0 || x > 1600) continue;
                Bar(x - 48, y, 96, 5, e.hp / e.maxHp, e.boss ? NordicArt.Gold : NordicArt.Red);
                if (e.state == 1)
                    Text(x - 105, y - 56, 210, 48, e.timer<=.32f?"지금 C · 패링!":"예고 · " + (e.target == BodyPart.RightArm ? "오른팔 공격" : "다리 공격"), 18, e.timer<=.32f?NordicArt.Gold:NordicArt.Red, TextAnchor.MiddleCenter);
                else if (e.stun > 0) Text(x - 70, y - 35, 140, 30, "무너짐", 15, NordicArt.Ice, TextAnchor.MiddleCenter);
                else if (e.boss) Text(x - 130, y - 35, 260, 30, "뼈를 부수는 자", 18, NordicArt.Gold, TextAnchor.MiddleCenter);
            }
        }
        void Shade() { Fill(new Rect(0, 125, 1600, 696), new Color(.025f, .045f, .06f, .73f)); }
        void Camp()
        {
            Shade();
            if (portrait) GUI.DrawTexture(new Rect(1050, 183, 500, 615), portrait, ScaleMode.ScaleToFit, true);
            Text(440, 213, 760, 30, "THE ASHEN THRESHOLD", 16, NordicArt.Gold);
            Text(435, 255, 980, 83, "발할라는, 아직 멀었다.", 47, pale);
            Text(440, 363, 770, 75, "한 방의 무게. 문신의 힘.\n어둠 속에서 당신의 몸은 새로운 싸움법을 배운다.", 22, muted);
            Text(442, 481, 780, 90, "01   패링 후 강공격으로 적의 빈틈을 찌르세요.\n02   문신으로 강공격·처치·대시를 강화하세요. 부상은 자동 적용됩니다.\n03   부상 3단계에서는 해당 부위 문신이 모두 꺼집니다.", 18, pale);
            if (Button(new Rect(440, 625, 410, 65), "시련 시작    /    ENTER")) G.BeginRun();
            Text(440, 719, 800, 40, "기본 장착: 오른팔 — 번개 / 피의 룬     다리 — 질주의 룬", 14, muted);
        }
        void Result()
        {
            Shade(); bool win = G.phase == Phase.Victory;
            Text(400, 245, 820, 35, "VALKYRIE'S VERDICT", 16, NordicArt.Gold);
            Text(395, 298, 990, 75, win ? "오늘은, 자격을 증명했다." : "도끼를 놓기에는 이르다.", 40, pale);
            Text(400, 408, 850, 70, win ? "세 번의 시련을 통과했다. 문신의 힘을 활용해 다시 싸워보자." : "발키리: “그 정도 상처로 벌써 누울 셈인가?”", 21, muted);
            Text(400, 493, 850, 90, $"처치 {G.kills}     패링 {G.parries}     명예 {G.honor:0}\n오른팔 부상 {G.body.arm.Stage}단계     다리 부상 {G.body.legs.Stage}단계", 20, pale);
            if (Button(new Rect(400, 628, 420, 65), "다시 출정    /    ENTER 또는 R")) G.BeginRun();
            Text(400, 723, 850, 30, "이번 버전은 전투 / 부상 검증용입니다. 영구 흉터와 저장은 다음 단계입니다.", 14, muted);
        }
        void Pause()
        {
            Shade(); Text(500, 290, 600, 65, "잠시, 숨을 고른다.", 36, pale, TextAnchor.MiddleCenter);
            if (Button(new Rect(590, 418, 420, 60), "계속하기    /    ESC")) { G.paused = false; G.SyncPause(); }
            if (Button(new Rect(590, 500, 420, 60), "처음부터    /    R")) G.BeginRun();
            if (Button(new Rect(590, 582, 420, 60), "게임 종료")) Application.Quit();
        }
        void Help()
        {
            Fill(new Rect(430, 285, 750, 370), ink);
            Text(465, 315, 680, 45, "전사의 기억", 28, NordicArt.Gold);
            Text(465, 383, 680, 230, "강공격: 충전 없이 준비 → 타격 → 긴 후딜.\n패링: 적을 향해 F. 성공하면 긴 반격 기회.\n회피: 이동 입력 방향 우선. 무입력은 바라보는 방향.\n부상: 오른팔 공격 / 다리 공격을 맞으면 외상 누적.\n외상 30: 자동 적응. 외상 55: 문신 전체 차단.\n체력 회복은 부상을 치료하지 않음. 재출정 시 초기화.\nTAB 닫기 · 도움말을 열어도 전투는 계속됩니다.", 18, pale);
        }
    }
}
