using UnityEngine;
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
        // 常量或静态成员。
        //################################
        private static readonly Vector2 s_Center = new Vector2(0.5f, 0.5f);


        //################################
        // 序列化成员。
        //################################
        [Tooltip("将图形的变换适配到目标变换。")]
        [SerializeField] private RectTransform m_FitTarget;

        [Tooltip("在每帧的 LateUpdate 中适配图形的变换到目标变换。")]
        [SerializeField] private bool m_FitOnLateUpdate;

        [Tooltip("解除遮罩只影响子对象。")]
        [SerializeField] private bool m_OnlyForChildren = false;

        [Tooltip("显示与解除遮罩渲染区域相关联的图形。")]
        [SerializeField] private bool m_ShowUnmaskGraphic = false;

        [Tooltip("边缘平滑度。")]
        [Range(0f, 1f)]
        [SerializeField] private float m_EdgeSmoothing = 0f;

        //################################
        // 公共成员。
        //################################

        /// <summary>
        /// 与解除遮罩相关联的图形。
        /// </summary>
        public MaskableGraphic graphic
        {
            get { return _graphic ?? (_graphic = GetComponent<MaskableGraphic>()); }
        }

        /// <summary>
        /// 将图形的变换适配到目标变换。
        /// </summary>
        public RectTransform fitTarget
        {
            get { return m_FitTarget; }
            set
            {
                // 设置属性值，并将图形适配到目标变换
                m_FitTarget = value;
                FitTo(m_FitTarget);
            }
        }

        /// <summary>
        /// 在每帧的 LateUpdate 中适配图形的变换到目标变换。
        /// </summary>
        public bool fitOnLateUpdate
        {
            get { return m_FitOnLateUpdate; }
            set { m_FitOnLateUpdate = value; }
        }

        /// <summary>
        /// 显示与解除遮罩渲染区域相关联的图形。
        /// </summary>
        public bool showUnmaskGraphic
        {
            get { return m_ShowUnmaskGraphic; }
            set
            {
                // 设置属性值并标记图形为"脏"
                m_ShowUnmaskGraphic = value;
                SetDirty();
            }
        }

        /// <summary>
        /// 解除遮罩只影响子对象。
        /// </summary>
        public bool onlyForChildren
        {
            get { return m_OnlyForChildren; }
            set
            {
                // 设置属性值并标记图形为"脏"
                m_OnlyForChildren = value;
                SetDirty();
            }
        }

        /// <summary>
        /// 边缘平滑度。
        /// </summary>
        public float edgeSmoothing
        {
            get { return m_EdgeSmoothing; }
            set { m_EdgeSmoothing = value; }
        }

        /// <summary>
        /// 在此函数中执行材质修改。
        /// </summary>
        /// <returns>修改后的材质。</returns>
        /// <param name="baseMaterial">配置好的材质。</param>
        public Material GetModifiedMaterial(Material baseMaterial)
        {
            // 如果当前对象未启用，则直接返回原始材质
            if (!isActiveAndEnabled)
            {
                return baseMaterial;
            }

            // 查找要停止处理的Canvas的Transform
            Transform stopAfter = MaskUtilities.FindRootSortOverrideCanvas(transform);
            
            // 获取当前对象到停止处理的Canvas的遮罩深度
            var stencilDepth = MaskUtilities.GetStencilDepth(transform, stopAfter);
            
            // 计算所需的模板缓冲位
            var desiredStencilBit = 1 << stencilDepth;

            // 移除之前的解除遮罩材质
            StencilMaterial.Remove(_unmaskMaterial);
            
            // 添加新的解除遮罩材质
            _unmaskMaterial = StencilMaterial.Add(baseMaterial, desiredStencilBit - 1, StencilOp.Invert, CompareFunction.Equal, m_ShowUnmaskGraphic ? ColorWriteMask.All : (ColorWriteMask)0, desiredStencilBit - 1, (1 << 8) - 1);

            // 获取图形对象的CanvasRenderer
            var canvasRenderer = graphic.canvasRenderer;
            
            // 如果仅对子对象进行解除遮罩
            if (m_OnlyForChildren)
            {
                // 移除之前的重新应用遮罩材质
                StencilMaterial.Remove(_revertUnmaskMaterial);
                
                // 添加新的重新应用遮罩材质
                _revertUnmaskMaterial = StencilMaterial.Add(baseMaterial, (1 << 7), StencilOp.Invert, CompareFunction.Equal, (ColorWriteMask)0, (1 << 7), (1 << 8) - 1);
                
                // 启用渲染器的POP指令
                canvasRenderer.hasPopInstruction = true;
                canvasRenderer.popMaterialCount = 1;
                
                // 设置渲染器的POP材质
                canvasRenderer.SetPopMaterial(_revertUnmaskMaterial, 0);
            }
            else
            {
                // 如果不仅对子对象进行解除遮罩，则禁用POP指令
                canvasRenderer.hasPopInstruction = false;
                canvasRenderer.popMaterialCount = 0;
            }

            // 返回解除遮罩材质
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
