using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager instance { get; private set; }

    //population
    [Header("Population")]
    [SerializeField] private int _batPop = 10;
    [SerializeField] private int _maxBatPop = 800;
    public int batPop => _batPop;


    //managed variables
    [Header("Managed variables")]
    [SerializeField] private float _floraPrey = 5f;
    [SerializeField] private float _predatorPressure = 0f;
    [SerializeField] private float _fireRisk = 0f;
    [SerializeField] private float _roostQuality = 100f;

    public float floraPrey => _floraPrey;
    public float predatorPressure => _predatorPressure;
    public float fireRisk => _fireRisk;
    public float roostQuality => _roostQuality;

    //fencing
    [Header("Fencing")]
    [SerializeField] private float _fenceCondition = 100f;
    [SerializeField] private float _fenceRepairThreshold = 30f;
    [SerializeField] private float _predatorPressureRisePerTick = 5f;
    [SerializeField] private float _fenceDecayPerDay = 5f;
    public float fenceCondition => _fenceCondition;

    //events
    public event Action onPopulationChanged;
    public event Action onCoreVariablesChanged;
    public event Action onFenceConditionChanged;
    public event Action onPopulationFailed; // triggered only if population drops below 2

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        TimeManager.instance.onDayStarted += DecayFence;
    }

    public void ModifyPopulation(int amount)
    {
        _batPop = Mathf.Clamp(_batPop + amount, 0, _maxBatPop);
        onPopulationChanged?.Invoke();

        if (_batPop < 2)
        {
            onPopulationFailed?.Invoke();
        }
    }

    public void ModifyFloraPrey(float amount)
    {
        _floraPrey = Mathf.Clamp(_floraPrey + amount, 0f, 100f);
        onCoreVariablesChanged?.Invoke();
    }

    public void ModifyPredatorPressure(float amount)
    {
        _predatorPressure = Mathf.Clamp(_predatorPressure + amount, 0f, 100f);
        onCoreVariablesChanged?.Invoke();
    }

    public void ModifyFireRisk(float amount)
    {
        _fireRisk = Mathf.Clamp(_fireRisk + amount, 0f, 100f);
        onCoreVariablesChanged?.Invoke();
    }

    public void ModifyRoostQuality(float amount)
    {
        _roostQuality = Mathf.Clamp(_roostQuality + amount, 0f, 100f);
        onCoreVariablesChanged?.Invoke();
    }

    public void ModifyFenceCondition(float amount)
    {
        _fenceCondition = Mathf.Clamp(_fenceCondition + amount, 0f, 100f);
        onFenceConditionChanged?.Invoke();

        if (_fenceCondition < _fenceRepairThreshold)
        {
            ModifyPredatorPressure(_predatorPressureRisePerTick);
        }
    }
    /*
    private void OnEnable()
    {
        TimeManager.instance.onDayStarted += DecayFence;
    }*/

    private void OnDisable()
    {
        TimeManager.instance.onDayStarted -= DecayFence;
    }

    private void DecayFence()
    {
        ModifyFenceCondition(-_fenceDecayPerDay);
    }
}
