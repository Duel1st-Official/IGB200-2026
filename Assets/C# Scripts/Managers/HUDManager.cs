using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Text elements")]
    [SerializeField] private TMP_Text _dayText;
    [SerializeField] private TMP_Text _hoursText;

    [Header("Bar elements")]
    [SerializeField] private Slider _populationBar;
    [SerializeField] private Slider _floraPreyBar;
    [SerializeField] private Slider _predatorPressureBar;
    [SerializeField] private Slider _fireRiskBar;

    [Header("Button elements")]
    [SerializeField] private Button _endDayButton;
    [SerializeField] private Button _giveTourButton;


    private void Start()
    {
        GameManager.instance.onPopulationChanged += UpdatePopulation;
        GameManager.instance.onCoreVariablesChanged += UpdateCoreVariables;
        TimeManager.instance.onHoursChanged += UpdateTime;
        TimeManager.instance.onDayStarted += UpdateTime;

        _endDayButton.onClick.AddListener(TimeManager.instance.EndDay);

        UpdatePopulation();
        UpdateCoreVariables();
        UpdateTime();
    }
    /*
    private void OnEnable()
    {
        GameManager.instance.onPopulationChanged += UpdatePopulation;
        GameManager.instance.onCoreVariablesChanged += UpdateCoreVariables;
        TimeManager.instance.onHoursChanged += UpdateTime;
        TimeManager.instance.onDayStarted += UpdateTime;

        _endDayButton.onClick.AddListener(TimeManager.instance.EndDay);
    }*/

    private void OnDisable()
    {
        GameManager.instance.onPopulationChanged -= UpdatePopulation;
        GameManager.instance.onCoreVariablesChanged -= UpdateCoreVariables;
        TimeManager.instance.onHoursChanged -= UpdateTime;
        TimeManager.instance.onDayStarted -= UpdateTime;

        _endDayButton.onClick.RemoveListener(TimeManager.instance.EndDay);
    }

    private void UpdatePopulation()
    {
        _populationBar.value = GameManager.instance.batPop;
    }

    private void UpdateCoreVariables()
    {
        _floraPreyBar.value = GameManager.instance.floraPrey;
        _predatorPressureBar.value = GameManager.instance.predatorPressure;
        _fireRiskBar.value = GameManager.instance.fireRisk;
    }

    private void UpdateTime()
    {
        _dayText.text = "Day: " + TimeManager.instance.currentDay;
        _hoursText.text = TimeManager.instance.hoursRemaining + " hrs left";
    }
}
