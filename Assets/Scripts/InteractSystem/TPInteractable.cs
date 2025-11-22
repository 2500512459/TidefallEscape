using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TPInteractable : BaseInteractable
{
    public GameObject CarouselPanel;
    public GameObject Curtain;
    public Animator Animator;
    
    private Coroutine closeAnimationCoroutine;
    
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
            PlayerInput.Instance.playerInputAction.Control.Fire.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }
        else
        {
            Curtain.SetActive(true);
            CarouselPanel.SetActive(true);
            InteractHintUI.Instance.ShowHint("关闭传送界面", key);
            // 禁止移动和视角旋转，保留 E 键交互事件继续有效
            PlayerInput.Instance.DisableMovementAndLook(disableQuestInput: true);
            PlayerInput.Instance.playerInputAction.Control.Fire.Disable();

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
