using System.Collections.Generic;
using UnityEngine;

public class PlotDisasterSystem : MonoBehaviour
{
    [Header("Lightning during thunderstorms")]
    [Range(0f, 1f)] public float destructionChancePerStrike = 0.03f;
    [Min(0)] public int maximumLightningLossesPerDay = 2;
    [Header("Fire events")]
    [Min(0)] public int defaultFirePlotCount = 2;
    [Header("Critical soil")]
    [Range(0f, 100f)] public float criticalSoilHealth = 10f;
    [Min(1)] public int consecutiveCriticalDays = 3;
    public static PlotDisasterSystem Instance { get; private set; }
    private int lightningDay = -1, lightningLosses;
    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
    }
    public static PlotDisasterSystem GetOrCreate()
    {
        if (Instance == null) Instance = FindFirstObjectByType<PlotDisasterSystem>();
        if (Instance == null) Instance = new GameObject("Plot Disaster System").AddComponent<PlotDisasterSystem>();
        return Instance;
    }
    public void LightningStrike()
    {
        EndDaySystem day = FindFirstObjectByType<EndDaySystem>();
        if (Time.timeScale <= 0f || day == null || day.HasGameEnded() || day.IsTransitionRunning()) return;
        if (lightningDay != day.GetCurrentDay()) { lightningDay = day.GetCurrentDay(); lightningLosses = 0; }
        if (lightningLosses >= maximumLightningLossesPerDay || Random.value >= destructionChancePerStrike) return;
        lightningLosses += DestroyRandomPlots(1, "Lightning");
    }
    public int DestroyRandomPlots(int count, string cause)
    {
        EndDaySystem day = FindFirstObjectByType<EndDaySystem>();
        if (day != null && day.HasGameEnded()) return 0;
        List<MonoBehaviour> eligible = new List<MonoBehaviour>();
        foreach (Plot plot in FindObjectsByType<Plot>(FindObjectsSortMode.None))
            if (plot.isActiveAndEnabled && !plot.IsDestroyed()) eligible.Add(plot);
        foreach (Trap trap in FindObjectsByType<Trap>(FindObjectsSortMode.None))
            if (trap.isActiveAndEnabled && !trap.IsDestroyed()) eligible.Add(trap);
        foreach (WaterPlot water in FindObjectsByType<WaterPlot>(FindObjectsSortMode.None))
            if (water.isActiveAndEnabled && !water.IsDestroyed()) eligible.Add(water);
        int destroyed = 0;
        while (destroyed < Mathf.Max(0, count) && eligible.Count > 0)
        {
            int index = Random.Range(0, eligible.Count);
            MonoBehaviour item = eligible[index]; eligible.RemoveAt(index);
            bool hit = item is Plot plot ? plot.DestroyPlot(cause)
                : item is Trap trap ? trap.DestroyTrap(cause)
                : item is WaterPlot water && water.DestroyWaterPlot(cause);
            if (hit) destroyed++;
        }
        return destroyed;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
