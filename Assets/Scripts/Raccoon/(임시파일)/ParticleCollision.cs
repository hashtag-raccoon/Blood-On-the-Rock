using UnityEngine;

public class ParticleCollision : MonoBehaviour
{
    public LiquidPouring pourObj; // LiquidEnter 호출용

    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Cup"))
        {
            // Particle가 컵에 닿았을 때 호출
            pourObj.LiquidEnter(other);
        }
    }
}