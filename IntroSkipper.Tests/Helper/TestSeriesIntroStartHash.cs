// SPDX-FileCopyrightText: 2026 Intro Skipper contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using IntroSkipper.Configuration;
using IntroSkipper.Data;
using IntroSkipper.Helper;
using IntroSkipper.Manager;
using Xunit;

namespace IntroSkipper.Tests;

public sealed class TestSeriesIntroStartHash
{
    [Fact]
    public void OnlySelectedSeriesIntroductionsChangeTheirAnalysisIdentity()
    {
        var selected = Guid.NewGuid();
        var other = Guid.NewGuid();
        var original = new PluginConfiguration();
        var config = new PluginConfiguration { IntroStartAtZeroSeriesIds = [selected.ToString("D")] };

        foreach (var mode in Enum.GetValues<AnalysisMode>())
        {
            var before = ConfigHasher.Analysis(original, mode, AnalyzerAction.Default, true, seriesId: selected);
            var after = ConfigHasher.Analysis(config, mode, AnalyzerAction.Default, true, seriesId: selected);
            Assert.Equal(mode != AnalysisMode.Introduction, before == after);
            Assert.Equal(
                ConfigHasher.Analysis(original, mode, AnalyzerAction.Default, true, seriesId: other),
                ConfigHasher.Analysis(config, mode, AnalyzerAction.Default, true, seriesId: other));
        }

        Assert.Equal(
            ConfigHasher.DetectionCache(original, CacheEntryType.Chromaprint, AnalysisMode.Introduction),
            ConfigHasher.DetectionCache(config, CacheEntryType.Chromaprint, AnalysisMode.Introduction));
    }

    [Fact]
    public void FormattingAndOtherSeriesChoicesDoNotInvalidateASelectedSeries()
    {
        var series = Guid.NewGuid();
        var original = new PluginConfiguration { IntroStartAtZeroSeriesIds = [series.ToString("D").ToUpperInvariant()] };
        var changed = new PluginConfiguration { IntroStartAtZeroSeriesIds = [Guid.NewGuid().ToString("N"), series.ToString("N"), series.ToString("D")] };

        Assert.True(original.StartsIntrosAtZero(series));
        Assert.False(original.StartsIntrosAtZero(Guid.Empty));
        Assert.Equal(
            ConfigHasher.Analysis(original, AnalysisMode.Introduction, AnalyzerAction.Default, true, seriesId: series),
            ConfigHasher.Analysis(changed, AnalysisMode.Introduction, AnalyzerAction.Default, true, seriesId: series));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TogglingTheSeriesChoiceReopensAnAutomaticAnalysis(bool previouslyEnabled)
    {
        using var temp = new TempSegmentDb();
        var series = Guid.NewGuid();
        var season = Guid.NewGuid();
        var id = Guid.NewGuid();
        var before = new PluginConfiguration { IntroStartAtZeroSeriesIds = previouslyEnabled ? [series.ToString()] : [] };
        var after = new PluginConfiguration { IntroStartAtZeroSeriesIds = previouslyEnabled ? [] : [series.ToString()] };
        var hash = ConfigHasher.Analysis(before, AnalysisMode.Introduction, AnalyzerAction.Default, true, seriesId: series);
        await temp.Database.MarkItemsAnalyzedAsync(AnalysisMode.Introduction, [id], hash);
        var snapshot = await temp.Database.GetSeasonQueueSnapshotAsync(season, [id]);
        var episode = new QueuedEpisode { EpisodeId = id, SeriesId = series, SeasonId = season };

        new QueueVerifier(after, [AnalysisMode.Introduction], snapshot, true, seriesId: series).Classify(episode);

        Assert.Equal(EpisodeState.NotAnalyzed, episode.GetAnalyzed(AnalysisMode.Introduction));
    }
}
