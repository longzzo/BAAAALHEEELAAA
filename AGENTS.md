# Project collaboration

- 0.16 current keys: C parry, V grab/throw (F/G retired). User said 0.15 was not bad but F/G reach uncomfortable; requested C/V. Fighters input plus HUD/tutorial/tattoo/prop/legacy labels updated. No timing changes. Source search found no duplicate C/V bindings or old live F/G prompts; Windows build successful. 297/21 are prior behavior checks, not rerun for key+text substitution. Old version notes use historical keys.

- 0.15 village HUD also compact by default: currency/weapon/equipped count, guide destination/direction; Tab restores detail and recent result, station prompts near bottom. Training and open facility panels retain full info. View-only change, Windows build and memory-profile compact/expanded/tattoo-shop captures checked; no repeat of entire 297/21 tests for this display-only edit.

- 0.15 supersedes 0.14 recovery/input notes after explicit user feedback (parry timing/dash responsiveness and intrusive UI): F/Shift buffer .25s, J/K .15s; defense may cancel final .12s of recovery ONLY after impact and when action cooldown is ready. No windup cancellation. Read-only Enemy.SecondsUntilThreat accounts for charge travel/vertical range/injury-dependent cue lead. Compact dungeon combat HUD by default, Tab toggles details without pausing; keep injury/activation alerts. Campaign297/0fail, combat21 and compact/expanded/injury screenshots verified. Claude scope audit report in parent outputs; feedback changes implemented by Codex.

- 0.14 tattoo world feedback: Claude authored TattooWorldFeedback, Codex integrated + corrected horizontal dash direction. Only Thunder/Ram/Reprisal have dedicated line shapes; not damage-range indicators. Six pooled renderers, owned shared material, scaled lifetime, BodyBuild.Has gate. Clear on room teardown/BeginRun/EndRun. Campaign292/0fail and combat21 pass; three controlled screenshot previews, not natural play footage. Do not consume gameplay Random in this view.

- 0.14 Claude review completed (../Claude-014-전투입력표시-검토보고.txt). HUD-button mouse clicks now excluded from combat via shared Rect geometry evaluated before OnGUI; keyboard input stays independent. Windows campaign286/0fail and combat21 passed after fix. Actual LoadRoom pending-input cleanup covered. Corpse proximity hint remains a UX follow-up, not a claim of implemented crate guidance.

- 0.14 input buffer: light/heavy/parry/dash keyboard/mouse requests retain latest input for .15 game seconds without cancelling heavy recovery. Hitstop preserves requests; menus, accepted hits, room cleanup, resets and EndRun clear them. Preserve dash-from-parry and requested dash direction. Direct action methods stay unbuffered. Windows campaign281/0fail, combat21; human feel still needs feedback.

- User priority confirmed 2026-10-11: (1) heavy/parry/dash control feel, then (2) tattoo ability differences and feedback, then (3) character/village/dungeon visual quality. Preserve this order for further work. Normal-combat probes are feasibility evidence, not proof of human feel; prioritize input/action transitions next instead of additional art or broad automated runs.

- Read README.md first. Use installed Unity 6000.6.0f1.
- Read the Notion development hub when continuing: https://app.notion.com/p/3f64ac9be092816b9e5edddf0fc05c1c . Keep confirmed decisions, proposals and actual implementation status separate.
- Campaign 0.4 implements lobby, village, three dungeon scenes, tutorial, byproducts, inventory, processing, crafting, equipment/tattoo shops and persistence. Read README.md and current test results for exact limits.
- Combat facing is automatic toward a living enemy, independent of movement. Dash prioritizes horizontal input, falling back to facing without input. Lock dash direction at launch. Parry window is 0.45 seconds, cooldown 0.60 seconds, with a final 0.32-second cue.
- Prefer the existing Unity CLI at C:/Users/back1/AppData/Local/Unity/bin/unity.exe. Pipeline 0.8.0-exp.1 is installed. Use connected-editor commands while the project is open.
- Preserve confirmed design: heavy attack begins a fixed windup immediately; no hold-to-charge.
- Tattoos are the primary ability system. At injury stage 2 apply adaptation automatically without a popup or injury pause. The old selection design is superseded.
- At stage 3, disable EVERY tattoo on the affected part including passive and fusion effects; increase penalties. Keep unaffected body parts independent.
- Tattoos are disabled for the run, not permanently deleted. Healing HP must not clear trauma.
- All tattoo effects must query BodyBuild.Has; never add a bypass in an attack or on-kill handler.
- Fatal hits take priority over adaptation feedback. Restart must clear pause, trauma, compatibility choice state and attack state.
- Prefer small playable changes. Document decisions, validation and next steps in README.md.
- Claude and Codex: one implementation owner at a time. Reviewers report findings without editing the same files. Do not run two Unity instances on this project.
- Do not modify Library, generated project files, installed Unity binaries, or Windows security settings.
- Run combat rule validation and runtime smoke tests when changing combat or state transitions.
- Do not add external assets until their source and license are recorded.

- Equipped tattoos, not all owned tattoos, define combat loadout. Catalog area: arm 2, legs 1. Preserve intentional empty loadouts and versioned migration.
- Scar unlocks are alternatives, never automatic buffs. Record each severe/fatal limb at most once per settled run. Pending Echo must query Has again and clear on room transition.

- 0.5 extends to 7 parts and 11 tattoos. Preserve enum IDs: Legs=1 means right leg. New reaction effects live in TattooCombatEffects; Recall spent flag belongs to ExpeditionState. All seven parts use BodyBuild.Has. Current area: RightArm 2, every other part 1. Old two-part notes are historical. See completion-audit.json for remaining scope.

- 0.6 has 15 tattoos, Fire/Giant typed scars and pure JudgmentRules. Only accepted severe/fatal damage earns typed scars at settlement once per expedition. Store pending combat effects separately from persistent unlocks. Preserve time grace for retreat classification. Current Windows campaign regression: 182 checks.

- 0.7 adds G grab/human shield/throw via Grapple. Enemy.Captured blocks AI. Only weak/stunned small foes are capturable; severe left arm cancels. All release/reset/room transitions must clear Captured without duplicate hits or drops. Windows campaign: 202 checks.

- 0.8 adds optional PressureTrial in Salvage rooms. Normal kill drops remain unique; corpse weights are visuals, not Enemy instances. Pause blocks actions, room changes destroy all weights, crate pushes require grounded swept contact. See README for remaining carry/throw limitations.

- 0.8.1 training uses temporary Thunder/Ram definitions owned and cleaned up by CampaignDirector. Never persist demo tattoos or overwrite intentional empty loadouts; completion/abort restores BuildBody from profile. First-completion reward and replay messaging remain distinct.
- 0.9 hero walk sprites are view-only in PaintedCombatVisual. Foot pivots are calibrated to BarbarianWalk-v1. Preserve reverse backpedal, paused frame/anchor, combat fallback and owned Sprite cleanup. Never destroy resource textures.
- 0.9.1 heavy sprites include the axe; hide the separate axe only while active and restore body/axe on exit. Read cached attack timing without changing combat. Hitstop displays impact pose; manual pause holds current pose. Heavy atlas uses calibrated nonuniform rectangles recorded in heavy-art-provenance.json.
- 0.10 adds environmental crate/corpse carry and throw via PressureTrial. G prioritizes an eligible enemy, otherwise a nearby prop. Holding blocks combat/jump and moves at 55%; props give no shield. Held/flying weights cannot press plates. Pause freezes flight; accepted hits, severe left arm and room changes cancel carry. Use the existing Enemy.TakeHit/drop path and clear flight before callbacks. Separate axe is also hidden during prop carry. Windows campaign: 255 checks; combat: 21.
- 0.11 audio: use Game.PlaySound(CombatCue), never frequency literals or gameplay Random. SoundPalette generates owned 44.1kHz clips, CombatAudio has eight voices, per-cue throttling and TattooSignals subscription. Menu pause freezes audio; hitstop must retain impact audio. Keep total mix headroom and destroy generated clips at scene teardown. Campaign: 259; combat: 21. Sound quality still needs listening/mix review; waveform tests are not an auditory judgment.
- 0.12 enemy attack sheet is view-only: Enemy states/timers select six poses. Preserve archetype timing, captured/stun cancellation, manual pause and hitstop behavior. Hide the separate axe only while its body-and-axe sheet is active. Use calibrated rectangles/feet in enemy-attack-art-provenance.json. Actual CampaignDirector spawns add PaintedCombatVisual; raw Game.Spawn does not. Campaign: 268; combat: 21.
- 0.13 preparation: locked tattoos still show their effect; TriggerHint/ProgressHint must agree with actual combat/unlocks. BuyTattoo auto-equips only if space fits. UnequipArmor preserves ownership/currency/weapon and saves none; it never heals trauma or HP. Campaign: 271; combat: 21. See Claude-프로토타입범위-검토보고.txt for scoped first-play review; resolve final requirement evidence before claiming goal completion.

