using VoiceDictation.Core.Models;

namespace VoiceDictation.Core.State;

public sealed class DictationStateMachine
{
    private readonly object _syncRoot = new();
    private DictationState _currentState = DictationState.Idle;
    private string? _lastError;

    public DictationState CurrentState
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentState;
            }
        }
    }

    public string? LastError
    {
        get
        {
            lock (_syncRoot)
            {
                return _lastError;
            }
        }
    }

    public event EventHandler<DictationStateChangedEventArgs>? StateChanged;

    public bool CanTransitionTo(DictationState nextState)
    {
        lock (_syncRoot)
        {
            return (_currentState, nextState) switch
            {
                (DictationState.Idle, DictationState.Recording) => true,
                (DictationState.Recording, DictationState.Transcribing) => true,
                (DictationState.Recording, DictationState.Idle) => true,
                (DictationState.Transcribing, DictationState.Injecting) => true,
                (DictationState.Transcribing, DictationState.Idle) => true,
                (DictationState.Injecting, DictationState.Idle) => true,
                (_, DictationState.Faulted) => true,
                (DictationState.Faulted, DictationState.Idle) => true,
                _ => false
            };
        }
    }

    public bool TryTransition(DictationState nextState, string? error = null)
    {
        DictationState previousState;
        lock (_syncRoot)
        {
            if (!CanTransitionTo(nextState))
            {
                return false;
            }

            previousState = _currentState;
            _currentState = nextState;
            _lastError = nextState == DictationState.Faulted ? error : null;
        }

        StateChanged?.Invoke(this, new DictationStateChangedEventArgs(previousState, nextState, error));
        return true;
    }

    public void Reset()
    {
        lock (_syncRoot)
        {
            var previous = _currentState;
            _currentState = DictationState.Idle;
            _lastError = null;
            StateChanged?.Invoke(this, new DictationStateChangedEventArgs(previous, DictationState.Idle, null));
        }
    }
}

public sealed class DictationStateChangedEventArgs : EventArgs
{
    public DictationState PreviousState { get; }
    public DictationState NewState { get; }
    public string? ErrorMessage { get; }

    public DictationStateChangedEventArgs(DictationState previousState, DictationState newState, string? errorMessage)
    {
        PreviousState = previousState;
        NewState = newState;
        ErrorMessage = errorMessage;
    }
}
