import assert from "node:assert/strict";
import test from "node:test";
import { seriesStartsAtZero, setSeriesStartsAtZero } from "../src/components/series-intro-start-rule.ts";

const first = "11111111-2222-3333-4444-555555555555";
const second = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

test("selecting and deselecting a series preserves another series", () => {
    const enabled = setSeriesStartsAtZero([second], first, true);
    assert.equal(seriesStartsAtZero(enabled, first), true);
    assert.equal(seriesStartsAtZero(enabled, second), true);
    const disabled = setSeriesStartsAtZero(enabled, first, false);
    assert.deepEqual(disabled, [second]);
});

test("GUID formatting is normalized and repeated selections do not duplicate", () => {
    const ids = setSeriesStartsAtZero([second.toUpperCase()], second.replaceAll("-", ""), true);
    assert.equal(ids.length, 1);
    assert.equal(seriesStartsAtZero(ids, second), true);
});

test("empty, malformed and unknown series never enable the rule", () => {
    for (const id of ["", "all", "Series title", "0".repeat(32)]) {
        assert.deepEqual(setSeriesStartsAtZero([], id, true), []);
        assert.equal(seriesStartsAtZero([id], id), false);
    }
    assert.equal(seriesStartsAtZero([first], second), false);
});
