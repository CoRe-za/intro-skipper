// SPDX-FileCopyrightText: 2026 Intro Skipper contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading;
using System.Threading.Tasks;
using IntroSkipper.Analyzers;
using IntroSkipper.Configuration;
using IntroSkipper.Data;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IntroSkipper.Tests;

public sealed class TestSeriesIntroStart
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChapterAnalysis_ValidatesTheDetectedLengthAndRetainsAutomaticProvenance(bool selected)
    {
        using var temp = new TempSegmentDb();
        var series = Guid.NewGuid();
        var config = Configuration(selected ? series : Guid.NewGuid());
        config.MinimumIntroDuration = 20;
        config.MaximumIntroDuration = 40;
        QueuedEpisode[] episodes =
        [
            new()
            {
                EpisodeId = Guid.NewGuid(), SeriesId = series, Duration = 300,
                Chapters = [new ChapterInfo { Name = "Intro", StartPositionTicks = 60 * TimeSpan.TicksPerSecond },
                            new ChapterInfo { Name = "Story", StartPositionTicks = 90 * TimeSpan.TicksPerSecond }],
            },
        ];
        var analyzer = new ChapterAnalyzer(NullLogger<ChapterAnalyzer>.Instance, null!, temp.Database, config);

        await analyzer.AnalyzeMediaFiles(episodes, AnalysisMode.Introduction, CancellationToken.None);

        var row = Assert.Single(await temp.Database.GetSegmentsAsync(episodes[0].EpisodeId));
        Assert.Equal(selected ? 0 : 60 * TimeSpan.TicksPerSecond, row.StartTicks);
        Assert.Equal(90 * TimeSpan.TicksPerSecond, row.EndTicks);
        Assert.Equal(SegmentSource.Chapter, row.Source);
    }

    [Theory]
    [InlineData(2773.143, 2830.096, 6480)]
    [InlineData(787.620, 844.401, 3480)]
    [InlineData(2592.325, 2640.440, 6900)]
    [InlineData(795.794, 861.201, 4020)]
    [InlineData(2714.815, 2780.599, 6480)]
    [InlineData(1676.542, 1745.440, 4500)]
    [InlineData(2448.863, 2516.959, 8220)]
    public async Task SelectedSeries_StartsAtZeroWithoutChangingEitherSeriesEnd(double start, double end, double duration)
    {
        var selected = Guid.NewGuid();
        var config = Configuration(selected);
        var episode = new QueuedEpisode { EpisodeId = Guid.NewGuid(), SeriesId = selected, Duration = duration };
        var helper = new TimeAdjustmentHelper(NullLogger.Instance, config, AnalysisMode.Introduction, null!);

        var result = await helper.AdjustIntroTimesAsync(episode, new Segment(episode.EpisodeId) { Start = start, End = end });

        Assert.Equal(0, result.Start);
        Assert.Equal(BitConverter.DoubleToInt64Bits(end), BitConverter.DoubleToInt64Bits(result.End));
        Assert.True(result.Valid);

        episode.SeriesId = Guid.NewGuid();
        var untouched = await helper.AdjustIntroTimesAsync(episode, new Segment(episode.EpisodeId) { Start = start, End = end });
        Assert.Equal(start, untouched.Start);
        Assert.Equal(end, untouched.End);
    }

    [Theory]
    [InlineData(AnalysisMode.Credits)]
    [InlineData(AnalysisMode.Recap)]
    [InlineData(AnalysisMode.Preview)]
    [InlineData(AnalysisMode.Commercial)]
    public async Task SelectedSeries_DoesNotChangeOtherModes(AnalysisMode mode)
    {
        var series = Guid.NewGuid();
        var helper = new TimeAdjustmentHelper(NullLogger.Instance, Configuration(series), mode, null!);
        var episode = new QueuedEpisode { EpisodeId = Guid.NewGuid(), SeriesId = series, Duration = 300 };

        var result = await helper.AdjustIntroTimesAsync(episode, new Segment(episode.EpisodeId) { Start = 60, End = 90 });

        Assert.Equal(60, result.Start);
        Assert.Equal(90, result.End);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 60)]
    public async Task SelectedSeries_DoesNotRescueInvalidDetections(double start, double end)
    {
        var series = Guid.NewGuid();
        var helper = new TimeAdjustmentHelper(NullLogger.Instance, Configuration(series), AnalysisMode.Introduction, null!);
        var episode = new QueuedEpisode { EpisodeId = Guid.NewGuid(), SeriesId = series, Duration = 300 };

        var result = await helper.AdjustIntroTimesAsync(episode, new Segment(episode.EpisodeId) { Start = start, End = end });

        Assert.False(result.Valid);
    }

    [Fact]
    public async Task SelectedSeries_PreservesNormalEndRefinement()
    {
        var series = Guid.NewGuid();
        var config = Configuration(series);
        config.AdjustIntroBasedOnSilence = true;
        config.SnapToKeyframe = true;
        config.AdjustWindowInward = 2;
        config.AdjustWindowOutward = 2;
        var service = new StubFFmpegService
        {
            Silence = (_, _, _) => [new TimeRange(88.9, 89.5)],
            KeyFrames = (_, _, _) => [89.1, 90.2],
        };
        var episode = new QueuedEpisode { EpisodeId = Guid.NewGuid(), SeriesId = series, Duration = 300 };
        var original = new Segment(episode.EpisodeId) { Start = 60, End = 90 };
        var baseline = await new TimeAdjustmentHelper(NullLogger.Instance, config, AnalysisMode.Introduction, service)
            .AdjustIntroTimesAsync(new QueuedEpisode { EpisodeId = episode.EpisodeId, Duration = 300 }, original);

        var result = await new TimeAdjustmentHelper(NullLogger.Instance, config, AnalysisMode.Introduction, service)
            .AdjustIntroTimesAsync(episode, original);

        Assert.Equal(0, result.Start);
        Assert.Equal(baseline.End, result.End);
        Assert.Equal(89.1, result.End);
    }

    [Fact]
    public async Task SelectedSeries_RuleWinsOverStartOffsetButKeepsEndOffset()
    {
        var series = Guid.NewGuid();
        var config = Configuration(series);
        config.IntroStartOffset = 3;
        config.IntroEndOffset = 2;
        var episode = new QueuedEpisode { EpisodeId = Guid.NewGuid(), SeriesId = series, Duration = 300 };

        var result = await new TimeAdjustmentHelper(NullLogger.Instance, config, AnalysisMode.Introduction, null!)
            .AdjustIntroTimesAsync(episode, new Segment(episode.EpisodeId) { Start = 60, End = 90 }, chapterSourced: true);

        Assert.Equal(0, result.Start);
        Assert.Equal(88, result.End);
    }

    private static PluginConfiguration Configuration(Guid series) => new()
    {
        IntroStartAtZeroSeriesIds = [series.ToString("N")],
        EndSnapThreshold = 0,
        AdjustIntroBasedOnChapters = false,
        AdjustIntroBasedOnSilence = false,
        SnapToKeyframe = false,
    };
}
