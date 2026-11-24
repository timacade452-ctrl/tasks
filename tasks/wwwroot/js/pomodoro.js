(function () {
    "use strict";

    const STORAGE_KEY = "studyflow.timer.v3";
    const CIRCUMFERENCE = 2 * Math.PI * 132;

    const meta = (name) => document.querySelector(`meta[name="${name}"]`)?.content ?? "";
    const metaNumber = (name, fallback) => {
        const value = Number.parseInt(meta(name), 10);
        return Number.isFinite(value) && value > 0 ? value : fallback;
    };

    const serverDefaults = {
        focusMinutes: metaNumber("timer-focus", 25),
        shortBreakMinutes: metaNumber("timer-short", 5),
        longBreakMinutes: metaNumber("timer-long", 15),
        longBreakInterval: metaNumber("timer-interval", 4),
        soundEnabled: meta("timer-sound") === "true",
        autoStartBreaks: meta("timer-autostart") === "true"
    };

    const fingerprint = [
        serverDefaults.focusMinutes,
        serverDefaults.shortBreakMinutes,
        serverDefaults.longBreakMinutes,
        serverDefaults.longBreakInterval
    ].join("-");

    function freshState() {
        return {
            v: 3,
            phase: "focus",
            status: "idle",
            focusMinutes: serverDefaults.focusMinutes,
            shortBreakMinutes: serverDefaults.shortBreakMinutes,
            longBreakMinutes: serverDefaults.longBreakMinutes,
            longBreakInterval: serverDefaults.longBreakInterval,
            fingerprint,
            durationMs: serverDefaults.focusMinutes * 60000,
            remainingMs: serverDefaults.focusMinutes * 60000,
            endAt: null,
            startedAt: null,
            completedInCycle: 0,
            courseId: "",
            taskId: "",
            soundEnabled: serverDefaults.soundEnabled,
            autoStartBreaks: serverDefaults.autoStartBreaks,
            expired: false
        };
    }

    function phaseMinutes(phase, state) {
        if (phase === "short") return state.shortBreakMinutes;
        if (phase === "long") return state.longBreakMinutes;
        return state.focusMinutes;
    }

    function phaseLabel(phase) {
        if (phase === "short") return "Short break";
        if (phase === "long") return "Long break";
        return "Focus session";
    }

    function phaseShortLabel(phase) {
        return phase === "focus" ? "Focus" : "Break";
    }

    function loadState() {
        try {
            const raw = window.localStorage.getItem(STORAGE_KEY);
            if (!raw) return freshState();
            const saved = JSON.parse(raw);
            if (!saved || saved.v !== 3) return freshState();

            const state = Object.assign(freshState(), saved);

            // Profile settings are the baseline. If they changed, adopt the new
            // durations; inline customisations keep the old fingerprint and survive.
            if (state.fingerprint !== fingerprint) {
                return Object.assign(freshState(), {
                    courseId: state.courseId || "",
                    taskId: state.taskId || ""
                });
            }

            state.soundEnabled = serverDefaults.soundEnabled;
            state.autoStartBreaks = serverDefaults.autoStartBreaks;

            if (state.status === "running") {
                const left = Number(state.endAt) - Date.now();
                state.remainingMs = Math.max(0, left);
                if (left <= 0) {
                    state.expired = true;
                    state.remainingMs = 0;
                }
            }

            return state;
        } catch (error) {
            return freshState();
        }
    }

    let state = loadState();
    let ticker = null;
    let baseTitle = document.title;
    let audioContext = null;
    let initialized = false;

    const page = document.getElementById("pomodoroPage");
    const isTimerPage = Boolean(page);
    const courseSelect = document.getElementById("timerCourse");
    const taskSelect = document.getElementById("timerTask");

    function saveState() {
        try {
            window.localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
        } catch (error) {
            /* Storage can be unavailable in private browsing; the timer still works for this page. */
        }
    }

    function formatClock(milliseconds) {
        const total = Math.max(0, Math.ceil(milliseconds / 1000));
        const minutes = Math.floor(total / 60);
        const seconds = total % 60;
        return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
    }

    function formatMinutes(minutes) {
        if (minutes < 60) return `${minutes} min`;
        const hours = Math.floor(minutes / 60);
        const rest = minutes % 60;
        return rest === 0 ? `${hours} hr` : `${hours}h ${rest}m`;
    }

    function getContext() {
        const courseOption = courseSelect?.selectedOptions?.[0];
        const taskOption = taskSelect?.selectedOptions?.[0];
        const courseName = courseOption?.dataset.name || "";
        const taskName = taskOption?.dataset.title || "";
        if (taskName && courseName) return `${courseName} · ${taskName}`;
        return taskName || courseName || "General study";
    }

    function ringElements() {
        return Array.from(document.querySelectorAll("[data-timer-ring]"));
    }

    function updateCycleDots() {
        const containers = document.querySelectorAll("[data-cycle-dots]");
        const interval = Math.max(2, Number(state.longBreakInterval) || 4);
        const completed = Math.min(state.completedInCycle, interval);

        containers.forEach((container) => {
            if (Number(container.dataset.dotCount) !== interval) {
                container.innerHTML = "";
                container.dataset.dotCount = String(interval);
                for (let index = 0; index < interval; index++) {
                    const dot = document.createElement("span");
                    dot.className = "h-2 w-2 rounded-full bg-slate-200 transition-colors";
                    container.appendChild(dot);
                }
            }
            Array.from(container.children).forEach((dot, index) => {
                dot.classList.toggle("bg-brand-600", index < completed);
                dot.classList.toggle("bg-slate-200", index >= completed);
            });
        });
    }

    function render() {
        const remaining = state.status === "running"
            ? Math.max(0, state.endAt - Date.now())
            : Math.max(0, state.remainingMs);
        const duration = Math.max(1, state.durationMs);
        const fractionRemaining = Math.min(1, remaining / duration);
        const offset = CIRCUMFERENCE * (1 - fractionRemaining);
        const clock = formatClock(remaining);
        const label = phaseLabel(state.phase);
        const statusText = state.status === "running"
            ? (state.phase === "focus" ? "Focusing" : "On a break")
            : state.status === "paused" ? "Paused" : "Ready";

        document.querySelectorAll("[data-timer-display]").forEach((el) => { el.textContent = clock; });
        document.querySelectorAll("[data-timer-phase]").forEach((el) => { el.textContent = label; });
        document.querySelectorAll("[data-timer-status]").forEach((el) => { el.textContent = statusText; });
        document.querySelectorAll("[data-timer-context]").forEach((el) => { el.textContent = getContext(); });

        ringElements().forEach((ring) => {
            ring.setAttribute("stroke-dasharray", String(CIRCUMFERENCE));
            ring.setAttribute("stroke-dashoffset", String(offset));
            ring.setAttribute("stroke", state.phase === "focus" ? "#4f46e5" : "#059669");
        });

        document.querySelectorAll("[data-start-label]").forEach((el) => {
            el.textContent = state.status === "running"
                ? "Pause"
                : state.status === "paused" ? "Resume" : state.phase === "focus" ? "Start focus" : "Start break";
        });

        document.querySelectorAll("[data-icon-play]").forEach((el) => {
            el.classList.toggle("hidden", state.status === "running");
        });
        document.querySelectorAll("[data-icon-pause]").forEach((el) => {
            el.classList.toggle("hidden", state.status !== "running");
        });

        document.querySelectorAll("[data-phase-badge]").forEach((badge) => {
            badge.textContent = label;
            badge.classList.toggle("bg-brand-50", state.phase === "focus");
            badge.classList.toggle("text-brand-700", state.phase === "focus");
            badge.classList.toggle("ring-brand-100", state.phase === "focus");
            badge.classList.toggle("bg-emerald-50", state.phase !== "focus");
            badge.classList.toggle("text-emerald-700", state.phase !== "focus");
            badge.classList.toggle("ring-emerald-100", state.phase !== "focus");
        });

        updateCycleDots();

        // Duration controls mirror state and are locked while a session is live.
        const focusInput = document.querySelector('[data-duration="focus"]');
        const shortInput = document.querySelector('[data-duration="short"]');
        const longInput = document.querySelector('[data-duration="long"]');
        const intervalInput = document.querySelector('[data-duration="interval"]');
        if (focusInput && document.activeElement !== focusInput) focusInput.value = state.focusMinutes;
        if (shortInput && document.activeElement !== shortInput) shortInput.value = state.shortBreakMinutes;
        if (longInput && document.activeElement !== longInput) longInput.value = state.longBreakMinutes;
        if (intervalInput && document.activeElement !== intervalInput) intervalInput.value = state.longBreakInterval;

        const locked = state.status !== "idle";
        [focusInput, shortInput, longInput, intervalInput].forEach((input) => {
            if (input) input.disabled = locked;
        });
        if (courseSelect) courseSelect.disabled = locked;
        if (taskSelect) taskSelect.disabled = locked;

        // Header mini timer
        const mini = document.getElementById("miniTimer");
        if (mini) {
            const active = state.status === "running" || state.status === "paused";
            mini.classList.toggle("hidden", !active);
            mini.classList.toggle("inline-flex", active);
            const miniTime = document.getElementById("miniTimerTime");
            const miniPhase = document.getElementById("miniTimerPhase");
            const miniDot = document.getElementById("miniTimerDot");
            if (miniTime) miniTime.textContent = clock;
            if (miniPhase) miniPhase.textContent = state.status === "paused" ? "Paused" : phaseShortLabel(state.phase);
            if (miniDot) miniDot.style.backgroundColor = state.status === "paused"
                ? "#f59e0b"
                : state.phase === "focus" ? "#4f46e5" : "#059669";
        }

        if (isTimerPage) {
            document.title = state.status === "running" || state.status === "paused"
                ? `${clock} · ${phaseShortLabel(state.phase)} — StudyFlow`
                : baseTitle;
        }
    }

    function stopTicker() {
        if (ticker) {
            window.clearInterval(ticker);
            ticker = null;
        }
    }

    function ensureTicker() {
        if (state.status !== "running") {
            stopTicker();
            return;
        }
        if (ticker) return;
        ticker = window.setInterval(() => {
            if (state.status !== "running") {
                stopTicker();
                return;
            }
            const left = state.endAt - Date.now();
            state.remainingMs = Math.max(0, left);
            if (left <= 0) {
                completePhase(false, state.endAt || Date.now());
                return;
            }
            render();
        }, 250);
    }

    function primeAudio() {
        if (!state.soundEnabled) return;
        try {
            if (!audioContext) {
                const Ctor = window.AudioContext || window.webkitAudioContext;
                if (!Ctor) return;
                audioContext = new Ctor();
            }
            if (audioContext.state === "suspended") audioContext.resume();
        } catch (error) {
            /* Sound is an enhancement; never break the timer for it. */
        }
    }

    function playChime() {
        if (!state.soundEnabled) return;
        primeAudio();
        if (!audioContext) return;

        try {
            const now = audioContext.currentTime;
            [0, 0.18, 0.36].forEach((offset, index) => {
                const oscillator = audioContext.createOscillator();
                const gain = audioContext.createGain();
                oscillator.type = "sine";
                oscillator.frequency.setValueAtTime(index === 2 ? 1046.5 : 783.99, now + offset);
                gain.gain.setValueAtTime(0.0001, now + offset);
                gain.gain.exponentialRampToValueAtTime(0.16, now + offset + 0.02);
                gain.gain.exponentialRampToValueAtTime(0.0001, now + offset + 0.42);
                oscillator.connect(gain).connect(audioContext.destination);
                oscillator.start(now + offset);
                oscillator.stop(now + offset + 0.45);
            });
        } catch (error) {
            /* Ignore audio failures. */
        }
    }

    function setPhase(phase, status) {
        state.phase = phase;
        state.durationMs = phaseMinutes(phase, state) * 60000;
        state.remainingMs = state.durationMs;
        state.endAt = null;
        state.startedAt = null;
        state.status = status;
    }

    function advanceAfterFocus() {
        state.completedInCycle = Math.min(state.completedInCycle + 1, state.longBreakInterval);
        const useLongBreak = state.completedInCycle >= state.longBreakInterval;
        return useLongBreak ? "long" : "short";
    }

    function completePhase(catchUp, completedAtMs) {
        if (state.status === "running" && !catchUp && state.endAt && Date.now() < state.endAt - 1000) {
            return;
        }

        stopTicker();
        const finished = state.phase;
        const completedAt = completedAtMs || state.endAt || Date.now();

        if (finished === "focus") {
            const minutes = state.focusMinutes;
            const nextPhase = advanceAfterFocus();

            state.totalRecorded = (state.totalRecorded || 0) + 1;
            state.status = "idle";
            state.durationMs = phaseMinutes(nextPhase, state) * 60000;
            state.remainingMs = state.durationMs;
            state.endAt = null;
            state.startedAt = null;
            state.phase = nextPhase;

            if (!catchUp && state.autoStartBreaks) {
                state.status = "running";
                state.endAt = Date.now() + state.remainingMs;
            }

            saveState();
            recordFocus(minutes, completedAt);
            playChime();
            window.SFToast?.(
                nextPhase === "long"
                    ? `Focus session complete — long break unlocked after ${state.longBreakInterval} sessions.`
                    : `Focus session complete — ${minutes} minutes added to your study time.`,
                "success");
        } else {
            if (finished === "long") {
                state.completedInCycle = 0;
            }
            setPhase("focus", "idle");
            state.endAt = null;
            saveState();
            playChime();
            window.SFToast?.("Break finished — ready for the next focus session?", "success");
        }

        render();
        ensureTicker();
        window.dispatchEvent(new CustomEvent("studyflow:timer", { detail: { state } }));
    }

    function toggleTimer() {
        primeAudio();

        if (state.status === "running") {
            state.remainingMs = Math.max(0, state.endAt - Date.now());
            state.endAt = null;
            state.status = "paused";
        } else {
            if (state.status === "idle" && state.phase === "focus") {
                state.startedAt = Date.now();
            }
            if (state.status === "idle") {
                state.durationMs = phaseMinutes(state.phase, state) * 60000;
                state.remainingMs = state.durationMs;
            }
            state.status = "running";
            state.endAt = Date.now() + Math.max(1000, state.remainingMs);
        }

        saveState();
        render();
        ensureTicker();
    }

    function resetTimer() {
        stopTicker();
        state.status = "idle";
        state.durationMs = phaseMinutes(state.phase, state) * 60000;
        state.remainingMs = state.durationMs;
        state.endAt = null;
        state.startedAt = null;
        saveState();
        render();
    }

    function skipPhase() {
        stopTicker();
        if (state.phase === "focus") {
            const nextPhase = state.completedInCycle + 1 >= state.longBreakInterval ? "long" : "short";
            setPhase(nextPhase, "idle");
            window.SFToast?.("Focus session skipped — no time was recorded.", "error");
        } else {
            if (state.phase === "long") state.completedInCycle = 0;
            setPhase("focus", "idle");
            window.SFToast?.("Break skipped. Ready when you are.", "success");
        }
        saveState();
        render();
    }

    function updateContext(changed) {
        if (state.status !== "idle") {
            render();
            return;
        }

        if (changed === "course") {
            state.courseId = courseSelect?.value || "";
            const selectedTask = taskSelect?.selectedOptions?.[0];
            if (selectedTask?.dataset.course && selectedTask.dataset.course !== state.courseId) {
                state.taskId = "";
                if (taskSelect) taskSelect.value = "";
            }
        } else if (changed === "task") {
            state.taskId = taskSelect?.value || "";
            const selectedTask = taskSelect?.selectedOptions?.[0];
            if (selectedTask?.dataset.course) {
                state.courseId = selectedTask.dataset.course;
                if (courseSelect) courseSelect.value = state.courseId;
            }
        }

        filterTaskOptions();
        saveState();
        render();
    }

    function filterTaskOptions() {
        if (!taskSelect || !courseSelect) return;
        const course = courseSelect.value;
        Array.from(taskSelect.options).forEach((option) => {
            if (!option.value) return;
            const matches = !course || option.dataset.course === course;
            option.hidden = !matches;
            option.disabled = !matches;
        });
    }

    function updateDuration(kind, rawValue) {
        if (state.status !== "idle") {
            render();
            return;
        }

        const limits = { focus: [1, 120], short: [1, 60], long: [1, 90], interval: [2, 8] };
        const [minimum, maximum] = limits[kind];
        const parsed = Number.parseInt(rawValue, 10);
        const value = Math.min(maximum, Math.max(minimum, Number.isFinite(parsed) ? parsed : minimum));

        if (kind === "focus") state.focusMinutes = value;
        else if (kind === "short") state.shortBreakMinutes = value;
        else if (kind === "long") state.longBreakMinutes = value;
        else if (kind === "interval") {
            state.longBreakInterval = value;
            state.completedInCycle = Math.min(state.completedInCycle, value);
        }

        state.durationMs = phaseMinutes(state.phase, state) * 60000;
        state.remainingMs = state.durationMs;
        saveState();
        render();
    }

    async function recordFocus(minutes, completedAtMs) {
        const token = meta("csrf-token");
        const payload = {
            courseId: state.courseId || null,
            taskId: state.taskId || null,
            minutes,
            startedAt: new Date(completedAtMs - minutes * 60000).toISOString(),
            completedAt: new Date(completedAtMs).toISOString()
        };

        try {
            const response = await fetch("/Pomodoro/Record", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token,
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: JSON.stringify(payload)
            });

            const data = await response.json().catch(() => ({}));
            if (!response.ok) {
                window.SFToast?.(data.error || "The focus session could not be saved.", "error");
                return;
            }

            updateStats(data);
            prependHistory(data);
            window.dispatchEvent(new CustomEvent("studyflow:session-recorded", { detail: data }));
        } catch (error) {
            window.SFToast?.("The session finished, but it could not be saved. It will not appear in analytics.", "error");
        }
    }

    function updateStats(data) {
        if (typeof data.todayFocusMinutes === "number") {
            document.querySelectorAll("[data-stat-today-focus]").forEach((el) => {
                el.textContent = formatMinutes(data.todayFocusMinutes);
            });
            const goal = Number(document.querySelector("[data-daily-goal]")?.dataset.dailyGoal || 0);
            if (goal > 0) {
                document.querySelectorAll("[data-goal-percent]").forEach((el) => {
                    el.textContent = `${Math.min(999, Math.round(data.todayFocusMinutes * 100 / goal))}%`;
                });
            }
        }
        if (typeof data.todaySessions === "number") {
            document.querySelectorAll("[data-stat-today-sessions]").forEach((el) => {
                el.textContent = String(data.todaySessions);
            });
        }
        if (typeof data.totalFocusMinutes === "number") {
            document.querySelectorAll("[data-stat-total-focus]").forEach((el) => {
                el.textContent = formatMinutes(data.totalFocusMinutes);
            });
        }
        if (typeof data.streak === "number") {
            document.querySelectorAll("[data-stat-streak]").forEach((el) => {
                el.textContent = `${data.streak}-day streak`;
            });
        }
    }

    function prependHistory(data) {
        const list = document.getElementById("sessionList");
        if (!list) return;

        const row = document.createElement("div");
        row.className = "flex items-center gap-3 border-b border-slate-100 px-5 py-3.5 last:border-0";
        row.setAttribute("data-session-row", "");

        const dot = document.createElement("span");
        dot.className = "h-2 w-2 shrink-0 rounded-full";
        dot.style.backgroundColor = data.courseColor || "#4f46e5";

        const body = document.createElement("div");
        body.className = "min-w-0 flex-1";
        const title = document.createElement("p");
        title.className = "truncate text-[13px] font-medium text-ink-900";
        const labelParts = [];
        if (data.courseName) labelParts.push(data.courseName);
        if (data.taskTitle) labelParts.push(data.taskTitle);
        title.textContent = labelParts.length ? labelParts.join(" · ") : "General study";
        const when = document.createElement("p");
        when.className = "mt-0.5 text-[11px] text-slate-500";
        when.textContent = `Today · ${data.completedAtLabel || ""}`;
        body.append(title, when);

        const duration = document.createElement("span");
        duration.className = "shrink-0 rounded-md bg-slate-50 px-2 py-1 text-[11px] font-medium text-slate-600";
        duration.textContent = data.durationLabel || "25 min";

        row.append(dot, body, duration);
        list.prepend(row);
        document.getElementById("sessionEmpty")?.remove();
    }

    function setContextFromQuery() {
        if (!isTimerPage || state.status !== "idle") return;
        const params = new URLSearchParams(window.location.search);
        const courseId = params.get("courseId");
        const taskId = params.get("taskId");
        if (!courseId && !taskId) return;

        if (taskId && taskSelect?.querySelector(`option[value="${CSS.escape(taskId)}"]`)) {
            state.taskId = taskId;
            const option = taskSelect.querySelector(`option[value="${CSS.escape(taskId)}"]`);
            state.courseId = option.dataset.course || courseId || state.courseId;
        } else if (courseId && courseSelect?.querySelector(`option[value="${CSS.escape(courseId)}"]`)) {
            state.courseId = courseId;
        }

        saveState();
    }

    function syncSelects() {
        if (courseSelect) {
            const exists = Array.from(courseSelect.options).some((option) => option.value === state.courseId);
            courseSelect.value = exists ? state.courseId : "";
            if (!courseSelect.value) state.courseId = "";
        }
        if (taskSelect) {
            const exists = Array.from(taskSelect.options).some((option) => option.value === state.taskId);
            taskSelect.value = exists ? state.taskId : "";
            if (!taskSelect.value) state.taskId = "";
        }
        filterTaskOptions();
    }

    function bindPage() {
        document.addEventListener("click", (event) => {
            const action = event.target.closest("[data-timer-action]")?.dataset.timerAction;
            if (!action) return;
            event.preventDefault();

            if (action === "toggle") toggleTimer();
            else if (action === "reset") resetTimer();
            else if (action === "skip") skipPhase();
            else if (action === "focus-mode") toggleFocusMode(true);
            else if (action === "close-focus") toggleFocusMode(false);
        });

        courseSelect?.addEventListener("change", () => updateContext("course"));
        taskSelect?.addEventListener("change", () => updateContext("task"));

        document.querySelectorAll("[data-duration]").forEach((input) => {
            input.addEventListener("change", () => updateDuration(input.dataset.duration, input.value));
        });
    }

    function toggleFocusMode(open) {
        const overlay = document.getElementById("focusOverlay");
        if (!overlay) return;
        overlay.classList.toggle("hidden", !open);
        document.body.classList.toggle("overflow-hidden", open);
        if (open) {
            overlay.querySelector("[data-overlay-close]")?.focus();
        }
        render();
    }

    function handleExpired() {
        if (!state.expired) return;
        state.expired = false;
        const finishedAt = state.endAt || Date.now();
        completePhase(true, finishedAt);
        window.SFToast?.("That session finished while you were away — it has been saved.", "success");
    }

    function onVisibilityChange() {
        if (document.hidden || state.status !== "running") return;
        const left = state.endAt - Date.now();
        if (left <= 0) {
            completePhase(false, state.endAt || Date.now());
        } else {
            state.remainingMs = left;
            render();
        }
    }

    function init() {
        if (initialized) return;
        initialized = true;

        bindPage();
        if (isTimerPage) {
            setContextFromQuery();
            syncSelects();
        }

        handleExpired();
        render();
        ensureTicker();

        document.addEventListener("visibilitychange", onVisibilityChange);
        window.addEventListener("focus", onVisibilityChange);

        // Keep multiple open tabs reasonably in sync.
        window.addEventListener("storage", (event) => {
            if (event.key !== STORAGE_KEY) return;
            state = loadState();
            if (state.expired) {
                state.expired = false;
                completePhase(true, state.endAt || Date.now());
            } else {
                if (isTimerPage) syncSelects();
                render();
                ensureTicker();
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
