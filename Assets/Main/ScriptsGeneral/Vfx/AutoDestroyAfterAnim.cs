using UnityEngine;

public class AutoDestroyAfterAnim : MonoBehaviour
{
    public void DestroySelf()
    {
        SimplePool.Despawn(gameObject);
    }
}
