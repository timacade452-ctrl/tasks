(function () {
    "use strict";

    const sidebar = document.getElementById("sidebar");
    const overlay = document.getElementById("sidebarOverlay");
    const sidebarToggle = document.getElementById("sidebarToggle");

    function openSidebar() {
        if (!sidebar || !overlay) return;
        sidebar.classList.remove("-translate-x-full");
        overlay.classList.remove("hidden");
        sidebarToggle?.setAttribute("aria-expanded", "true");
        document.body.classList.add("overflow-hidden", "lg:overflow-auto");
    }

    function closeSidebar() {
        if (!sidebar || !overlay) return;
        sidebar.classList.add("-translate-x-full");
        overlay.classList.add("hidden");
        sidebarToggle?.setAttribute("aria-expanded", "false");
        document.body.classList.remove("overflow-hidden", "lg:overflow-auto");
    }

    sidebarToggle?.addEventListener("click", openSidebar);
    overlay?.addEventListener("click", closeSidebar);
    sidebar?.querySelectorAll("a").forEach((link) => {
        link.addEventListener("click", () => {
            if (window.matchMedia("(max-width: 1023px)").matches) closeSidebar();
        });
    });

    // Dropdown menus
    const dropdownRoots = Array.from(document.querySelectorAll("[data-dropdown-root]"));

    function closeDropdowns(except) {
        dropdownRoots.forEach((root) => {
            if (root === except) return;
            const menu = root.querySelector("[data-dropdown-menu]");
            const trigger = root.querySelector("[data-dropdown-trigger]");
            menu?.classList.remove("dropdown-open");
            trigger?.setAttribute("aria-expanded", "false");
        });
    }

    dropdownRoots.forEach((root) => {
        const trigger = root.querySelector("[data-dropdown-trigger]");
        const menu = root.querySelector("[data-dropdown-menu]");
        if (!trigger || !menu) return;

        trigger.addEventListener("click", (event) => {
            event.stopPropagation();
            const isOpen = menu.classList.contains("dropdown-open");
            closeDropdowns(root);
            menu.classList.toggle("dropdown-open", !isOpen);
            trigger.setAttribute("aria-expanded", String(!isOpen));
        });
    });

    document.addEventListener("click", (event) => {
        if (!event.target.closest("[data-dropdown-root]")) closeDropdowns();
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            closeDropdowns();
            closeSidebar();
            hideConfirm();
        }
    });

    // Destructive action confirmation
    const confirmDialog = document.getElementById("confirmDialog");
    const confirmMessage = document.getElementById("confirmMessage");
    let pendingForm = null;

    function showConfirm(form) {
        pendingForm = form;
        if (!confirmDialog) {
            if (window.confirm(form.dataset.confirm || "Are you sure?")) {
                form.dataset.confirmed = "true";
                form.requestSubmit();
            }
            return;
        }
        if (confirmMessage) {
            confirmMessage.textContent = form.dataset.confirm || "This action cannot be undone.";
        }
        confirmDialog.classList.remove("hidden");
        confirmDialog.classList.add("flex");
    }

    function hideConfirm() {
        pendingForm = null;
        if (!confirmDialog) return;
        confirmDialog.classList.add("hidden");
        confirmDialog.classList.remove("flex");
    }

    document.addEventListener("submit", (event) => {
        const form = event.target.closest("form[data-confirm]");
        if (!form || form.dataset.confirmed === "true") return;
        event.preventDefault();
        showConfirm(form);
    });

    confirmDialog?.querySelectorAll("[data-confirm-cancel]").forEach((button) => {
        button.addEventListener("click", hideConfirm);
    });

    confirmDialog?.querySelector("[data-confirm-ok]")?.addEventListener("click", () => {
        if (!pendingForm) return hideConfirm();
        const form = pendingForm;
        form.dataset.confirmed = "true";
        hideConfirm();
        form.requestSubmit();
        window.setTimeout(() => { delete form.dataset.confirmed; }, 1000);
    });

    // Toasts
    function dismissToast(toast) {
        if (!toast) return;
        toast.classList.add("opacity-0", "translate-y-1");
        window.setTimeout(() => toast.remove(), 220);
    }

    function wireToast(toast) {
        toast.querySelector("[data-toast-close]")?.addEventListener("click", () => dismissToast(toast));
        window.setTimeout(() => dismissToast(toast), 6000);
    }

    function showToast(message, tone) {
        const stack = document.getElementById("toastStack");
        if (!stack) return;

        const isError = tone === "error";
        const toast = document.createElement("div");
        toast.className = "pointer-events-auto flex items-start gap-3 rounded-xl border bg-white p-3.5 shadow-pop transition-all duration-200";

        const iconWrap = document.createElement("span");
        iconWrap.className = "mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg " +
            (isError ? "bg-rose-50 text-rose-600" : "bg-emerald-50 text-emerald-600");

        const icon = document.createElement("i");
        icon.setAttribute("data-lucide", isError ? "alert-circle" : "check-circle-2");
        icon.className = "h-4 w-4";
        iconWrap.appendChild(icon);

        const body = document.createElement("div");
        body.className = "min-w-0 flex-1";
        const heading = document.createElement("p");
        heading.className = "text-sm font-semibold text-ink-900";
        heading.textContent = isError ? "Something needs attention" : "Done";
        const text = document.createElement("p");
        text.className = "mt-0.5 text-xs leading-relaxed text-slate-600";
        text.textContent = message;
        body.appendChild(heading);
        body.appendChild(text);

        const close = document.createElement("button");
        close.type = "button";
        close.className = "text-slate-400 hover:text-slate-600";
        close.setAttribute("aria-label", "Dismiss");
        const closeIcon = document.createElement("i");
        closeIcon.setAttribute("data-lucide", "x");
        closeIcon.className = "h-4 w-4";
        close.appendChild(closeIcon);

        toast.append(iconWrap, body, close);
        stack.appendChild(toast);
        window.lucide?.createIcons();
        wireToast(toast);
    }

    document.querySelectorAll("[data-toast]").forEach(wireToast);

    // Filters submit automatically for selects and dates
    document.querySelectorAll("[data-auto-submit]").forEach((field) => {
        field.addEventListener("change", () => field.form?.requestSubmit());
    });

    window.SFToast = showToast;

    // Refresh icons after the DOM is ready
    window.lucide?.createIcons({ attrs: { "stroke-width": 1.8, "aria-hidden": "true" } });
})();
