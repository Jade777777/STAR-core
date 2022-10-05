using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UniverseChronology : MonoBehaviour
{
    UniverseSimulation universeSimulation;
    HashSet<FactionCommander> readiedFactions = new();
    public TurnPhase currentPhase { get; private set; }
    public UnityEvent MainPhaseStart = new();
    public UnityEvent MainPhaseEnd= new();
    public UnityEvent CombatPhaseStart = new();
    public UnityEvent CombatPhaseEnd = new();

    //Required for initialization. If this method doesn't get called it won't funtion properly
    public void EstablishUniverseChronology(UniverseSimulation universeSimulation)
    {
        if (this.universeSimulation != null)
        {
            Debug.LogError("The UniverseSimulation should never Change! This method should only be called durring initialization");
        }
        else
        {
            this.universeSimulation = universeSimulation;
            StartCoroutine(TurnPhase());
        }
    }


    IEnumerator TurnPhase()
    {
        float transitionTime=3;
        int currentRound = 0;



        while (true)
        {
            currentRound++;

            //Trasition to main
            currentPhase = global::TurnPhase.TransitionToMain;
            Debug.Log(currentPhase);
            yield return new WaitForSeconds(transitionTime);
            readiedFactions.Clear();
            //----

            //Main 
            currentPhase = global::TurnPhase.Main;
            MainPhaseStart.Invoke();
            Debug.Log(currentPhase);
            yield return new WaitUntil(() => (readiedFactions.SetEquals(universeSimulation.factionsInPlay)));//set equals checks if the sets are equal, it does nto set them to equivilant values lol
            MainPhaseEnd.Invoke();
            //----

            //Transition to combat
            currentPhase = global::TurnPhase.TransitionToCombat;
            Debug.Log(currentPhase);
            yield return new WaitForSeconds(transitionTime);
            readiedFactions.Clear();
            //----

            //Combat
            currentPhase = global::TurnPhase.Combat;
            CombatPhaseStart.Invoke();
            Debug.Log(currentPhase);
            yield return new WaitUntil(() => (readiedFactions.SetEquals(universeSimulation.factionsInPlay)));//set equals checks if the sets are equal, it does nto set them to equivilant values lol
            CombatPhaseEnd.Invoke();
            //----

            Debug.Log("***Round Complete***");
            

        }
    }




    public bool MarkFactionReady(FactionCommander factionCommander)
    {
        if (readiedFactions.Add(factionCommander))
        {
            Debug.Log(factionCommander.factionName + " has completed the " + currentPhase + " phase.");
            return true;
        }
        else
        {
            readiedFactions.Remove(factionCommander);
            Debug.Log(factionCommander.factionName + " has allready completed the " + currentPhase + " phase!" + "\n" + "Canceling ready Status!");
            return false;
        }

    }



}
public enum TurnPhase { TransitionToMain, Main, TransitionToCombat, Combat };