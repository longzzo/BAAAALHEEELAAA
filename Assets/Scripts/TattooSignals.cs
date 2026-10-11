using System;
using UnityEngine;

namespace Valhalla
{
    // Combat publishes ability activation; presentation can listen without owning combat rules.
    public static class TattooSignals
    {
        public static event Action<TattooPower> Activated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetListeners() => Activated = null;

        public static void Emit(TattooPower power) => Activated?.Invoke(power);
    }
}
