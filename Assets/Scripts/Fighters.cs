using System.Collections.Generic;
using UnityEngine;

namespace Valhalla
{
    public sealed class Barbarian : MonoBehaviour
    {
        public enum CombatInput { None, Light, Heavy, Parry, Dash }
        CombatInput pendingInput;
        float pendingUntil, pendingDirection;
        public bool HasBufferedInput => pendingInput != CombatInput.None;
        public CombatInput BufferedInput => pendingInput;
        public float AttackWindupRemaining => Attacking ? Mathf.Max(0,impactAt-AttackElapsed) : 0;
        public bool CanExitRecovery => Attacking && impacted && attackTimer<=.12f;
        public void ClearBufferedInput() { pendingInput=CombatInput.None; }
        public void QueueCombatInput(CombatInput input,float direction=0)
        {
            if(!Game.I || Game.I.phase!=Phase.Combat || Game.I.paused || Game.I.choice.HasValue || Holding)return;
            pendingInput=input; pendingDirection=direction; pendingUntil=Time.time+((input==CombatInput.Parry||input==CombatInput.Dash)?.25f:.15f);
            ConsumeBufferedInput();
        }
        void ConsumeBufferedInput()
        {
            if(pendingInput==CombatInput.None)return;
            if(Time.time>pendingUntil) { ClearBufferedInput(); return; }
            if(Holding) { ClearBufferedInput(); return; }
            if(!CanAct || Dodging || Headbutting || (Parrying && pendingInput!=CombatInput.Dash))return;
            if(pendingInput==CombatInput.Dash && dashCooldown>0 || pendingInput==CombatInput.Parry && parryCooldown>0)return;
            if(Attacking) {
                bool defense=pendingInput==CombatInput.Parry||pendingInput==CombatInput.Dash;
                if(!defense||!CanExitRecovery)return;
                attackTimer=0;
            }
            var input=pendingInput; float direction=pendingDirection; ClearBufferedInput();
            switch(input) {
                case CombatInput.Light: Attack(false); break;
                case CombatInput.Heavy: Attack(true); break;
                case CombatInput.Parry: Parry(); break;
                case CombatInput.Dash: Dash(direction); break;
            }
        }
        public float hp = 110;
        public int facing = 1;
        public Vector2 velocity;
        public float attackTimer, dashTimer, parryTimer, invulnerability, dashCooldown, parryCooldown;
        public float headbuttTimer, headbuttCooldown;
        public bool heavy;
        public TattooCombatEffects tattooEffects;
        public Grapple grapple;
        float attackLength, impactAt;
        bool impacted, headbuttImpacted;
        const float HeadbuttWindup = .16f, HeadbuttLength = .42f, HeadbuttCooldownDuration = 1.3f;
        Enemy facingTarget;
        int dashDirection=1;
        int headbuttDirection=1;
        float echoRemaining;
        Vector2 echoOrigin;
        int echoDirection;
        bool frostTriggered;
        public void ClearDelayedAttacks() { ClearBufferedInput(); echoRemaining=0; headbuttTimer=0; if(tattooEffects)tattooEffects.ClearPending(); if(grapple)grapple.Cancel(); if(Props)Props.CancelCarry(); }
        public Enemy FacingTarget => facingTarget;
        readonly HashSet<Enemy> dashHits = new HashSet<Enemy>();
        Transform visual, weapon;
        public bool Grounded => transform.position.y <= -.34f;
        public bool Attacking => attackTimer > 0;
        public bool Dodging => dashTimer > 0;
        public bool Parrying => parryTimer > 0;
        public bool Headbutting => headbuttTimer > 0;
        Campaign.PressureTrial Props => Campaign.CampaignDirector.I ? Campaign.CampaignDirector.I.pressureTrial : null;
        public bool CarryingProp => Props&&Props.Carrying;
        public bool Holding => grapple&&grapple.Holding || CarryingProp;
        // G has one owner: an existing hold throws first; otherwise eligible enemies take priority over props.
        public bool GrabOrThrow()
        {
            if(!CanAct)return false;
            if(CarryingProp)return Props.ThrowHeld();
            if(grapple.Holding)return grapple.Throw();
            if(grapple.TryGrab())return true;
            return Props&&Props.TryLift();
        }
        public float AttackElapsed => Attacking ? attackLength - attackTimer : 0;
        public float AttackRecoveryProgress
        {
            get
            {
                if (!Attacking) return 0;
                float elapsed = attackLength - attackTimer;
                return elapsed <= impactAt ? 0 : Mathf.Clamp01((elapsed - impactAt) / (attackLength - impactAt));
            }
        }
        public bool CanAct => Game.I.phase == Phase.Combat && !Game.I.Frozen;
        public bool CanMove => (Game.I.phase == Phase.Combat || Game.I.phase == Phase.Village) && !Game.I.Frozen;
        public void Initialize()
        {
            tattooEffects=gameObject.AddComponent<TattooCombatEffects>();
            grapple=gameObject.AddComponent<Grapple>();
            visual = NordicArt.Warrior(transform, false, false, out weapon);
            ResetPlayer();
        }
        public void ResetPlayer()
        {
            ClearBufferedInput();
            if(Props)Props.CancelCarry();
            if(tattooEffects)tattooEffects.ResetEffects();
            if(grapple)grapple.ResetState();
            hp = 110; facing = 1; velocity = Vector2.zero; attackTimer = dashTimer = parryTimer = invulnerability = dashCooldown = parryCooldown = 0;
            transform.position = new Vector3(-9, -.34f); impacted = false; facingTarget=null;dashDirection=1;echoRemaining=0;
            headbuttTimer = headbuttCooldown = 0; headbuttImpacted = false;
        }
        public bool FaceEnemy(bool keepTarget=false)
        {
            if(Holding)return true;
            if(!Game.I || Game.I.phase!=Phase.Combat)return false;
            Enemy nearest=null;float distance=float.MaxValue;
            foreach(var enemy in Game.I.enemies)
            {
                if(!enemy||!enemy.Alive)continue;
                float d=Mathf.Abs(enemy.transform.position.x-transform.position.x);
                if(d<distance){distance=d;nearest=enemy;}
            }
            if(!nearest){facingTarget=null;return false;}
            // Keep an action's target; otherwise use hysteresis to avoid rapid left/right flips.
            if(!facingTarget||!facingTarget.Alive||(!keepTarget&&distance+1f<Mathf.Abs(facingTarget.transform.position.x-transform.position.x)))facingTarget=nearest;
            float dx=facingTarget.transform.position.x-transform.position.x;
            if(Mathf.Abs(dx)>.08f)facing=dx>0?1:-1;
            return true;
        }
        public void Attack(bool strong)
        {
            if (Attacking || Dodging || Parrying || Headbutting || Holding || !CanAct) return;
            FaceEnemy();
            heavy = strong; attackLength = strong ? 1.05f : .49f;
            impactAt = strong ? .43f : .15f;
            attackLength = impactAt + (attackLength - impactAt) * Game.I.body.RecoveryMultiplier;
            attackTimer = attackLength; impacted = false;
            Game.I.PlaySound(strong ? CombatCue.HeavyWindup : CombatCue.LightWindup);
        }
        public void Dash(float horizontalInput = 0)
        {
            if (!CanAct || Attacking || Dodging || Headbutting || Holding || dashCooldown > 0) return;
            FaceEnemy();
            // Explicit movement input picks the dash direction; only an idle input falls back to current facing.
            int inputSign = horizontalInput > .001f ? 1 : horizontalInput < -.001f ? -1 : 0;
            dashDirection = inputSign != 0 ? inputSign : facing;
            dashTimer = .23f; dashCooldown = Game.I.body.legs.Stage == 3 ? 1.25f : .72f;
            dashHits.Clear(); parryTimer = 0;frostTriggered=false;
            Game.I.PlaySound(CombatCue.Dash);
        }
        public void Parry()
        {
            if (!CanAct || Attacking || Dodging || Headbutting || Holding || parryCooldown > 0) return;
            FaceEnemy();parryTimer = Game.I.body.ParryWindow; parryCooldown = .60f;
        }
        public bool Headbutt()
        {
            if (!CanAct || Attacking || Dodging || Parrying || Headbutting || Holding || headbuttCooldown > 0 || hp <= 4) return false;
            FaceEnemy();
            hp -= 4;
            headbuttDirection=facing;
            headbuttTimer = HeadbuttLength; headbuttCooldown = HeadbuttCooldownDuration*Game.I.body.HeadbuttCooldownMultiplier; headbuttImpacted = false;
            return true;
        }
        void Update()
        {
            if (!Game.I) return;
            if(Game.I.phase!=Phase.Combat || Game.I.paused || Game.I.choice.HasValue)ClearBufferedInput();
            float inputMove=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
            if(Game.I.phase==Phase.Combat && !Game.I.paused && !Game.I.choice.HasValue) {
                bool combatPointer=!Campaign.CampaignHUD.BlocksCombatPointer(Input.mousePosition);
                if((combatPointer&&Input.GetMouseButtonDown(0))||Input.GetKeyDown(KeyCode.J))QueueCombatInput(CombatInput.Light);
                if((combatPointer&&Input.GetMouseButtonDown(1))||Input.GetKeyDown(KeyCode.K))QueueCombatInput(CombatInput.Heavy);
                if(Input.GetKeyDown(KeyCode.C))QueueCombatInput(CombatInput.Parry);
                if(Input.GetKeyDown(KeyCode.LeftShift))QueueCombatInput(CombatInput.Dash,inputMove);
            }
            if(Game.I.Frozen)return;
            ConsumeBufferedInput();
            if(Game.I.phase!=Phase.Combat)headbuttTimer=0;
            // Village allows locomotion (walking between stations) but attacks/dash/parry stay combat-only
            // unless CampaignDirector has switched the phase to Combat for training.
            if (Game.I.phase != Phase.Combat && Game.I.phase != Phase.Village) { echoRemaining=0;headbuttTimer=0;Animate(); return; }
            float dt = Time.deltaTime;
            if(echoRemaining>0) {
                echoRemaining-=dt;
                if(echoRemaining<=0&&CanAct&&Game.I.body.Has(TattooPower.Echo)) {
                    TattooSignals.Emit(TattooPower.Echo);
                    Game.I.Burst(echoOrigin+Vector2.right*echoDirection*2,NordicArt.Gold,24,6);
                    float weapon=Game.I.sceneRole==Campaign.SceneRole.Legacy?1:Campaign.CampaignSession.WeaponMultiplier;
                    foreach(var e in Game.I.enemies.ToArray())if(e&&e.Alive&&!e.Captured) {
                        Vector2 delta=(Vector2)e.transform.position-echoOrigin;
                        if(delta.x*echoDirection>=-.25f&&delta.x*echoDirection<=3.5f&&Mathf.Abs(delta.y)<2)
                            e.TakeHit(18*weapon,echoDirection,.3f);
                    }
                }
            }
            invulnerability = Mathf.Max(0, invulnerability - dt); dashCooldown = Mathf.Max(0, dashCooldown - dt); parryCooldown = Mathf.Max(0, parryCooldown - dt); headbuttCooldown = Mathf.Max(0, headbuttCooldown - dt);
            if (CanMove)
            {
                float move = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
                bool hasTarget=FaceEnemy(Attacking||Dodging||Headbutting);
                if (!hasTarget && move != 0 && !Attacking && !Dodging && !Parrying && !Headbutting) facing = move > 0 ? 1 : -1;
                if (Input.GetKeyDown(KeyCode.Space) && Grounded && !Attacking && !Headbutting && !Holding) velocity.y = Game.I.body.JumpSpeed;
                if (CanAct)
                {
                    if (Input.GetKeyDown(KeyCode.Q)) Headbutt();
                    if (Input.GetKeyDown(KeyCode.V)) GrabOrThrow();
                }
                float speed = 5.6f * Game.I.body.MoveMultiplier;
                velocity.x = Dodging ? dashDirection * Game.I.body.DashSpeed : move * speed * (Holding?.55f:Attacking ? .14f : Parrying ? .35f : Headbutting ? .2f : 1);
                if (CanAct) Game.I.TrackHonor(move, dt);
            }
            else velocity.x = 0;
            velocity.y -= 24 * dt;
            transform.position += (Vector3)(velocity * dt);
            var pos = transform.position; pos.x = Mathf.Clamp(pos.x, -18.5f, 18.5f);
            if (pos.y <= -.34f) { pos.y = -.34f; velocity.y = 0; } transform.position = pos;
            if (Attacking)
            {
                attackTimer -= dt;
                if (!impacted && attackLength - attackTimer >= impactAt) { impacted = true; Impact(); }
            }
            if (Headbutting)
            {
                headbuttTimer -= dt;
                if (!headbuttImpacted && HeadbuttLength - headbuttTimer >= HeadbuttWindup) { headbuttImpacted = true; HeadbuttImpact(); }
            }
            if (Dodging)
            {
                dashTimer -= dt;
                if (Game.I.body.Has(TattooPower.Ram))
                    foreach (var e in Game.I.enemies.ToArray())
                        if (e && e.Alive && !e.Captured && Mathf.Abs(e.transform.position.x - pos.x) < 1.25f && Mathf.Abs(e.transform.position.y - pos.y) < 2 && dashHits.Add(e))
                        {
                            e.TakeHit(Game.I.body.legs.adaptation == Adaptation.Fury ? 32 : 16, dashDirection, .22f);
                            TattooSignals.Emit(TattooPower.Ram);
                        }
            }
            parryTimer = Mathf.Max(0, parryTimer - dt);
            Animate();
        }
        public int Impact()
        {
            if(Holding)return 0;
            FaceEnemy(true);
            var g = Game.I; int hits = 0;
            float reach = heavy ? 2.45f : 1.8f;
            // Equipped weapon quality only applies outside the legacy prototype scene, where the economy isn't wired up.
            float weaponMul = g.sceneRole == Campaign.SceneRole.Legacy ? 1f : Campaign.CampaignSession.WeaponMultiplier;
            Campaign.CampaignDirector.I?.AttackProps(transform.position, facing, reach, heavy);
            foreach (var enemy in g.enemies.ToArray())
            {
                if (!enemy || !enemy.Alive || enemy.Captured) continue;
                Vector2 diff = enemy.transform.position - transform.position;
                if (diff.x * facing < -.25f || diff.x * facing > reach || Mathf.Abs(diff.y) > 1.8f) continue;
                float counter=heavy?0:tattooEffects.ConsumeCounter();
                counter+=tattooEffects.ConsumeScarStrike(heavy,enemy);
                enemy.TakeHit((heavy ? g.body.HeavyDamage : 16) * weaponMul+counter, facing, heavy||counter>0 ? .6f : .13f); hits++;
            }
            if (heavy && g.body.Has(TattooPower.Thunder))
            {
                TattooSignals.Emit(TattooPower.Thunder);
                bool fusion = g.body.arm.adaptation == Adaptation.Fury;
                Vector2 strike = (Vector2)transform.position + Vector2.right * facing * 2.3f;
                g.Burst(strike, NordicArt.Ice, fusion ? 28 : 15, 5);
                foreach (var enemy in g.enemies.ToArray())
                    if (enemy && enemy.Alive && !enemy.Captured && Mathf.Abs(enemy.transform.position.x - strike.x) < (fusion ? 2.2f : 1.25f))
                        enemy.TakeHit((fusion ? 22 : 10) * weaponMul, facing, .18f);
            }
            if(heavy&&g.body.Has(TattooPower.Echo)) { echoRemaining=.25f;echoOrigin=transform.position;echoDirection=facing; }
            g.Burst((Vector2)transform.position + new Vector2(facing * 1.3f, .1f), heavy ? NordicArt.Gold : Color.white, heavy ? 14 : 6, 5);
            if (hits > 0) { g.HitStop(heavy ? .09f : .035f); g.shake = heavy ? .2f : .06f; g.PlaySound(heavy ? CombatCue.HeavyHit : CombatCue.LightHit); }
            else g.PlaySound(CombatCue.Miss);
            if (heavy && hits > 0) Campaign.CampaignDirector.I?.Action("heavy-hit");
            return hits;
        }
        void HeadbuttImpact()
        {
            if(!CanAct)return;
            var g = Game.I;
            Enemy target = null; float distance = float.MaxValue;
            foreach (var enemy in g.enemies)
            {
                if (!enemy || !enemy.Alive || enemy.Captured) continue;
                Vector2 diff = enemy.transform.position - transform.position;
                if (diff.x * headbuttDirection < 0 || diff.x * headbuttDirection > 1.8f || Mathf.Abs(diff.y) >= 1.6f) continue;
                float d = diff.x * headbuttDirection;
                if (d < distance) { distance = d; target = enemy; }
            }
            if (!target) return;
            float stun = target.boss ? .35f : target.elite ? .55f : .8f;
            target.TakeHit(g.body.HeadbuttDamage+tattooEffects.ConsumePain(), headbuttDirection, 0);
            if (target.Alive) target.Stun(stun);
            tattooEffects.HeadbuttHit();
            g.Burst((Vector2)transform.position + new Vector2(headbuttDirection * 1.3f, .1f), NordicArt.Ice, 10, 4);
            g.HitStop(.055f); g.PlaySound(CombatCue.Headbutt);
        }
        public void TakeHit(float damage, BodyPart part, Enemy source, bool ignoreDefense = false, DamageKind kind=DamageKind.Physical)
        {
            var g = Game.I;
            if(source&&source.boss)kind=DamageKind.Giant;
            if (g.phase != Phase.Combat || hp <= 0) return;
            bool training = Campaign.CampaignDirector.I && Campaign.CampaignDirector.I.training;
            if (!ignoreDefense)
            {
                if (invulnerability > 0) return;
                if(grapple.AbsorbHit(damage,source))return;
                if (Dodging)
                {
                    tattooEffects.Dodged(kind);
                    if(Campaign.CampaignSession.Run!=null&&g.sceneRole>=Campaign.SceneRole.Forest)Campaign.CampaignSession.Run.dodges++;
                    if(!frostTriggered&&g.body.Has(TattooPower.Frost)) {
                        frostTriggered=true;
                        TattooSignals.Emit(TattooPower.Frost);g.Burst(transform.position,NordicArt.Ice,26,5);
                        foreach(var e in g.enemies.ToArray())if(e&&e.Alive&&Vector2.Distance(e.transform.position,transform.position)<4)
                            e.TakeHit(8,facing,.8f);
                    }
                    g.GainHonor(4); g.Notice("전진 회피", "공격 속으로 파고들었다.", NordicArt.Ice);
                    Campaign.CampaignDirector.I?.Action("dodge"); return;
                }
                if (Parrying && source && (source.transform.position.x - transform.position.x) * facing >= 0)
                {
                    source.Stun(1.3f); parryTimer = 0; invulnerability = .25f;
                    g.GainHonor(8); g.parries++;
                    g.Burst(transform.position + Vector3.right * facing, NordicArt.Ice, 25, 7); g.HitStop(.08f); g.shake = .13f;
                    g.Notice("패링", "빈틈이다. 강공격으로 응수하라.", NordicArt.Ice); g.PlaySound(CombatCue.Parry);
                    if(g.body.Has(TattooPower.Reprisal)) {
                        TattooSignals.Emit(TattooPower.Reprisal);g.Burst(transform.position+Vector3.right*facing*3,NordicArt.Ice,22,6);
                        foreach(var e in g.enemies.ToArray())if(e&&e.Alive&&!e.Captured) {
                            Vector2 delta=e.transform.position-transform.position;
                            if(delta.x*facing>=0&&delta.x*facing<=6&&Mathf.Abs(delta.y)<2)e.TakeHit(20,facing,.3f);
                        }
                    }
                    tattooEffects.Parried(kind);Campaign.CampaignDirector.I?.Action("parry"); return;
                }
            }
            // Training hits must never kill: clamp the floor instead of letting a stray swing end the lesson.
            float appliedDamage=damage*g.body.IncomingMultiplier(part);
            hp = Mathf.Max(training ? 1 : 0, hp - appliedDamage); invulnerability = .58f;
            ClearBufferedInput();
            attackTimer = dashTimer = parryTimer = headbuttTimer = 0;
            if(grapple)grapple.Cancel();
            if(Props)Props.CancelCarry();
            g.damageTaken += damage; g.Burst(transform.position, NordicArt.Red, 16, 5); g.shake = .2f; g.PlaySound(CombatCue.Hurt);
            // Incidental training damage (sparring, not the scripted ignoreDefense hits) must not accumulate
            // trauma or trigger adaptation; the forced tutorial hits below still teach it.
            if (training && !ignoreDefense) return;
            var body = g.body.Get(part); int oldStage = body.Stage; bool wasAdapted = body.adaptation != Adaptation.None;
            // Equipped armor's trauma reduction only applies to real combat hits in campaign scenes; forced
            // ignoreDefense hits (debug tools, tutorial injury) bypass it so threshold teaching stays exact.
            float traumaMul = !ignoreDefense && g.sceneRole != Campaign.SceneRole.Legacy ? Campaign.CampaignSession.TraumaMultiplier(part) : 1f;
            body.AddTrauma(damage * traumaMul);
            if(!training&&Campaign.CampaignSession.Run!=null&&oldStage<3&&body.Stage==3)Campaign.CampaignSession.Run.severeKinds.Add(kind);
            if(hp>0)tattooEffects.StorePain(appliedDamage);
            // A fatal hit wins outright: no adaptation/injury notice follows the killing blow, and nothing pauses.
            if(hp<=0&&tattooEffects.TryRecall())return;
            if (hp <= 0) {
                if(!training&&Campaign.CampaignSession.Run!=null) {
                    Campaign.CampaignSession.Run.fatalPart=part;
                    Campaign.CampaignSession.Run.fatalKind=kind;
                    Campaign.CampaignSession.Run.fatalSource=source?source.archetype:"환경 / 함정";
                }
                g.EndRun(false); return;
            }
            bool justAdapted = !wasAdapted && body.adaptation != Adaptation.None;
            tattooEffects.Damaged();
            if (body.Stage == 3 && oldStage < 3)
                g.Notice(Game.PartName(part) + " · 심각한 부상", "해당 부위 문신 / 융합 효과 전체 비활성화. 페널티 강화.", NordicArt.Red, 5);
            else if (justAdapted)
                g.Notice(Game.PartName(part) + " · 분노 적응", "외상 30 돌파 · 힘을 얻었다. 같은 부위 피격 피해가 20% 증가한다.", NordicArt.Ice, 4);
        }
        void Animate()
        {
            if (!visual) return;
            float bob = Grounded && Mathf.Abs(velocity.x) > .1f ? Mathf.Sin(Time.time * 15) * .045f : Mathf.Sin(Time.time * 2.5f) * .016f;
            float lean = 0;
            if (Headbutting)
            {
                float elapsed = HeadbuttLength - headbuttTimer;
                lean = elapsed < HeadbuttWindup ? Mathf.Lerp(0, .4f, elapsed / HeadbuttWindup) : Mathf.Lerp(.4f, 0, Mathf.Clamp01((elapsed - HeadbuttWindup) / (HeadbuttLength - HeadbuttWindup)));
            }
            visual.localPosition = new Vector2(facing * lean, bob);
            visual.localScale = new Vector3(facing, 1, 1);
            float angle = -12;
            if (Attacking)
            {
                float elapsed = attackLength - attackTimer;
                angle = elapsed < impactAt ? Mathf.Lerp(-12, heavy ? 85 : 45, elapsed / impactAt) : Mathf.Lerp(-115, -12, Mathf.Clamp01((elapsed - impactAt) / (attackLength - impactAt)));
            }
            else if (Parrying) angle = 55;
            else if (Holding) angle = -95;
            else if (Dodging) angle = -75;
            else if (Headbutting) angle = -150;
            weapon.localRotation = Quaternion.Euler(0, 0, angle);
            foreach (var sr in visual.GetComponentsInChildren<SpriteRenderer>())
            {
                var c = sr.color; c.a = invulnerability > 0 && Mathf.FloorToInt(Time.time * 20) % 2 == 0 ? .4f : 1; sr.color = c;
                if (sr.name == "Tattoo") sr.color = Game.I.body.arm.TattoosEnabled ? NordicArt.Ice : NordicArt.Red;
            }
        }
    }

    public sealed class Enemy : MonoBehaviour
    {
        public float hp, maxHp, timer, stun;
        public bool boss, charger, elite;
        public bool Captured;
        public string archetype = "draugr";
        public int facing = -1, state, attackCount;
        public BodyPart target;
        public bool Alive => hp > 0;
        Transform visual, weapon;
        float speed, knockback;
        bool struck, eliteStyled;
        public float WindupDuration => elite?1.05f:boss?1f:charger?.95f:.8f;
        // Read-only estimate for the timing cue; charge travel matters as much as its windup.
        public float SecondsUntilThreat(Barbarian hero)
        {
            if(!Alive||Captured||stun>0||struck||(state!=1&&state!=2))return float.PositiveInfinity;
            float dx=(hero.transform.position.x-transform.position.x)*facing;
            if(dx<=-.65f||Mathf.Abs(hero.transform.position.y-transform.position.y)>=(boss?2.1f:1.6f))return float.PositiveInfinity;
            float travel=charger?Mathf.Max(0,(dx-1.4f)/13f):0;
            if(!charger&&dx>=(boss?3.2f:2.1f))return float.PositiveInfinity;
            if(charger&&travel> (state==1?.48f:timer))return float.PositiveInfinity;
            return (state==1?Mathf.Max(0,timer):0)+travel;
        }
        public void Initialize(bool isBoss, bool isCharger)
        {
            boss = isBoss; charger = isCharger; maxHp = hp = boss ? 320 : charger ? 66 : 82;
            speed = boss ? 1.7f : charger ? 3.3f : 2;
            visual = NordicArt.Warrior(transform, true, boss, out weapon);
            Game.I.enemies.Add(this); timer = .5f;
        }
        void Update()
        {
            var g = Game.I;
            if (!g || !Alive || Captured || g.phase != Phase.Combat || g.Frozen) return;
            float dt = Time.deltaTime;
            if (stun > 0) { stun -= dt; state = 0; timer = .45f; }
            else
            {
                var delta = g.player.transform.position.x - transform.position.x;
                if (state == 0)
                {
                    facing = delta >= 0 ? 1 : -1; timer -= dt;
                    float range = charger ? 5 : boss ? 3 : 1.8f;
                    if (Mathf.Abs(delta) > range) transform.position += Vector3.right * facing * speed * (elite ? .55f : 1f) * dt;
                    else if (timer <= 0)
                    {
                        state = 1; timer = WindupDuration; struck = false;
                        target = charger || (boss && attackCount % 2 == 1) ? BodyPart.Legs : BodyPart.RightArm;
                        if(g.sceneRole!=Campaign.SceneRole.Legacy && !(Campaign.CampaignDirector.I&&Campaign.CampaignDirector.I.training)) {
                            var pattern=boss?new[]{BodyPart.Head,BodyPart.Legs,BodyPart.Chest,BodyPart.LeftLeg}:charger?new[]{BodyPart.Legs,BodyPart.LeftLeg,BodyPart.Back}:elite?new[]{BodyPart.LeftArm,BodyPart.Chest,BodyPart.RightArm}:new[]{BodyPart.RightArm,BodyPart.LeftArm,BodyPart.Head,BodyPart.Back};
                            target=pattern[attackCount%pattern.Length];
                        }
                        attackCount++;
                    }
                }
                else if (state == 1)
                {
                    timer -= dt;
                    if (timer <= 0) { state = 2; timer = charger ? .48f : .2f; }
                }
                else if (state == 2)
                {
                    timer -= dt;
                    if (charger) transform.position += Vector3.right * facing * 13 * dt;
                    float dx = (g.player.transform.position.x - transform.position.x) * facing;
                    if (!struck && dx > -.65f && dx < (boss ? 3.2f : charger ? 1.5f : 2.1f) && Mathf.Abs(g.player.transform.position.y - transform.position.y) < (boss ? 2.1f : 1.6f))
                    {
                        struck = true;
                        g.player.TakeHit((boss ? 19 : charger ? 13 : 12) * (elite ? 1.5f : 1f), target, this);
                    }
                    if (timer <= 0) { state = 3; timer = boss ? 1.2f : .95f; }
                }
                else { timer -= dt; if (timer <= 0) state = 0; }
            }
            transform.position += Vector3.right * knockback * dt; knockback = Mathf.MoveTowards(knockback, 0, dt * 20);
            var p = transform.position; p.x = Mathf.Clamp(p.x, -18, 18); transform.position = p;
            if (elite && !eliteStyled)
            {
                eliteStyled = true;
                foreach (var sr in visual.GetComponentsInChildren<SpriteRenderer>()) if (sr.name == "Eye") sr.color = NordicArt.Gold;
            }
            float size = boss ? 1.65f : elite ? 1.3f : 1;
            visual.localScale = new Vector3(facing * size, size, 1);
            visual.localPosition = new Vector3(0, boss ? .67f : elite ? .32f : 0);
            weapon.localRotation = Quaternion.Euler(0, 0, stun > 0 ? -65 : state == 1 ? 80 : state == 2 ? -110 : -10);
        }
        public void Stun(float seconds) { stun = Mathf.Max(stun, seconds); state = 0; }
        public void TakeHit(float damage, int direction, float stagger)
        {
            if (!Alive || Captured) return;
            hp -= damage; knockback = direction * (boss ? 1.8f : elite ? 2.4f : 4);
            // Boss and elite sentinels resist light-hit stun; parries and heavy blows still make openings.
            if ((!boss && !elite) || stagger >= .5f || state == 3) Stun(stagger);
            Game.I.Burst(transform.position + Vector3.up * .4f, NordicArt.Gold, 6, 3);
            if (hp > 0) return;
            hp = 0; Game.I.kills++; Game.I.GainHonor(5);
            if(Campaign.CampaignSession.Run!=null&&Game.I.sceneRole>=Campaign.SceneRole.Forest){if(boss)Campaign.CampaignSession.Run.bossKills++;else if(elite)Campaign.CampaignSession.Run.eliteKills++;}
            if (Game.I.body.Has(TattooPower.Blood))
            {
                Game.I.player.hp = Mathf.Min(110, Game.I.player.hp + 7);
                TattooSignals.Emit(TattooPower.Blood);
            }
            Game.I.enemies.Remove(this); Game.I.Burst(transform.position, NordicArt.Red, boss ? 40 : 20, 6);
            Campaign.CampaignDirector.I?.EnemyDefeated(this);
            Destroy(gameObject);
        }
    }
}


