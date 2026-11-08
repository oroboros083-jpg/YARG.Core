using NUnit.Framework;
using YARG.Core.Chart;
using YARG.Core.Engine.Drums;
using YARG.Core.Engine.Drums.Engines;
using YARG.Core.Game;

namespace YARG.Core.UnitTests.Engine;

public class FlyTakeTests : EngineTester
{
    // Notes at 1.0 s red, 1.5 s kick, 2.0 s yellow, 2.5 s blue, 3.0 s green.
    private const string CHART = """
        [Song]
        {
          Resolution = 192
        }
        [SyncTrack]
        {
          0 = TS 4
          0 = B 120000
        }
        [ExpertDrums]
        {
          384 = N 1 0
          576 = N 0 0
          768 = N 2 0
          960 = N 3 0
          1152 = N 4 0
        }
        """;

    // Red, kick and blue on time, yellow 0.3 s late, green never played.
    private const string TAKE = """
        [
          {"t": 1.0, "pad": "red"},
          {"t": 1.5, "pad": "kick"},
          {"t": 2.3, "pad": "yellow"},
          {"t": 2.5, "pad": "blue"}
        ]
        """;

    [Test]
    public void BotWithoutTake_StillPlaysPerfectly()
    {
        var stats = Play(null);

        Assert.That((stats.NotesHit, stats.Overhits), Is.EqualTo((5, 0)));
    }

    [Test]
    public void FlyTake_IsScoredLikeAPlayer()
    {
        var stats = Play(FlyTake.Parse(TAKE));

        Assert.That((stats.NotesHit, stats.NotesMissed, stats.Overhits), Is.EqualTo((3, 2, 1)));
    }

    [Test]
    public void EmptyTake_MissesEverything()
    {
        var stats = Play(FlyTake.Empty);

        Assert.That((stats.NotesHit, stats.NotesMissed), Is.EqualTo((0, 5)));
    }

    [Test]
    public void UnknownPad_IsRejected()
    {
        Assert.Throws<InvalidDataException>(() => FlyTake.Parse("""[{"t": 1.0, "pad": "cowbell"}]"""));
    }

    private static DrumsStats Play(FlyTake? take)
    {
        var chart = SongChart.FromDotChart(in ParseSettings.Default_Chart, CHART);
        var notes = chart.FourLaneDrums.GetDifficulty(Difficulty.Expert);
        var parameters = EnginePreset.Default.Drums.Create(StarMultiplierThresholds,
            SoloBonusStarMultiplierThresholds, DrumsEngineParameters.DrumMode.NonProFourLane);
        var engine = new YargDrumsEngine(notes, chart.SyncTrack, parameters, true, false) { FlyTake = take };
        engine.Reset();

        for (double t = 0; t <= 4.0; t += 1.0 / 60)
        {
            engine.Update(t);
        }

        return engine.EngineStats;
    }
}
