namespace SheetNotify.Services;

/// <summary>
/// Persists the last processed row index to a local file so we survive restarts
/// without re-sending messages to previous customers.
/// </summary>
public class StateTracker(ILogger<StateTracker> logger)
{
    private const string StateFile = "logsift-state.txt";

    public int GetLastProcessedRow()
    {
        try
        {
            if (File.Exists(StateFile))
                return int.TryParse(File.ReadAllText(StateFile).Trim(), out var n) ? n : 1;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read state file — defaulting to row 1.");
        }
        return 1;
    }

    public void SaveLastProcessedRow(int row)
    {
        try
        {
            File.WriteAllText(StateFile, row.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not save state file — duplicate messages may be sent on restart.");
        }
    }
}
