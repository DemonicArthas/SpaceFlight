using UnityEngine;

public class SpaceGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject asteroidPrefab;

    [SerializeField]
    private int generationRange = 100;

    [SerializeField]
    private int asteroidsAmount = 200;

    void Start()
    {
        GenerateAsteroids();
    }

    private void GenerateAsteroids()
    {
        for (int i = 0; i < asteroidsAmount; i++)
        {
            var randomSpawnPosition = new Vector3(Random.Range(-generationRange, generationRange), 
                Random.Range(-generationRange, generationRange), 
                Random.Range(-generationRange, generationRange));
            var randomSpawnRotation = new Quaternion(Random.Range(-180, 180), 
                Random.Range(-180, 180), 
                Random.Range(-180, 180), 
                Random.Range(-180, 180));
            GameObject newAsteroid = Instantiate(asteroidPrefab, randomSpawnPosition, randomSpawnRotation, gameObject.transform);
            float randomSpawnScale = Random.Range(1, 5);
            newAsteroid.transform.localScale = new Vector3(randomSpawnScale, randomSpawnScale, randomSpawnScale);
        }
    }
}
