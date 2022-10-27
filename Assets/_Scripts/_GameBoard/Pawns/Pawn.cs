using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;
public abstract class Pawn : MonoBehaviour
{
    [SerializeField]
    UniverseSimulation universeSimulation;
    [SerializeField]
    FactionCommander faction;

    public GameObject componentMenu;
    public GameObject foreignComponentMenu;
    public Transform componentContainer;
    public Transform foreignComponentContainer;
    public GameObject statsMenu;
    public TMP_Text statsText;

    public Dictionary<string, float> stats = new();

    #region Copy and lock inpsector value hack
    [SerializeField]
    private List<GameObject> setPawnComponents;//variable exposed in the inspector
    private List<GameObject> PawnComponentsReference => pawnComponents; // this holds a reference to the pawnComponents list that can not be written to
    private void CopyInspectorPawnValues()//call in awake
    {
        pawnComponents = new();
        foreach (GameObject pawnComponent in setPawnComponents)
        {
            
            if (pawnComponent != null)
            {
                AddPawnComponent(pawnComponent);
            }
            else
            {
                pawnComponents.Add(null);
            }
        }
        setPawnComponents = PawnComponentsReference;
    }


    #endregion
    [SerializeField]// why does this make it work? I had a feeling it would work and it did, I don't have time to look into right now unfortunatley
    [HideInInspector]
    private List<GameObject> pawnComponents;//this is the real list that should be referenced by the code and shown at runtime
    private Dictionary<string, List<PawnComponent>> pawnComponentPriorityLists;
    Action Move;
    Action Attack;


    private void Start()// we want to close these menus after they awake
    {
        CloseComponentMenu();
        CloseStatMenu();
    }


    public virtual void EstablishPawn(string name, UniverseSimulation universeSimulation, FactionCommander faction)
    {
        this.name = name;
        this.universeSimulation = universeSimulation;
        this.faction = faction;

        universeSimulation.universeChronology.MainPhaseStart.AddListener(() => OnMainPhaseStart());
        universeSimulation.universeChronology.MainPhaseEnd.AddListener(() => OnMainPhaseEnd());
        universeSimulation.universeChronology.CombatPhaseStart.AddListener(() => OnCombatPhaseStart());
        universeSimulation.universeChronology.CombatPhaseEnd.AddListener(() => OnCombatPhaseEnd());

        CopyInspectorPawnValues();

        DamagePawn(8f);

    }




    public void DefaultAction(FactionCommander faction)
    {
        switch (universeSimulation.universeChronology.currentPhase)
        {
            case TurnPhase.Main:
                if (faction == this.faction)
                {
                    Debug.Log("Friendly default main action!");
                }
                else
                {
                    Debug.Log("Foreign default main action!");
                }
                break;
            case TurnPhase.Combat:
                if (faction == this.faction)
                {
                    Debug.Log("Friendly default combat action!");
                }
                else
                {
                    Debug.Log("Foreign default combat action!");
                }
                break;
            default:
                Debug.Log("No default action for phase " + universeSimulation.universeChronology.currentPhase);
                break;
        }

    }

    public void OpenComponentMenu(FactionCommander faction)
    {
        if (faction == this.faction)
        {
            componentMenu.SetActive(true);
            Debug.Log("Opening"+ this +" Component Menu");
        }
        else
        {
            foreignComponentMenu.SetActive(true);
            Debug.Log("Opening "+ this +" Foreign Menu");
        }
        
    }
    public void CloseComponentMenu()
    {
        componentMenu.SetActive(false);
        foreignComponentMenu.SetActive(false);
        Debug.Log(this + " is closing menus");
    }
    public void OpenStatMenu(FactionCommander faction)
    {
        if (faction == this.faction)
        {
            statsMenu.SetActive(true);
            Debug.Log("Opening" + this + " Stat Menu");
        }
        else
        {

            Debug.Log("Opening " + this + " Foreign Stat Menu");
        }
    }
    public void CloseStatMenu()
    {
        statsMenu.SetActive(false);
    }




    private void AddPawnComponent(GameObject pawnComponent)
    {
        Debug.Assert(pawnComponent.TryGetComponent(typeof(PawnComponent), out _));
        GameObject newPawnComponent = Instantiate(pawnComponent,componentContainer);
        pawnComponents.Add(newPawnComponent);
        PawnComponent c = newPawnComponent.GetComponent<PawnComponent>();
        if (c.isForeign)
        {
            newPawnComponent.transform.SetParent( foreignComponentContainer);
        }
        else
        {
            newPawnComponent.transform.SetParent(componentContainer);
        }
        
        c.EstablishPawnComponent(this, universeSimulation);

        UpdatePrioritys();
        UpdateStats();
    }

    private void RemovePawnComponent(GameObject pawnComponent)
    {
        pawnComponents.Remove(pawnComponent);

        UpdatePrioritys();
        UpdateStats();
        Destroy(pawnComponent);
    }


    public void UpdatePrioritys()
    {
        pawnComponentPriorityLists = new();
        foreach(GameObject pawnComponent  in pawnComponents)
        {
            PawnComponent script = pawnComponent.GetComponent<PawnComponent>();
            foreach(KeyValuePair<string,int> priority in script.Prioritys)
            {
                pawnComponentPriorityLists.TryAdd(priority.Key, new List<PawnComponent>());
                pawnComponentPriorityLists[priority.Key].Add(script);
            }
        }
        foreach(KeyValuePair<string, List<PawnComponent>> priorityList in pawnComponentPriorityLists)
        {
            string priorityName = priorityList.Key;
            pawnComponentPriorityLists[priorityName].Sort((priorityA, priorityB) => priorityB.Prioritys[priorityName].CompareTo(priorityA.Prioritys[priorityName]));
        }


        Debug.Log("______Draw Order______");
        for (int i = 0; i < pawnComponentPriorityLists["DrawOrder"].Count; i++) {
            pawnComponentPriorityLists["DrawOrder"][i].transform.SetSiblingIndex(i);
            Debug.Log(i + ".    " + pawnComponentPriorityLists["DrawOrder"][i].name);
        }

    }
    
    public void UpdateStats()
    {
        stats = new();
        foreach (GameObject pawnComponent in pawnComponents)
        {
            PawnComponent script = pawnComponent.GetComponent<PawnComponent>();
            foreach(KeyValuePair<string,float> stat in script.Stats)
            {
                stats.TryAdd(stat.Key, 0); //if the stat is not present creat a new one with a starting value of 0
                stats[stat.Key] += stat.Value;
            }
        }

        //update stat UI
        string statString = "";
        foreach(KeyValuePair<string, float> stat in stats)
        {
            statString += stat.Key + ": " + stat.Value + "\n";
        }
        statsText.text = statString;
    }

    public void DamagePawn(float damage)
    {
        float excess = damage;
        if (pawnComponentPriorityLists.ContainsKey("DamageOrder"))
        {
            for (int i = 0; i < pawnComponentPriorityLists["DamageOrder"].Count; i++)
            {
                excess = pawnComponentPriorityLists["DamageOrder"][i].DamageComponent(excess);
            }
        }

        if(excess > 0)
        {
            Debug.Log("CRITICAL DAMAGE HAS BEEN SUSTAINED!!!!");
        }
        
    }

    public float GetAggregatePower()
    {
        if (stats.TryGetValue("Aggregate Power", out float power))
        {
            return power;
        }
        else
        {
            stats.Add("Aggregate Power", 0);
            return 0;
        }
    }

    public float GetWeaponsPowerPercent()
    {
        if(stats.TryGetValue("Weapons Power", out float power))
        {
            return power;
        }
        else
        {
            stats.Add("Weapons Power", 0);
            return 0;
        }
    }
   
    public float GetSheildsPowerPercent()
    {
        if (stats.TryGetValue("Sheilds Power", out float power))
        {
            return power;
        }
        else
        {
            stats.Add("Sheilds Power", 0);
            return 0;
        }
    }


    public float GetThrustersPowerPercent()
    {
        if (stats.TryGetValue("Thrusters Power", out float power))
        {
            return power;
        }
        else
        {
            stats.Add("Thrusters Power", 0);
            return 0;
        }
    }
    



    protected virtual void OnPhaseTransition()
    {
        Move = () => { Debug.Log("No move action taken!"); };
        Attack = () => { Debug.Log("No attack action taken!"); };
        CloseComponentMenu();
    }


    protected virtual void OnMainPhaseStart()
    {
        OnPhaseTransition();
    }
    protected virtual void OnMainPhaseEnd()
    {
        
        Move();
        OnPhaseTransition();
    }
    protected virtual void OnCombatPhaseStart()
    {
        OnPhaseTransition();
    }
    protected virtual void OnCombatPhaseEnd()
    {
        Attack();
        OnPhaseTransition();

    }



    public void SetMovePattern(Action moveMethod)
    {
        Move = moveMethod;
    }
    public void SetAttackPattern(Action attackMethod)
    {
        Attack = attackMethod;
    }









    //Getters/setters

    public FactionCommander GetFaction()
    {
        return faction;
    }
    public void SetFaction(FactionCommander faction)
    {
        this.faction = faction;
    }

}

