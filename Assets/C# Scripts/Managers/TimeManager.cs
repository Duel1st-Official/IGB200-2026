using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    //singleton use, other scripts access it via TimeManager.instance
    public static TimeManager instance { get; private set; }

    //Day variables
    [SerializeField] private int _hoursPerDay = 12;
    [SerializeField] private int _currentDay = 1;

    public int hoursRemaining { get; private set; }
    public bool isNight { get; private set; }
    public int currentDay => _currentDay;

    //event calls
    public event Action onHoursChanged;
    public event Action onDayEnded;
    public event Action onDayStarted;

    
    private void Awake()
    {
       if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
       instance = this;
        StartDay();
    }

    private void StartDay()
    {
        hoursRemaining = _hoursPerDay;
        isNight = false;
        onDayEnded?.Invoke();
        onHoursChanged?.Invoke();
    }

    public bool TrySpendHours(int cost)
    {
        if (cost >  hoursRemaining)
        {
            return false;
        }
        hoursRemaining -= cost;
        onHoursChanged?.Invoke();
        return true;
    }

    public void EndDay()
    {
        isNight = true;
        onDayEnded?.Invoke();
        _currentDay++;
    }

    public void ConfirmDayReport()
    {
        StartDay();
    }

}
