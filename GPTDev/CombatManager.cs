using System.Threading.Tasks;

public class CombatManager
{
    private object? _state;

    public bool IsInProgress { get; private set; }
    public bool IsPlayPhase { get; private set; }
    public bool IsPaused { get; private set; }

    public void SetUpCombat(object state)
    {
        _state = state;

        // Initialize combat state.
    }

    public async Task StartCombat()
    {
        IsInProgress = true;

        // Before combat starts.

        await StartTurn();
    }

    private async Task StartTurn()
    {
        if (!IsInProgress)
            return;

        await WaitIfPaused();

        if (IsPlayerTurn())
        {
            await StartPlayerTurn();
        }
        else
        {
            await StartEnemyTurn();
        }
    }

    private async Task StartPlayerTurn()
    {
        IsPlayPhase = false;

        // Player turn start.

        if (CheckCombatEnd())
        {
            await EndCombat();
            return;
        }

        IsPlayPhase = true;

        // Player can now act.
    }

    public async Task SetReadyToEndTurn()
    {
        IsPlayPhase = false;

        // Player turn end.

        if (CheckCombatEnd())
        {
            await EndCombat();
            return;
        }

        SwitchToEnemyTurn();

        await StartTurn();
    }

    private async Task StartEnemyTurn()
    {
        IsPlayPhase = false;

        // Enemy turn start and enemy actions.

        if (CheckCombatEnd())
        {
            await EndCombat();
            return;
        }

        await EndEnemyTurn();
    }

    private async Task EndEnemyTurn()
    {
        // Enemy turn end.

        if (CheckCombatEnd())
        {
            await EndCombat();
            return;
        }

        SwitchToPlayerTurn();

        await StartTurn();
    }

    private void SwitchToPlayerTurn()
    {
        // Current side = player.
    }

    private void SwitchToEnemyTurn()
    {
        // Current side = enemy.
    }

    private bool IsPlayerTurn()
    {
        // Check current side.
        return true;
    }

    private bool CheckCombatEnd()
    {
        // Check win or loss.
        return false;
    }

    private Task EndCombat()
    {
        IsInProgress = false;
        IsPlayPhase = false;

        // Combat end.

        Reset();

        return Task.CompletedTask;
    }

    public void Pause()
    {
        IsPaused = true;
    }

    public void Unpause()
    {
        IsPaused = false;
    }

    private async Task WaitIfPaused()
    {
        while (IsPaused && IsInProgress)
        {
            await Task.Delay(16);
        }
    }

    public void Reset()
    {
        _state = null;
        IsInProgress = false;
        IsPlayPhase = false;
        IsPaused = false;
    }
}
