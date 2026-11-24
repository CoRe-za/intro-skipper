/** Jellyfin can serialize the same series id with or without GUID separators. */
function normalizedId(value: string): string {
    return value.trim().replaceAll("-", "").toLowerCase();
}

export function seriesStartsAtZero(ids: readonly string[], seriesId: string): boolean {
    const id = normalizedId(seriesId);
    return /^[0-9a-f]{32}$/.test(id) && id !== "0".repeat(32)
        && ids.some((value) => normalizedId(value) === id);
}

/** Change one series without removing another series' choice or adding duplicates. */
export function setSeriesStartsAtZero(ids: readonly string[], seriesId: string, enabled: boolean): string[] {
    const id = normalizedId(seriesId);
    const remaining = ids.filter((value) => normalizedId(value) !== id);
    if (!enabled || !/^[0-9a-f]{32}$/.test(id) || id === "0".repeat(32)) return remaining;
    return [...remaining, id];
}
