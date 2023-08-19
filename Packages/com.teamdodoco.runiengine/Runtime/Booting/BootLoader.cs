#nullable enable
using Cysharp.Threading.Tasks;
using RuniEngine.Data;
using RuniEngine.Splash;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.LowLevel;

namespace RuniEngine.Booting
{
    public static class BootLoader
    {
        public static StorableClass[] projectData
        {
            get
            {
                if (!basicDataLoaded)
                    throw new BasicDataNotLoadedException();

                return _projectData;
            }
        }
        internal static StorableClass[] _projectData = null!;

        public static StorableClass[] userData
        {
            get
            {
                if (!basicDataLoaded)
                    throw new BasicDataNotLoadedException();

                return _userData;
            }
        }
        internal static StorableClass[] _userData = null!;

        public static bool basicDataLoaded { get; set; } = false;
        public static bool allLoaded { get; set; } = false;

        public static bool splashAniPlaying { get; set; } = false;

        [RuntimeInitializeOnLoadMethod]
        public static async void Booting()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            //CS0162 접근할 수 없는 코드 경고를 비활성화 하기 위해 변수로 우회합니다
            bool warningDisable = true;
            if (warningDisable)
                throw new NotSupportedException("SC KRM은 WebGL을 지원하지 않습니다\nSC KRM does not support WebGL");
#endif

            //Path Init
            Kernel.PathInitialize();

            //Player Loop Setting
            Debug.Log("Player Loop Setting...", nameof(BootLoader));
            {
                PlayerLoopSystem loopSystems = PlayerLoop.GetCurrentPlayerLoop();

                //UniTask Setting
                PlayerLoopHelper.Initialize(ref loopSystems);

                //Awaken Invoke
                AttributeInvoke<AwakenAttribute>();

                //Custom Update Setting
                CustomPlayerLoopSetter.EventRegister(ref loopSystems);
                PlayerLoop.SetPlayerLoop(loopSystems);
            }

            //Storable Class
            Debug.Log("Storable Class Loading...", nameof(BootLoader));
            {
                await UniTask.WhenAll(
                    UniTask.RunOnThreadPool(() => _projectData = StorableClassUtility.AutoInitialize<ProjectDataAttribute>()),
                    UniTask.RunOnThreadPool(() => _userData = StorableClassUtility.AutoInitialize<UserDataAttribute>())
                    );

                await UniTask.WhenAll(
                    UniTask.RunOnThreadPool(() => StorableClassUtility.LoadAll(_projectData, Kernel.projectDataPath)),
                    UniTask.RunOnThreadPool(() => StorableClassUtility.LoadAll(_userData, Kernel.userDataPath))
                    );

                basicDataLoaded = true;
            }

            await UniTask.Delay(100);
            SplashScreen.isPlaying = true;

            allLoaded = true;
        }

        static void AttributeInvoke<T>() where T : Attribute
        {
            Type[] types = ReflectionManager.types;
            for (int typesIndex = 0; typesIndex < types.Length; typesIndex++)
            {
                MethodInfo[] methodInfos = types[typesIndex].GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                for (int methodInfoIndex = 0; methodInfoIndex < methodInfos.Length; methodInfoIndex++)
                {
                    MethodInfo methodInfo = methodInfos[methodInfoIndex];
                    if (Attribute.GetCustomAttributes(methodInfo, typeof(T)).Length > 0 && methodInfo.GetParameters().Length <= 0)
                    {
                        try
                        {
                            methodInfo.Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }
    }
}
