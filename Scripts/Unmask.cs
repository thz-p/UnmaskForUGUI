﻿using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.UI;


namespace Coffee.UIExtensions
{
    /// <summary>
    /// Reverse masking for parent Mask component.
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("UI/Unmask/Unmask", 1)]
    public class Unmask : MonoBehaviour, IMaterialModifier
    {
        //################################
        // Constant or Static Members.
        //################################
        private static readonly Vector2 s_Center = new Vector2(0.5f, 0.5f);


        //################################
        // Serialize Members.
        //################################
        [Tooltip("Fit graphic's transform to target transform.")]
        [SerializeField] private RectTransform m_FitTarget;

        [Tooltip("Fit graphic's transform to target transform on LateUpdate every frame.")]
        [SerializeField] private bool m_FitOnLateUpdate;

        [Tooltip("Unmask affects only for children.")]
        [SerializeField] private bool m_OnlyForChildren = false;

        [Tooltip("Show the graphic that is associated with the unmask render area.")]
        [SerializeField] private bool m_ShowUnmaskGraphic = false;

        [Tooltip("Edge smoothing.")]
        [Range(0f, 1f)]
        [SerializeField] private float m_EdgeSmoothing = 0f;



        //################################
        // Public Members.
        //################################
        /// <summary>
        /// The graphic associated with the unmask.
        /// </summary>
        public MaskableGraphic graphic { get { return _graphic ?? (_graphic = GetComponent<MaskableGraphic>()); } }

        /// <summary>
        /// Fit graphic's transform to target transform.
        /// </summary>
        public RectTransform fitTarget
        {
            get { return m_FitTarget; }
            set
            {
                m_FitTarget = value;
                FitTo(m_FitTarget);
            }
        }

        /// <summary>
        /// Fit graphic's transform to target transform on LateUpdate every frame.
        /// </summary>
        public bool fitOnLateUpdate { get { return m_FitOnLateUpdate; } set { m_FitOnLateUpdate = value; } }

        /// <summary>
        /// Show the graphic that is associated with the unmask render area.
        /// </summary>
        public bool showUnmaskGraphic
        {
            get { return m_ShowUnmaskGraphic; }
            set
            {
                m_ShowUnmaskGraphic = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Unmask affects only for children.
        /// </summary>
        public bool onlyForChildren
        {
            get { return m_OnlyForChildren; }
            set
            {
                m_OnlyForChildren = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Edge smooting.
        /// </summary>
        public float edgeSmoothing
        {
            get { return m_EdgeSmoothing; }
            set { m_EdgeSmoothing = value; }
        }

        /// <summary>
        /// Perform material modification in this function.
        /// </summary>
        /// <returns>Modified material.</returns>
        /// <param name="baseMaterial">Configured Material.</param>
        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled)
            {
                return baseMaterial;
            }

            Transform stopAfter = MaskUtilities.FindRootSortOverrideCanvas(transform);
            var stencilDepth = MaskUtilities.GetStencilDepth(transform, stopAfter);
            var desiredStencilBit = 1 << stencilDepth;

            StencilMaterial.Remove(_unmaskMaterial);
            _unmaskMaterial = StencilMaterial.Add(baseMaterial, desiredStencilBit - 1, StencilOp.Invert, CompareFunction.Equal, m_ShowUnmaskGraphic ? ColorWriteMask.All : (ColorWriteMask)0, desiredStencilBit - 1, (1 << 8) - 1);

            // Unmask affects only for children.
            var canvasRenderer = graphic.canvasRenderer;
            if (m_OnlyForChildren)
            {
                StencilMaterial.Remove(_revertUnmaskMaterial);
                _revertUnmaskMaterial = StencilMaterial.Add(baseMaterial, (1 << 7), StencilOp.Invert, CompareFunction.Equal, (ColorWriteMask)0, (1 << 7), (1 << 8) - 1);
                canvasRenderer.hasPopInstruction = true;
                canvasRenderer.popMaterialCount = 1;
                canvasRenderer.SetPopMaterial(_revertUnmaskMaterial, 0);
            }
            else
            {
                canvasRenderer.hasPopInstruction = false;
                canvasRenderer.popMaterialCount = 0;
            }

            return _unmaskMaterial;
        }

        /// <summary>
        /// 将当前 RectTransform 适配到目标 RectTransform。
        /// </summary>
        /// <param name="target">目标 RectTransform。</param>
        public void FitTo(RectTransform target)
        {
            // 获取当前对象的 RectTransform
            var rt = transform as RectTransform;

            // 设置当前对象的中心点、位置和旋转为目标对象的中心点、位置和旋转
            rt.pivot = target.pivot;
            rt.position = target.position;
            rt.rotation = target.rotation;

            // 计算目标对象和父对象的缩放比例
            var s1 = target.lossyScale;
            var s2 = rt.parent.lossyScale;

            // 根据缩放比例调整当前对象的缩放
            rt.localScale = new Vector3(s1.x / s2.x, s1.y / s2.y, s1.z / s2.z);

            // 设置当前对象的大小为目标对象的大小
            rt.sizeDelta = target.rect.size;

            // 设置当前对象的锚点为中心点
            rt.anchorMax = rt.anchorMin = s_Center;
        }

        //################################
        // 私有成员。
        //################################
        private Material _unmaskMaterial;       // 用于保存解除遮罩后的材质
        private Material _revertUnmaskMaterial; // 用于保存重新应用遮罩前的材质
        private MaskableGraphic _graphic;       // 保存图形对象的引用

        /// <summary>
        /// 当对象变为启用和活动状态时调用此函数。
        /// </summary>
        private void OnEnable()
        {
            // 如果存在适配目标，则将图形适配到目标
            if (m_FitTarget)
            {
                FitTo(m_FitTarget);
            }
            // 标记图形为"脏"
            SetDirty();
        }

        /// <summary>
        /// 当行为被禁用（）或非活动状态时调用此函数。
        /// </summary>
        private void OnDisable()
        {
            // 移除解除遮罩后的材质和重新应用遮罩前的材质
            StencilMaterial.Remove(_unmaskMaterial);
            StencilMaterial.Remove(_revertUnmaskMaterial);
            // 重置为null，释放资源
            _unmaskMaterial = null;
            _revertUnmaskMaterial = null;

            // 如果图形对象存在
            if (graphic)
            {
                var canvasRenderer = graphic.canvasRenderer;
                // 设置画布渲染器的属性
                canvasRenderer.hasPopInstruction = false;
                canvasRenderer.popMaterialCount = 0;
                // 标记图形为"脏"
                graphic.SetMaterialDirty();
            }
            // 标记图形为"脏"
            SetDirty();
        }

        /// <summary>
        /// LateUpdate is called every frame, if the Behaviour is enabled.
        /// </summary>
        private void LateUpdate()
        {
            #if UNITY_EDITOR
                // 如果在Unity编辑器中，并且适配目标存在，且需要在LateUpdate中进行适配或者在编辑模式下
                if (m_FitTarget && (m_FitOnLateUpdate || !Application.isPlaying))
            #else
                // 如果不在Unity编辑器中，但是适配目标存在，并且需要在LateUpdate中进行适配
                if (m_FitTarget && m_FitOnLateUpdate)
            #endif
                {
                    // 调用适配函数，将图形适配到指定目标
                    FitTo(m_FitTarget);
                }

            // 调用平滑函数，对图形进行平滑处理
            Smoothing(graphic, m_EdgeSmoothing);
        }

        #if UNITY_EDITOR
        /// <summary>
        /// This function is called when the script is loaded or a value is changed in the inspector (Called in the editor only).
        /// </summary>
        private void OnValidate()
        {
            // 将图形标记为"脏"
            SetDirty();
        }
        #endif

        /// <summary>
        /// Mark the graphic as dirty.
        /// </summary>
        void SetDirty()
        {
            if (graphic)
            {
                // 将图形的材质标记为"脏"
                graphic.SetMaterialDirty();
            }
        }

        private static void Smoothing(MaskableGraphic graphic, float smooth)
        {
            // 检查传入的图形对象是否为空，如果为空则直接返回，不进行任何操作
            if (!graphic) return;

            // 开始性能分析器样本，用于性能调优
            Profiler.BeginSample("[Unmask] Smoothing");

            // 获取图形对象的画布渲染器
            var canvasRenderer = graphic.canvasRenderer;
            
            // 获取当前颜色
            var currentColor = canvasRenderer.GetColor();
            
            // 目标透明度初始化为1
            var targetAlpha = 1f;
            
            // 如果图形对象可遮罩并且平滑度大于0
            if (graphic.maskable && 0 < smooth)
            {
                // 计算当前透明度，考虑到图形对象的颜色和继承的透明度
                var currentAlpha = graphic.color.a * canvasRenderer.GetInheritedAlpha();
                
                // 如果当前透明度大于0
                if (0 < currentAlpha)
                {
                    // 根据平滑度计算目标透明度，确保不会出现除以0的情况
                    targetAlpha = Mathf.Lerp(0.01f, 0.002f, smooth) / currentAlpha;
                }
            }

            // 如果当前颜色的透明度与目标透明度不接近
            if (!Mathf.Approximately(currentColor.a, targetAlpha))
            {
                // 更新当前颜色的透明度，并确保其在0到1的范围内
                currentColor.a = Mathf.Clamp01(targetAlpha);
                
                // 设置更新后的颜色到画布渲染器
                canvasRenderer.SetColor(currentColor);
            }

            // 结束性能分析器样本
            Profiler.EndSample();
        }
    }
}
