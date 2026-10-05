using System;

public enum TutorialAction
{
    None, PlayerMoved, RangerStationInspected, CaveInspected,
    SelectionWheelOpened, BuildModeSelected, FarmPlotPlaced, CropPlanted,
    WaterPlotPlaced, TrapPlaced, TourStarted, DayEndReached,
    EndDayPressed, NightReportClosed
}

public static class TutorialEvents
{
    public static event Action<TutorialAction> ActionReported;

    public static void Report(TutorialAction action)
    {
        if (action != TutorialAction.None)
            ActionReported?.Invoke(action);
    }
}
