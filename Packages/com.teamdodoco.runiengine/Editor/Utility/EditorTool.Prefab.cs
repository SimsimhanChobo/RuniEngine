#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RuniEngine.Editor
{
    public partial class EditorTool
    {
        public static T? AddComponentCompatibleWithPrefab<T>(GameObject gameObject, bool backToTop = false) where T : Component
        {
            //오브젝트의 프리팹 타입을 가져옵니다
            PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(gameObject);
            if (prefabAssetType == PrefabAssetType.NotAPrefab) //프리팹이 아니라면 평소대로 컴포넌트를 추가합니다
            {
                T addedRectTransformTool = Undo.AddComponent<T>(gameObject);
                if (backToTop)
                    ComponentBackToTop(addedRectTransformTool);

                EditorUtility.SetDirty(addedRectTransformTool);
            }
            else if (prefabAssetType != PrefabAssetType.MissingAsset) //미싱되지 않은 프리팹이라면
            {
                //프리팹에서 오버라이딩으로 삭제된 컴포넌트를 가져옵니다
                List<RemovedComponent> removedComponents = PrefabUtility.GetRemovedComponents(gameObject);
                for (int j = 0; j < removedComponents.Count; j++)
                {
                    RemovedComponent removedComponent = removedComponents[j];
                    if (removedComponent.assetComponent.GetType() == typeof(T))
                    {
                        /*
                         * 만약 현제 추가할 컴포넌트가 오버라이딩으로 삭제된 컴포넌트랑 타입이 똑같다면
                         * 오버라이딩을 되돌린후, 후에 수행할 작업을 취소합니다
                         */

                        removedComponent.Revert();
                        return (T)removedComponent.assetComponent;
                    }
                }

                /*
                 * 프리팹의 오리지널 (에셋에 있는) 오브젝트를 가져오고
                 * 그 오리지널 프리팹에 컴포넌트를 추가합니다
                 */
                GameObject original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
                T addedRectTransformTool = Undo.AddComponent<T>(original);

                //수정 사항을 저장합니다
                EditorUtility.SetDirty(original);

                //컴포넌트를 맨 위로 올립니다
                if (backToTop)
                    ComponentBackToTop(addedRectTransformTool);

                return addedRectTransformTool;
            }

            return null;
        }

        public static void DestroyComponentCompatibleWithPrefab(Component component)
        {
            //오브젝트의 프리팹 타입을 가져옵니다
            PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(component);

            //오브젝트가 프리팹이 아니라면 평소대로 컴포넌트를 삭제합니다
            if (prefabAssetType == PrefabAssetType.NotAPrefab)
            {
                GameObject gameObject = component.gameObject;
                Undo.DestroyObjectImmediate(component);

                //수정 사항을 저장합니다
                EditorUtility.SetDirty(gameObject);
            }
            else if (prefabAssetType != PrefabAssetType.MissingAsset) //오브젝트가 미싱되지 않은 프리팹이라면 프리팹의 오리지널을 가져온후, 그 프리팹에서 컴포넌트를 삭제합니다
            {
                Component original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(component);
                GameObject gameObject = original.gameObject;

                Undo.DestroyObjectImmediate(original);

                //수정 사항을 저장합니다
                EditorUtility.SetDirty(gameObject);
            }
        }

        public static void ComponentBackToTop(Component component)
        {
            int length = component.GetComponents<Component>().Length;
            for (int j = 0; j < length - 2; j++)
            {
                UnityEditorInternal.ComponentUtility.MoveComponentUp(component);
                EditorUtility.SetDirty(component);
            }
        }
    }
}
