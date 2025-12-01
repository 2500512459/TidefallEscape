using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TPInteractable : BaseInteractable
{
    public GameObject CarouselPanel;
    public GameObject Curtain;
    public Animator Animator;
    
    [Header("费用设置")]
    [Tooltip("传送费用(金币),默认为0表示免费")]
    public int cost = 0;
    
    private Coroutine closeAnimationCoroutine;
    
    private void Start()
    {
        var tpCanvas = GameObject.Find("TP Canvas");
        if (tpCanvas != null)
        {
            CarouselPanel = tpCanvas.transform.Find("Carousel Element")?.gameObject;
            Curtain = tpCanvas.transform.Find("Curtain")?.gameObject;
            if (Curtain != null)
            {
                Animator = Curtain.GetComponent<Animator>();
            }
        }
    }

    public override void Interact(Character player)
    {
        if (CarouselPanel.activeSelf)
        {
            if (closeAnimationCoroutine != null)
            {
                StopCoroutine(closeAnimationCoroutine);
            }
            closeAnimationCoroutine = StartCoroutine(ClosePanelSequence());
            InteractHintUI.Instance.ShowHint(hintText, key);
            
            PlayerInput.Instance.EnableMovementAndLook(enableQuestInput: true);
            PlayerInput.Instance.FireInput?.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }
        else
        {
            // 检查费用
            if (cost > 0)
            {
                var gamePanel = GamePanel.Instance;
                if (gamePanel != null && gamePanel.CurrencyData != null)
                {
                    var currencyData = gamePanel.CurrencyData;
                    currencyData.EnsureLoaded();
                    
                    // 检查是否有足够的金币
                    if (currencyData.GoldCoinAmount < cost)
                    {
                        InteractHintUI.Instance.ShowHint($"金币不足！需要 {cost} 金币", key);
                        return;
                    }
                    
                    // 扣除费用
                    currencyData.AddGoldCoins(-cost);
                }
            }
            
            Curtain.SetActive(true);
            CarouselPanel.SetActive(true);
            InteractHintUI.Instance.ShowHint("关闭传送界面", key);
            // 禁止移动和视角旋转，保留 E 键交互事件继续有效
            PlayerInput.Instance.DisableMovementAndLook(disableQuestInput: true);
            PlayerInput.Instance.FireInput?.Disable();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    private IEnumerator ClosePanelSequence()
    {
        // 播放 Close 动画
        Animator.SetTrigger("Close");
        
        // 等待 Close 动画播放完毕
        yield return WaitForAnimation("Close");
        
        // Close 动画播放完毕后，关闭 CarouselPanel
        CarouselPanel.SetActive(false);
        
        // 播放 Open 动画
        Animator.SetTrigger("Open");
        
        // 等待 Open 动画播放完毕
        yield return WaitForAnimation("Open");
        
        // Open 动画播放完毕后，失能 Curtain
        Curtain.SetActive(false);
        
        closeAnimationCoroutine = null;
    }
    
    private IEnumerator WaitForAnimation(string animationName)
    {
        int animationHash = Animator.StringToHash(animationName);
        
        // 等待动画开始播放
        while (!Animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
        {
            yield return null;
        }
        
        // 等待动画播放完毕（归一化时间 >= 1）
        while (Animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null;
        }
    }
}
