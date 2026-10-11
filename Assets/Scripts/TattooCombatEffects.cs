using UnityEngine;
using Valhalla.Campaign;

namespace Valhalla
{
    // Reactive tattoo effects own their transient state; BodyBuild.Has owns limb validity.
    public sealed class TattooCombatEffects : MonoBehaviour
    {
        Barbarian hero;
        float counterTime, breathCooldown, landingCooldown;
        float emberTime,giantTime,pain;
        bool airborne;
        Game G => Game.I;
        void Awake() { hero=GetComponent<Barbarian>(); }
        // Presentation reads pending effects without consuming charges or bypassing limb validity.
        public string StateHint(TattooPower power)
        {
            if(!G || !G.body.Has(power))return "문신 침묵";
            if(G.phase!=Phase.Combat)return TattooCatalog.TriggerHint(power);
            switch(power) {
                case TattooPower.Countermark: return counterTime>0?$"J 강화 대기 · {counterTime:0.0}초":"패링 → 다음 J 강화";
                case TattooPower.Ember: return emberTime>0?$"J 화염 대기 · {emberTime:0.0}초":"화염 회피 → J 강화";
                case TattooPower.GiantCounter: return giantTime>0?$"정예·보스 K · {giantTime:0.0}초":"거인 패링 → K 강화";
                case TattooPower.PainEcho: return pain>0?$"Q에 저장 피해 +{pain:0}":"피격 피해 → 다음 Q";
                case TattooPower.Recall: return CampaignSession.Run!=null&&CampaignSession.Run.recallUsed?"이번 출정 소진":G.honor>=70?"치명타 방어 준비":"명예 70 필요";
                case TattooPower.SecondWind: return breathCooldown>0?$"재사용 {breathCooldown:0.0}초":"생명 35 이하 피격";
                case TattooPower.Landing: return landingCooldown>0?$"재사용 {landingCooldown:0.0}초":"점프 후 착지 충격";
                default: return TattooCatalog.TriggerHint(power);
            }
        }
        public void ClearPending() { counterTime=emberTime=giantTime=pain=0;airborne=false; }
        public void ResetEffects() { ClearPending();breathCooldown=landingCooldown=0; }
        void Update()
        {
            if(!G||G.Frozen)return;
            if(G.phase!=Phase.Combat){ClearPending();return;}
            float dt=Time.deltaTime;counterTime=Mathf.Max(0,counterTime-dt);
            breathCooldown=Mathf.Max(0,breathCooldown-dt);landingCooldown=Mathf.Max(0,landingCooldown-dt);
            emberTime=Mathf.Max(0,emberTime-dt);giantTime=Mathf.Max(0,giantTime-dt);
            if(!G.body.Has(TattooPower.Ember))emberTime=0;
            if(!G.body.Has(TattooPower.GiantCounter))giantTime=0;
            if(!G.body.Has(TattooPower.PainEcho))pain=0;
            if(!G.body.Has(TattooPower.Countermark))counterTime=0;
            if(hero.transform.position.y>.5f)airborne=true;
            if(airborne&&hero.Grounded){airborne=false;Land();}
        }
        public void Parried(DamageKind kind=DamageKind.Physical)
        {
            if(G.body.Has(TattooPower.Countermark)){counterTime=3;TattooSignals.Emit(TattooPower.Countermark);}
            if(kind==DamageKind.Giant&&G.body.Has(TattooPower.GiantCounter)){giantTime=4;TattooSignals.Emit(TattooPower.GiantCounter);}
            if(hero.hp<=35&&G.honor>=5&&G.body.Has(TattooPower.Valor)){hero.hp=Mathf.Min(110,hero.hp+8);G.honor-=5;TattooSignals.Emit(TattooPower.Valor);}
        }
        public void Dodged(DamageKind kind) { if(kind==DamageKind.Fire&&G.body.Has(TattooPower.Ember)){emberTime=3;TattooSignals.Emit(TattooPower.Ember);} }
        public float ConsumeScarStrike(bool heavy,Enemy target)
        {
            if(!heavy&&emberTime>0&&G.body.Has(TattooPower.Ember)){emberTime=0;TattooSignals.Emit(TattooPower.Ember);return 18;}
            if(heavy&&(target.boss||target.elite)&&giantTime>0&&G.body.Has(TattooPower.GiantCounter)){giantTime=0;TattooSignals.Emit(TattooPower.GiantCounter);return 25;}
            return 0;
        }
        public void StorePain(float amount) { if(G.body.Has(TattooPower.PainEcho))pain=Mathf.Min(20,pain+Mathf.Max(0,amount)); }
        public float ConsumePain() { float stored=G.body.Has(TattooPower.PainEcho)?pain:0;pain=0;if(stored>0)TattooSignals.Emit(TattooPower.PainEcho);return stored; }
        public float ConsumeCounter()
        {
            if(counterTime<=0||!G.body.Has(TattooPower.Countermark)){counterTime=0;return 0;}
            counterTime=0;TattooSignals.Emit(TattooPower.Countermark);
            return G.body.Get(BodyPart.LeftArm).adaptation==Adaptation.Fury?22:14;
        }
        public void HeadbuttHit()
        {
            if(!G.body.Has(TattooPower.Roar))return;
            float radius=G.body.Get(BodyPart.Head).adaptation==Adaptation.Fury?4:3;
            foreach(var e in G.enemies.ToArray())if(e&&e.Alive&&!e.Captured&&Vector2.Distance(hero.transform.position,e.transform.position)<=radius)e.Stun(.5f);
            G.Burst(hero.transform.position,NordicArt.Gold,24,5);TattooSignals.Emit(TattooPower.Roar);
        }
        public void Damaged()
        {
            if(hero.hp<=0||hero.hp>35||breathCooldown>0||!G.body.Has(TattooPower.SecondWind))return;
            breathCooldown=6;float damage=G.body.Get(BodyPart.Chest).adaptation==Adaptation.Fury?10:6;
            foreach(var e in G.enemies.ToArray())if(e&&e.Alive&&!e.Captured&&Vector2.Distance(hero.transform.position,e.transform.position)<=3)e.TakeHit(damage,e.transform.position.x>=hero.transform.position.x?1:-1,.6f);
            G.Burst(hero.transform.position,NordicArt.Ice,25,6);TattooSignals.Emit(TattooPower.SecondWind);
        }
        public bool TryRecall()
        {
            var run=CampaignSession.Run;
            if(run==null||run.recallUsed||G.honor<70||!G.body.Has(TattooPower.Recall))return false;
            run.recallUsed=true;G.honor-=30;hero.hp=G.body.Get(BodyPart.Back).adaptation==Adaptation.Fury?20:10;hero.invulnerability=.8f;
            foreach(var e in G.enemies.ToArray())if(e&&e.Alive&&!e.Captured&&Vector2.Distance(hero.transform.position,e.transform.position)<=3)e.Stun(.8f);
            G.Notice("발키리의 맹세", "명예 30을 바쳐 죽음을 한 번 피했다. 이번 출정에는 다시 발동하지 않는다.",NordicArt.Gold,4);
            TattooSignals.Emit(TattooPower.Recall);return true;
        }
        void Land()
        {
            if(landingCooldown>0||!G.body.Has(TattooPower.Landing))return;
            landingCooldown=2;float damage=G.body.Get(BodyPart.LeftLeg).adaptation==Adaptation.Fury?14:8;
            foreach(var e in G.enemies.ToArray())if(e&&e.Alive&&!e.Captured&&Vector2.Distance(hero.transform.position,e.transform.position)<=2.5f)e.TakeHit(damage,e.transform.position.x>=hero.transform.position.x?1:-1,.6f);
            G.Burst(hero.transform.position,NordicArt.Gold,20,5);TattooSignals.Emit(TattooPower.Landing);
        }
    }
}

