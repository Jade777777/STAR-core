using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniverseGeneration : MonoBehaviour
{
    //constants
    //sizes refer to the width of each of the squares
    private const int ZONE_SIZE = 4, SQUARE_SIZE = 3;
    //public variables
    public static int universeLength = 2, universeWidth = 2;
    public static int seed= 100;
    //private variables
    public GameObject[] zones;
    public float zoneProb=0.1f;
    private int zonesGenerated;

    public SaveObject so;
    public GameObject planet;
    private UniverseSimulation universeSimulation;
  
    void Start()
    {
        Random.InitState(seed);
        universeSimulation = GetComponent<UniverseSimulation>();
        so = SaveManager.Load();
        so.worldSeed = seed;
        SaveManager.Save(so);
        GenerateUniverse();
    }
    public void setUniverseL(int x)
    {
        universeLength = x;
    }
    public void setUniverseW(int y)
    {
        universeWidth = y;
    }
    public void GenerateUniverse(){

        //generate universe
        for (int i = 0; i < universeLength; i++) {
            for (int j = 0; j < universeWidth; j++) {

                //generate each space zone
                for (int k = 0; k < ZONE_SIZE; k++) {
                    for (int l = 0; l < ZONE_SIZE; l++)
                    {
                        Vector3 zonePosition = new Vector3((j * ZONE_SIZE) * SQUARE_SIZE + l * SQUARE_SIZE, 0, (i * ZONE_SIZE) * SQUARE_SIZE + k * SQUARE_SIZE);

                        if (Random.value < zoneProb)
                        {
                            GameObject newZone = universeSimulation.GeneratePawn(planet, null, ("Space Zone " + (zonesGenerated) + ": (" + k + "," + l + ")"), zonePosition);
                            //GameObject newZone = Instantiate(zones[Random.Range(0, zones.Length)]);
                            //if (newZone.tag == "System") {
                            //    newZone.GetComponent<planet>().generateStructure(Random.Range(0,3), Random.Range(0,3), Random.Range(0,2));
                            //}
                            //newZone.transform.position = zonePosition;
                            //newZone.transform.SetParent(transform);
                            //newZone.name = ("Space Zone " + (zonesGenerated) + ": (" + k + "," + l + ")");
                        }
                        else
                        {
                            Debug.Log("NoGen");
                        }
                    }
                }
                zonesGenerated++;
            }
        }
    }

}
