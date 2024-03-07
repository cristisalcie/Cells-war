using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodCellsManager : MonoBehaviour
{
    public GameObject foodCellPrefab;

    private const int maxNumberOfFoodCells = 100;

    private void Start()
    {
        // Instantiate all food cells
        for (int i = 0; i < maxNumberOfFoodCells; i++)
        {
            // Currently, the instance of every food cell is not necessary to store
            Instantiate(foodCellPrefab, Vector3.zero, Quaternion.identity, transform);
        }
    }
}
