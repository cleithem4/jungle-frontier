using UnityEngine;

public class Tree : MonoBehaviour
{
    public float chopTime = 2f; // seconds to chop the tree
    private bool isChopped = false;

    private PlayerScript player;
    public GameObject woodPiecePrefab; // assign in inspector

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isChopped)
        {
            player = other.GetComponent<PlayerScript>();
            if (player != null)
            {
                player.SetNearTree(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (player != null)
            {
                player.ClearNearTree(this);
            }
        }
    }

    public void ChopTree()
    {
        isChopped = true;
        Debug.Log("Tree chopped!");

        // Spawn 5 wood pieces with random force
        for (int i = 0; i < 5; i++)
        {
            Quaternion spawnRotation = Quaternion.Euler(90f, 90f, 0f);
            GameObject woodPiece = Instantiate(woodPiecePrefab, transform.position + Vector3.up * 1f, spawnRotation);

            Rigidbody rb = woodPiece.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 randomForce = new Vector3(Random.Range(-2f, 2f), Random.Range(2f, 5f), Random.Range(-2f, 2f));
                rb.AddForce(randomForce, ForceMode.Impulse);
            }

            if (this.player != null)
            {
                float delay = Random.Range(0f, 0.3f); // random delay so they don't fly all at once

                Wood woodComponent = woodPiece.GetComponent<Wood>();
                if (woodComponent != null)
                {
                    woodComponent.Init(this.player, this.player.woodStackPoint, delay);
                }
            }
        }

        Destroy(gameObject);
    }

    public float GetChopTime()
    {
        return chopTime;
    }
}
