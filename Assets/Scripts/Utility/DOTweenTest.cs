using UnityEngine;
using DG.Tweening; // This should NOT show errors now

public class DOTweenTest : MonoBehaviour
{
    void Start()
    {
        // Simple test animation
        transform.DOMoveY(transform.position.y + 2f, 2f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
        
        Debug.Log("DOTween is working perfectly!");
    }
}