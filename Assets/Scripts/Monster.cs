using UnityEngine;

public class Monster : MonoBehaviour
{
    public float moveSpeed = 2f;

    public float destroyY = -0.5f;

    void Start()
    {
        float randomX = Random.Range(-0.4f, 0.4f);
        transform.position = new Vector3(randomX, 2, -7.5f);
    }
    private void Update()
    {
        transform.Translate(Vector2.down * moveSpeed * Time.deltaTime);

        if (transform.position.y < destroyY)

        {
            Destroy(gameObject);
        }
    }
}
