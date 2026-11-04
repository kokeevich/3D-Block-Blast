using NUnit.Framework;

public class SessionAndTutorialTests
{
    [Test] public void TutorialOnlyAdvancesOnTheExpectedAction()
    {
        var guide=new TutorialProgress(); guide.Start();
        Assert.IsTrue(guide.IsIntro); Assert.IsFalse(guide.CanSelect); Assert.IsFalse(guide.CanPlace);
        Assert.IsFalse(guide.Signal(TutorialProgress.Action.PlaceShape)); Assert.IsTrue(guide.IsIntro);
        guide.Begin(); Assert.AreEqual(1,guide.Number); Assert.IsFalse(guide.CanSelect);
        Assert.IsFalse(guide.Signal(TutorialProgress.Action.ChangeDepth)); Assert.AreEqual(1,guide.Number);
        guide.Signal(TutorialProgress.Action.RotateBoard); Assert.IsTrue(guide.CanSelect); Assert.IsFalse(guide.CanManipulate);
        guide.Signal(TutorialProgress.Action.SelectShape); Assert.IsTrue(guide.CanManipulate); Assert.IsFalse(guide.CanPlace);
        guide.Signal(TutorialProgress.Action.MoveShape); Assert.AreEqual(4,guide.Number);
        guide.Signal(TutorialProgress.Action.ChangeDepth); Assert.IsTrue(guide.CanPlace); Assert.AreEqual(5,guide.Number);
        Assert.IsTrue(guide.Signal(TutorialProgress.Action.PlaceShape)); Assert.IsFalse(guide.Active);
        Assert.AreEqual(TutorialProgress.Step.Complete,guide.Current);
        Assert.IsFalse(guide.Signal(TutorialProgress.Action.PlaceShape));
    }
    [Test] public void SkipAndReplayDoNotLeaveTutorialInputLocked()
    {
        var guide=new TutorialProgress(); guide.Start(); guide.Skip();
        Assert.IsTrue(guide.CanSelect && guide.CanManipulate && guide.CanPlace); Assert.IsFalse(guide.Active);
        guide.Start(); Assert.IsTrue(guide.IsIntro); guide.Begin(); Assert.AreEqual(1,guide.Number);
    }
    [Test] public void PauseAndResumePreserveTheCurrentParty()
    {
        var flow=new SessionFlow(); Assert.IsTrue(flow.CanAcceptInput);
        Assert.IsTrue(flow.TryPause()); Assert.IsTrue(flow.Paused); Assert.IsFalse(flow.CanAcceptInput);
        Assert.IsFalse(flow.TryPause()); flow.Resume(); Assert.IsTrue(flow.CanAcceptInput);
    }
    [Test] public void GameOverCannotBeUnpausedBackIntoGameplay()
    {
        var flow=new SessionFlow(); flow.TryPause(); flow.EndGame();
        Assert.IsFalse(flow.Paused); Assert.IsTrue(flow.GameOver); Assert.IsFalse(flow.CanAcceptInput);
        Assert.IsFalse(flow.TryPause()); flow.Resume(); Assert.IsFalse(flow.CanAcceptInput);
    }
    [Test] public void SceneTransitionsBlockDuplicateNavigationAndReleasePause()
    {
        var flow=new SessionFlow(); flow.TryPause();
        Assert.IsTrue(flow.TryBeginTransition()); Assert.IsFalse(flow.Paused);
        Assert.IsFalse(flow.CanAcceptInput); Assert.IsFalse(flow.TryBeginTransition()); Assert.IsFalse(flow.TryPause());
        flow.Resume(); Assert.IsFalse(flow.CanAcceptInput);
    }
    [Test] public void CompletingAGameStillAllowsReturnToMenu()
    {
        var flow=new SessionFlow(); flow.EndGame(); Assert.IsTrue(flow.TryBeginTransition()); Assert.IsFalse(flow.CanAcceptInput);
    }
}
