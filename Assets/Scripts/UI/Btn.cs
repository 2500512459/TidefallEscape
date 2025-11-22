using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TidefallEscape.UI
{
    /// <summary>
    /// 通用按钮交互脚本：鼠标悬停放大，移开恢复。
    /// </summary>
    public class Btn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        [Tooltip("鼠标悬停时的放大倍数。")]
        private float hoverScaleMultiplier = 1.15f;

        [SerializeField]
        [Tooltip("缩放动画时长（秒）。")]
        private float tweenDuration = 0.15f;

        private Vector3 _originalScale;
        private Tween _scaleTween;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayScaleTween(_originalScale * hoverScaleMultiplier);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PlayScaleTween(_originalScale);
        }

        private void OnDisable()
        {
            _scaleTween?.Kill();
            transform.localScale = _originalScale;
        }

        private void PlayScaleTween(Vector3 targetScale)
        {
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(targetScale, tweenDuration)
                .SetEase(Ease.OutQuad);
        }
    }
}

