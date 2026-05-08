using UnityEngine;
public class CubeSpawner : MonoBehaviour
{
    public GameObject cubePrefab; 
    private float offset = 0f;
    void Start()
    {
        
        InvokeRepeating("SpawnCube", 0f, 1f); 
        
   
    }

    void SpawnCube()
    {
        
        if (offset >= 5f) 
        StopSpawning();
        Vector3 spawnPosition = new Vector3(offset, 0, 0); 
        Instantiate(cubePrefab, spawnPosition, Quaternion.identity); 
        offset += 1f; 
       
    }

    void StopSpawning()
    {
    CancelInvoke("SpawnCube"); 
    }

}