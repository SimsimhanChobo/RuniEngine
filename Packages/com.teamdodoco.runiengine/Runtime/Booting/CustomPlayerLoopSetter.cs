#nullable enable
using System;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace RuniEngine.Booting
{
    public static class CustomPlayerLoopSetter
    {
        public static event PlayerLoopSystem.UpdateFunction? initEvent;
        public static event PlayerLoopSystem.UpdateFunction? earlyUpdateEvent;
        public static event PlayerLoopSystem.UpdateFunction? fixedUpdateEvent;
        public static event PlayerLoopSystem.UpdateFunction? preUpdateEvent;
        public static event PlayerLoopSystem.UpdateFunction? updateEvent;
        public static event PlayerLoopSystem.UpdateFunction? preLateUpdateEvent;
        public static event PlayerLoopSystem.UpdateFunction? postLateUpdateEvent;
#if UNITY_2020_2_OR_NEWER
        public static event PlayerLoopSystem.UpdateFunction? timeUpdateEvent;
#endif

        [Awaken]
        static void Awaken() => Application.quitting += () => PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());

        public static void EventRegister(ref PlayerLoopSystem loopSystems)
        {
            for (int i = 0; i < loopSystems.subSystemList.Length; i++)
            {
                PlayerLoopSystem loopSystem = loopSystems.subSystemList[i];
                PlayerLoopSystem.UpdateFunction? updateDelegate = null;
                Type type = loopSystem.type;

                if (type == typeof(Initialization))
                    updateDelegate += initEvent;
                else if (type == typeof(EarlyUpdate))
                    updateDelegate += earlyUpdateEvent;
                else if (type == typeof(FixedUpdate))
                    updateDelegate += fixedUpdateEvent;
                else if (type == typeof(PreUpdate))
                    updateDelegate += preUpdateEvent;
                else if (type == typeof(Update))
                    updateDelegate += updateEvent;
                else if (type == typeof(PreLateUpdate))
                    updateDelegate += preLateUpdateEvent;
                else if (type == typeof(PostLateUpdate))
                    updateDelegate += postLateUpdateEvent;
#if UNITY_2020_2_OR_NEWER
                else if (type == typeof(TimeUpdate))
                    updateDelegate += timeUpdateEvent;
#endif

                loopSystem.updateDelegate += updateDelegate;
                loopSystems.subSystemList[i] = loopSystem;
            }
        }
    }
}
