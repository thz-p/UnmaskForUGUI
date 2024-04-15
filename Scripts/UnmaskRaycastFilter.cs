using UnityEngine;

namespace Coffee.UIExtensions
{
    /// <summary>
    /// Unmask Raycast Filter.
    /// The ray passes through the unmasked rectangle.
    /// </summary>
    [AddComponentMenu("UI/Unmask/UnmaskRaycastFilter", 2)]
    public class UnmaskRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        //################################
        // Serialize Members.
        //################################
        [Tooltip("Target unmask component. The ray passes through the unmasked rectangle.")]
        [SerializeField] private Unmask m_TargetUnmask;


        //################################
        // Public Members.
        //################################
        /// <summary>
        /// Target unmask component. Ray through the unmasked rectangle.
        /// </summary>
        public Unmask targetUnmask { get { return m_TargetUnmask; } set { m_TargetUnmask = value; } }

        /// <summary>
        /// 给定一个点和一个相机，判断射线投射是否有效。
        /// </summary>
        /// <returns>是否有效。</returns>
        /// <param name="sp">屏幕位置。</param>
        /// <param name="eventCamera">射线投射相机。</param>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            // 如果当前对象或目标解除遮罩未启用，则视为有效
            if (!isActiveAndEnabled || !m_TargetUnmask || !m_TargetUnmask.isActiveAndEnabled)
            {
                return true;
            }

            // 检查是否在解除遮罩区域内
            if (eventCamera)
            {
                return !RectTransformUtility.RectangleContainsScreenPoint((m_TargetUnmask.transform as RectTransform), sp, eventCamera);
            }
            else
            {
                return !RectTransformUtility.RectangleContainsScreenPoint((m_TargetUnmask.transform as RectTransform), sp);
            }
        }

        //################################
        // Private Members.
        //################################

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        void OnEnable()
        {
        }
    }
}