import { configStore } from "../store/config-store.ts";
import { el } from "./dom.ts";
import { seriesStartsAtZero, setSeriesStartsAtZero } from "./series-intro-start-rule.ts";

/** The normal dashboard Save button persists the choice; scanning remains explicit. */
export function seriesIntroStart(seriesId: string, signal: AbortSignal): HTMLElement {
    const input = el("input", { type: "checkbox", id: "series-intro-start-zero" });
    const label = el("label", { className: "checkbox-label", for: input.id }, input,
        el("span", {}, "Start detected intros at zero for this series"));
    const container = el("div", { className: "checkbox-container" }, label,
        el("p", { className: "field-description" },
            "Applies to all seasons. Keeps the detected intro end after normal adjustments. Save settings, then Scan Series to update existing automatic segments."));

    const refresh = () => {
        input.disabled = !configStore.isLoaded();
        if (!input.disabled) {
            input.checked = seriesStartsAtZero(configStore.get("IntroStartAtZeroSeriesIds"), seriesId);
        }
    };
    configStore.subscribe("loaded", refresh, { signal });
    configStore.subscribe("changed", ({ field }) => {
        if (field === "IntroStartAtZeroSeriesIds") refresh();
    }, { signal });
    input.addEventListener("change", () => {
        configStore.set("IntroStartAtZeroSeriesIds",
            setSeriesStartsAtZero(configStore.get("IntroStartAtZeroSeriesIds"), seriesId, input.checked));
    }, { signal });
    refresh();
    return container;
}
