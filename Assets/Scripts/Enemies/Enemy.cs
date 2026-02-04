using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 1;
    public int currentHealth;

    public GameObject orbPrefab;
    public float energyRestoredOnDeath = 5f;
    public int minimumOrbs = 1;
    public int maximumOrbs = 3;
    private void Start()
    {
        currentHealth = maxHealth;

    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        int orbCount = Random.Range(minimumOrbs, maximumOrbs);

        for (int i = 0; i < orbCount; i++)
        {
            if (!orbPrefab) break;

            GameObject o = Instantiate(orbPrefab, transform.position, Quaternion.identity);
            Orb orb = o.GetComponent<Orb>();
            if (orb != null)
            {
                float angle = Random.Range(-60f, 60f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;
                orb.Initialize(dir, Random.Range(4f, 9f));
            }
        }

        Camera.main?.GetComponent<CameraFollow>()?.Shake(0.08f, 0.08f);

        GameObject player = GameObject.FindWithTag("Player");
        if (player)
        {
            player.GetComponent<PlayerEnergy>()?.RestoreEnergy(energyRestoredOnDeath);

            var upgradeMgr = player.GetComponent<PlayerUpgradeManager>();
            if (upgradeMgr != null)
                upgradeMgr.EnemyKilled(gameObject);
        }

        Destroy(gameObject);

    }
}
