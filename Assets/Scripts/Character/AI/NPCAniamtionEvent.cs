using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NPCAniamtionEvent : MonoBehaviour
{
    public NPC npc;
    public void DeathEvent()
    {
        if (npc == null) return;
        npc.Dissolution();
    }
}
