#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniEngine.Datas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using Object = UnityEngine.Object;

namespace RuniEngine.Resource.Objects
{
    public sealed class ObjectLoader : IResourceElement
    {
        [UserData]
        public struct UserData
        {
            [JsonProperty] public static bool allowOtherResourcePackLoad { get; set; } = false;
        }

        public bool isLoaded { get; private set; } = false;
        public ResourcePack? resourcePack { get; set; }

        /// <summary>
        /// Object = unityObjects[nameSpace][key];
        /// </summary>
        Dictionary<string, Dictionary<string, Object?>> unityObjects = new();

        /// <summary>
        /// GameObject = gameObjects[nameSpace][key];
        /// </summary>
        Dictionary<string, Dictionary<string, GameObject?>> gameObjects = new();

        public string name { get; } = "objects";



        public static Object? SearchObject(string name, string nameSpace = "")
        {
            ResourceManager.SetDefaultNameSpace(ref nameSpace);

            Object? result = null;
            ResourceManager.ResourceElementLoop<ObjectLoader>(x =>
            {
                if (x.unityObjects.TryGetValue(nameSpace, out var value) && value.TryGetValue(name, out Object? value2))
                {
                    result = value2;
                    return true;
                }

                return false;
            });

            return result;
        }



        public static GameObject? SearchGameObject(string name, string nameSpace = "")
        {
            ResourceManager.SetDefaultNameSpace(ref nameSpace);
            
            GameObject? result = null;
            ResourceManager.ResourceElementLoop<ObjectLoader>(x =>
            {
                if (x.gameObjects.TryGetValue(nameSpace, out var value) && value.TryGetValue(name, out GameObject? value2))
                {
                    result = value2;
                    return true;
                }

                return false;
            });

            return result;
        }



        public UniTask Load() => Load(null);
        public async UniTask Load(IProgress<float>? progress)
        {
            /* 
             * 기본 리소스팩이 아닌 다른 리소스팩에서 불러와도 (이론상) 지장이 전혀 없습니다
             * 다만 오브젝트를 통째로 불러오는건 굉장히 위험한 방법이기에 기본적으로 내장 리소스팩에서만 불러오게 설정했습니다
             */
            if (resourcePack == null || (!UserData.allowOtherResourcePackLoad && resourcePack != ResourcePack.defaultPack))
                return;

            await UniTask.SwitchToThreadPool();

            foreach (Object? unityObject in unityObjects.SelectMany(x => x.Value).Select(x => x.Value).Where(unityObject => unityObject != null))
                ResourceManager.garbages.Add(unityObject);

            await UniTask.SwitchToMainThread();

            Dictionary<string, Dictionary<string, Object?>> tempUnityObjects = new();
            Dictionary<string, Dictionary<string, GameObject?>> tempGameObjects = new();

            for (int i = 0; i < resourcePack.nameSpaces.Count; i++)
            {
                string nameSpace = resourcePack.nameSpaces[i];
                string path = Path.Combine(resourcePack.path, ResourceManager.rootName, nameSpace, name);
                
                if (!File.Exists(path))
                {
                    Report(1);
                    continue;
                }

                AssetBundle assetBundle = await AssetBundle.LoadFromFileAsync(path).ToUniTask(Progress.Create<float>(x => Report(x * 0.5f)));
                Object[] unityObjects = await assetBundle.LoadAllAssetsAsync().AwaitForAllAssets(Progress.Create<float>(x => Report((x * 0.5f) + 0.5f)));

                Report(1);

                void Report(float value)
                {
                    if (resourcePack != null)
                        progress?.Report((float)(i + value) / resourcePack.nameSpaces.Count);
                }

                for (int j = 0; j < unityObjects.Length; j++)
                {
                    Object unityObject = unityObjects[j];

                    tempUnityObjects.TryAdd(nameSpace, new());
                    tempUnityObjects[nameSpace].TryAdd(unityObject.name, unityObject);

                    if (unityObject is GameObject gameObject)
                    {
                        tempGameObjects.TryAdd(nameSpace, new());
                        tempGameObjects[nameSpace].TryAdd(gameObject.name, gameObject);
                    }
                }
            }

            unityObjects = tempUnityObjects;
            gameObjects = tempGameObjects;
        }

        public async UniTask Unload()
        {
            foreach (Object? unityObject in unityObjects.SelectMany(x => x.Value).Select(x => x.Value).Where(unityObject => unityObject != null))
            {
                Object.DestroyImmediate(unityObject);
                await UniTask.Yield();
            }

            unityObjects = new();
            gameObjects = new();
        }
    }
}
