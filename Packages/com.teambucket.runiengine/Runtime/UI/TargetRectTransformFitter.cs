#nullable enable
using UnityEngine;

namespace RuniEngine.UI
{
    [ExecuteAlways]
    public class TargetRectTransformFitter : UIBase
    {
        public RectTransform? targetRectTransform { get => _rectTargetTransform; set => _rectTargetTransform = value; }
        [SerializeField, NotNullField] RectTransform? _rectTargetTransform;

        DrivenRectTransformTracker tracker;

        protected override void OnEnable() => Canvas.preWillRenderCanvases += Refresh;

        protected override void OnDisable()
        {
            Canvas.preWillRenderCanvases -= Refresh;

            if (!Kernel.isPlaying)
                tracker.Clear();
        }

        readonly Vector3[] fourCornersArray = new Vector3[4];
        protected virtual void Refresh()
        {
            if (targetRectTransform == null)
                return;

            if (!Kernel.isPlaying)
            {
                tracker.Clear();
                tracker.Add(this, rectTransform, DrivenTransformProperties.All);
            }

            targetRectTransform.GetWorldCorners(fourCornersArray);
            Matrix4x4 matrix4x = rectTransform.worldToLocalMatrix;
            for (int i = 0; i < 4; i++)
            {
                fourCornersArray[i] = matrix4x.MultiplyPoint(fourCornersArray[i]);

                if (i == 0)
                    rectTransform.offsetMin = fourCornersArray[i];
                else if (i == 2)
                    rectTransform.offsetMax = fourCornersArray[i];
            }

            rectTransform.position = targetRectTransform.position;
            rectTransform.rotation = targetRectTransform.rotation;
            rectTransform.localScale = targetRectTransform.localScale;
        }
    }
}
