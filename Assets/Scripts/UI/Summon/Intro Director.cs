using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Intro Director 控制器
/// 控制 Timeline 播放，并在停止后禁用所有被激活的子对象
/// </summary>
public class IntroDirector : MonoBehaviour
{
    public PlayableDirector director;
    [SerializeField] private Animator animator;
    [SerializeField] private CanvasGroup canvasGroup;
    
    /// <summary>
    /// 存储所有子对象
    /// </summary>
    [SerializeField] private GameObject[] objects;

    private void Awake()
    {
        director = GetComponent<PlayableDirector>();
        director.stopped += OnPlayableDirectorStoped;
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && director.state == PlayState.Playing)
        {
            director.Stop();
        }
    }


    private void OnPlayableDirectorStoped(PlayableDirector director)
    {
        // 禁用所有子对象（Timeline 通过 Activation Track 使能的对象）
        StartCoroutine(DisableAllChildrenCoroutine());
    }
    
    private IEnumerator DisableAllChildrenCoroutine()
    {
        // ------ 1. 平滑淡出 CanvasGroup ------
        float duration = 1f;
        float startAlpha = canvasGroup.alpha;
        float endAlpha = 0f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;


        // ------ 2. 禁用所有子对象 ------
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        // ------ 3. 触发 End 动画 ------
        animator.SetTrigger("End");
        yield return new WaitForSeconds(1f);

        animator.gameObject.SetActive(false);

        // ------ 4. 最终禁用自身 ------
        if (gameObject != null)
        {
            gameObject.SetActive(false);
        }
    }

}
