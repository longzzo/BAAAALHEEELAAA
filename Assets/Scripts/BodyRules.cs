using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valhalla
{
    // Legacy name kept for save/enum compatibility: BodyPart.Legs is the right leg.
    public enum BodyPart { RightArm = 0, Legs = 1, Head = 2, Chest = 3, Back = 4, LeftArm = 5, LeftLeg = 6 }
    public enum Adaptation { None, Fury, Endure }
    public enum TattooPower { Thunder = 0, Blood = 1, Ram = 2, Reprisal = 3, Echo = 4, Frost = 5, Roar = 6, SecondWind = 7, Recall = 8, Countermark = 9, Landing = 10, Ember = 11, GiantCounter = 12, Valor = 13, PainEcho = 14 }

    [CreateAssetMenu(menuName = "Valhalla/Tattoo")]
    public sealed class TattooDefinition : ScriptableObject
    {
        public string title;
        public BodyPart bodyPart;
        public TattooPower power;
    }

    [Serializable]
    public sealed class BodyState
    {
        public BodyPart part;
        public float trauma;
        public Adaptation adaptation;
        public bool choiceResolved;
        public int Stage => trauma >= 55 ? 3 : trauma >= 30 ? 2 : trauma >= 14 ? 1 : 0;
        public bool TattoosEnabled => Stage < 3;
        // Retained for compile compatibility with existing HUD references; gameplay never leaves this true.
        public bool NeedsChoice => Stage >= 2 && !choiceResolved;
        public void Reset() { trauma = 0; adaptation = Adaptation.None; choiceResolved = false; }
        // Crossing into stage 2 deterministically grants Fury once per limb. No popup, no pause, no player choice.
        public void AddTrauma(float amount)
        {
            trauma = Mathf.Max(0, trauma + amount);
            if (Stage >= 2 && adaptation == Adaptation.None) { adaptation = Adaptation.Fury; choiceResolved = true; }
        }
    }

    // One gate for EVERY tattoo effect. Severe injury cannot leave a passive or fusion active.
    public sealed class BodyBuild
    {
        public readonly BodyState arm = new BodyState { part = BodyPart.RightArm };
        public readonly BodyState legs = new BodyState { part = BodyPart.Legs };
        public readonly BodyState head = new BodyState { part = BodyPart.Head };
        public readonly BodyState chest = new BodyState { part = BodyPart.Chest };
        public readonly BodyState back = new BodyState { part = BodyPart.Back };
        public readonly BodyState leftArm = new BodyState { part = BodyPart.LeftArm };
        public readonly BodyState leftLeg = new BodyState { part = BodyPart.LeftLeg };
        public readonly List<TattooDefinition> tattoos = new List<TattooDefinition>();

        public static readonly BodyPart[] Parts =
        {
            BodyPart.RightArm, BodyPart.Legs, BodyPart.Head, BodyPart.Chest, BodyPart.Back, BodyPart.LeftArm, BodyPart.LeftLeg
        };

        public IEnumerable<BodyState> States
        {
            get
            {
                yield return arm;
                yield return legs;
                yield return head;
                yield return chest;
                yield return back;
                yield return leftArm;
                yield return leftLeg;
            }
        }

        public BodyState Get(BodyPart part)
        {
            switch (part)
            {
                case BodyPart.RightArm: return arm;
                case BodyPart.Legs: return legs;
                case BodyPart.Head: return head;
                case BodyPart.Chest: return chest;
                case BodyPart.Back: return back;
                case BodyPart.LeftArm: return leftArm;
                case BodyPart.LeftLeg: return leftLeg;
                default: throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown BodyPart.");
            }
        }

        public bool Has(TattooPower power)
        {
            foreach (var tattoo in tattoos)
                if (tattoo.power == power && Get(tattoo.bodyPart).TattoosEnabled) return true;
            return false;
        }

        public float HeavyDamage => 35 * (arm.adaptation == Adaptation.Fury ? 1.6f : 1f) * (arm.Stage == 3 ? .7f : 1f);
        public float RecoveryMultiplier => arm.Stage == 3 ? 1.9f : arm.Stage == 2 ? (arm.adaptation == Adaptation.Endure ? 1.1f : 1.4f) : 1f;

        public float MoveMultiplier
        {
            get
            {
                float rightLegFactor = legs.Stage == 3 ? .48f : legs.Stage == 2 ? (legs.adaptation == Adaptation.Endure ? .9f : .7f) : 1f;
                float leftLegFactor = leftLeg.Stage == 3 ? .75f : leftLeg.Stage == 2 ? .9f : 1f;
                return rightLegFactor * leftLegFactor;
            }
        }

        public float DashSpeed => legs.Stage == 3 ? 10 : legs.adaptation == Adaptation.Fury ? 23 : 18;

        public float IncomingMultiplier(BodyPart part)
        {
            float multiplier = Get(part).adaptation == Adaptation.Fury ? 1.2f : 1f;
            if (part == BodyPart.Chest) multiplier *= chest.Stage == 3 ? 1.25f : chest.Stage == 2 ? 1.1f : 1f;
            return multiplier;
        }

        public float HeadbuttDamage => head.Stage == 3 ? 6 : head.Stage >= 2 ? 12 : 8;
        public float HeadbuttCooldownMultiplier => head.Stage == 3 ? 2f : head.Stage == 2 ? 1.5f : 1f;
        public float ParryWindow => leftArm.Stage == 3 ? .25f : leftArm.Stage == 2 ? .36f : .45f;
        public float JumpSpeed => leftLeg.Stage == 3 ? 5.3f : leftLeg.Stage == 2 ? 7.5f : 8.8f;
        public float HonorGainMultiplier => back.Stage == 3 ? .5f : back.Stage == 2 ? .8f : 1f;

        public void Reset()
        {
            arm.Reset();
            legs.Reset();
            head.Reset();
            chest.Reset();
            back.Reset();
            leftArm.Reset();
            leftLeg.Reset();
        }
    }
}
