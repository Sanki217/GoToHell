using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 1;
    public int currentHealth;
    public GameObject orbPrefab;    

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
            

        }
    }

    private void Die()
    {

        int orbCount = Random.Range(1, 3); // spawn 1 or 2 orbs

        for (int i = 0; i < orbCount; i++)
        {
            if (orbPrefab == null) break;

            GameObject o = Instantiate(orbPrefab, transform.position, Quaternion.identity);

            Orb orb = o.GetComponent<Orb>();
            if (orb != null)
            {
                // pick random horizontal angle and some upward
                float angle = Random.Range(-60f, 60f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;

                float force = Random.Range(4f, 9f); // tweak in inspector by setting orb prefab defaults
                orb.Initialize(dir, force);
            }
        }

        // Add death effects here later (particles, sounds)
        Camera.main.GetComponent<CameraFollow>()?.Shake(0.2f, 0.2f); // big shake
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerEnergy energy = player.GetComponent<PlayerEnergy>();
            if (energy != null)
            {
                energy.RestoreEnergy(10); // Adjust value as needed
            }
        }
        Destroy(gameObject);
    }
}
