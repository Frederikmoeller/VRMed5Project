using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CharacterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject CharacterPrefab;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i <= 99; i++)
        {
            float startPositionX = Random.Range(-49f, 49f);
            float startPositionZ = Random.Range(-49f, 49f);
            Instantiate(CharacterPrefab, new Vector3(startPositionX, 0f, startPositionZ), Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
