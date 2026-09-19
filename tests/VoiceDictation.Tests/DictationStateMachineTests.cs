using VoiceDictation.Core.Models;
using VoiceDictation.Core.State;
using Xunit;

namespace VoiceDictation.Tests;

public sealed class DictationStateMachineTests
{
    [Fact]
    public void InitialState_ShouldBeIdle()
    {
        var fsm = new DictationStateMachine();
        Assert.Equal(DictationState.Idle, fsm.CurrentState);
        Assert.Null(fsm.LastError);
    }

    [Fact]
    public void ValidTransitions_ShouldSucceedSequentially()
    {
        var fsm = new DictationStateMachine();
        var recordedEvents = new List<DictationState>();

        fsm.StateChanged += (_, e) => recordedEvents.Add(e.NewState);

        Assert.True(fsm.TryTransition(DictationState.Recording));
        Assert.Equal(DictationState.Recording, fsm.CurrentState);

        Assert.True(fsm.TryTransition(DictationState.Transcribing));
        Assert.Equal(DictationState.Transcribing, fsm.CurrentState);

        Assert.True(fsm.TryTransition(DictationState.Injecting));
        Assert.Equal(DictationState.Injecting, fsm.CurrentState);

        Assert.True(fsm.TryTransition(DictationState.Idle));
        Assert.Equal(DictationState.Idle, fsm.CurrentState);

        Assert.Equal(4, recordedEvents.Count);
        Assert.Equal(DictationState.Recording, recordedEvents[0]);
        Assert.Equal(DictationState.Transcribing, recordedEvents[1]);
        Assert.Equal(DictationState.Injecting, recordedEvents[2]);
        Assert.Equal(DictationState.Idle, recordedEvents[3]);
    }

    [Fact]
    public void InvalidTransition_FromIdleToInjecting_ShouldBeRejected()
    {
        var fsm = new DictationStateMachine();
        Assert.False(fsm.TryTransition(DictationState.Injecting));
        Assert.Equal(DictationState.Idle, fsm.CurrentState);
    }

    [Fact]
    public void FaultTransition_ShouldRecordErrorMessageAndAllowReset()
    {
        var fsm = new DictationStateMachine();
        fsm.TryTransition(DictationState.Recording);

        var faulted = fsm.TryTransition(DictationState.Faulted, "Microphone disconnected");
        Assert.True(faulted);
        Assert.Equal(DictationState.Faulted, fsm.CurrentState);
        Assert.Equal("Microphone disconnected", fsm.LastError);

        fsm.Reset();
        Assert.Equal(DictationState.Idle, fsm.CurrentState);
        Assert.Null(fsm.LastError);
    }
}
