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

public class RunSnapshotTests
{
    private static RunSnapshot Valid() => new RunSnapshot { blocks=new RunSnapshot.Block[0],
        catalog=new[] { 0,1,2 },orientation=new[] { 0,1,2 },used=new[] { false,false,false },
        rotation=new[] { 0f,0f,0f,1f },distance=10,ortho=5 };
    [Test] public void AcceptsInitialAndCommittedPendingCheckpoints()
    {
        var s=Valid(); Assert.IsTrue(s.IsValid());
        s.consume=0; s.moves=1; s.score=3;
        Assert.IsTrue(s.IsValid()); s.used[0]=true; Assert.IsFalse(s.IsValid());
    }
    [Test] public void RejectsUnsupportedVersionAndInvalidGeometry()
    {
        var s=Valid(); s.version=2; Assert.IsFalse(s.IsValid());
        s=Valid(); s.catalog[0]=BalanceRules.Catalog.Count; Assert.IsFalse(s.IsValid());
        s=Valid(); s.orientation[0]=24; Assert.IsFalse(s.IsValid());
        s=Valid(); s.used=new[] { true,true,true }; Assert.IsFalse(s.IsValid());
    }
    [Test] public void RejectsDuplicateAndOutOfBoundsBlocks()
    {
        var s=Valid(); s.peak=2;
        s.blocks=new[] { new RunSnapshot.Block { x=1 },new RunSnapshot.Block { x=1 } };
        Assert.IsFalse(s.IsValid()); s.blocks[1].x=8; Assert.IsFalse(s.IsValid());
        s.blocks[1].x=2; Assert.IsTrue(s.IsValid());
    }
    [Test] public void RejectsInvalidStatisticsCameraAndSelection()
    {
        var s=Valid(); s.moves=1; Assert.IsFalse(s.IsValid());
        s=Valid(); s.rotation[0]=float.NaN; Assert.IsFalse(s.IsValid());
        s=Valid(); s.distance=float.PositiveInfinity; Assert.IsFalse(s.IsValid());
        s=Valid(); s.selected=1; s.anchorZ=8; Assert.IsFalse(s.IsValid());
        s.anchorZ=7; Assert.IsTrue(s.IsValid());
        s.used[1]=true; Assert.IsFalse(s.IsValid());
    }
    [Test] public void RestoredStatisticsContinueAccumulating()
    {
        var stats=new RunStatistics(); stats.Restore(12,3,100);
        stats.RecordPlacement(new BoardModel().GetCompletedLines(),5);
        Assert.AreEqual(13,stats.Moves); Assert.AreEqual(3,stats.Lines); Assert.AreEqual(100,stats.PeakOccupied);
    }
}

public class BoardRevisionTests
{
    [Test] public void EveryOccupancyMutationInvalidatesPreviewCaches()
    {
        var board=new BoardModel(); int revision=board.Revision;
        board.SetOccupied(0,0,0,true); Assert.Greater(board.Revision,revision); revision=board.Revision;
        board.SetOccupied(0,0,0,true); Assert.AreEqual(revision,board.Revision);
        board.SetOccupied(-1,0,0,true); Assert.AreEqual(revision,board.Revision);
        board.ClearCell(0,0,0); Assert.Greater(board.Revision,revision); revision=board.Revision;
        Assert.IsTrue(board.TryPlaceShape(BoardModel.Cell.zero,new[] { BoardModel.Cell.zero }));
        Assert.Greater(board.Revision,revision); revision=board.Revision;
        board.ClearPlane(new BoardModel.Plane(BoardModel.PlaneAxis.XY,0)); Assert.Greater(board.Revision,revision);
        for (int x=0;x<8;x++) board.SetOccupied(x,0,0,true);
        revision=board.Revision; board.ClearLines(board.GetCompletedLines()); Assert.Greater(board.Revision,revision);
        Assert.AreEqual(512,board.CountFreeCells());
    }
}

public class AdExposureRulesTests
{
    [Test] public void FirstFinishedGameDoesNotShowAnInterstitial()
    {
        var rules=new AdExposureRules(); Assert.IsFalse(rules.CanShow(1000));
        rules.RecordLoss(); Assert.IsFalse(rules.CanShow(1000));
        rules.RecordLoss(); Assert.IsTrue(rules.CanShow(1000));
    }
    [Test] public void CooldownStartsOnlyWhenAnAdIsShown()
    {
        var rules=new AdExposureRules(2);
        Assert.IsTrue(rules.CanShow(1000)); Assert.IsTrue(rules.CanShow(1001));
        rules.RecordShown(1001);
        Assert.IsFalse(rules.CanShow(1120)); Assert.IsTrue(rules.CanShow(1121));
    }
    [Test] public void RelaunchAndClockRollbackDoNotBypassCooldown()
    {
        var saved=new AdExposureRules(5); saved.RecordShown(1000);
        var restored=new AdExposureRules(saved.Losses,saved.LastShown);
        Assert.IsFalse(restored.CanShow(900)); Assert.IsFalse(restored.CanShow(1119));
        Assert.IsTrue(restored.CanShow(1120));
    }
    [Test] public void CorruptNegativeCountersAreClamped()
    {
        var rules=new AdExposureRules(-1,-10); Assert.AreEqual(0,rules.Losses); Assert.AreEqual(0,rules.LastShown);
        Assert.IsFalse(rules.CanShow(1000));
    }
}
