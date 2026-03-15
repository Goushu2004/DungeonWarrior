using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public GameObject[] enemys;
    public GameObject enemySpawnPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instantiate(enemys[0], enemySpawnPosition.transform.position, enemys[0].transform.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
