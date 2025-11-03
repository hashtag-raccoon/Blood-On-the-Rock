using UnityEngine;

public class LiquidSensor : MonoBehaviour
{
    public System.Action<GameObject> onEnter;
    public System.Action<GameObject> onExit;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Liquid")) 
        {
            onEnter?.Invoke(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Liquid"))
        {
            onExit?.Invoke(other.gameObject);
        }
    }
}