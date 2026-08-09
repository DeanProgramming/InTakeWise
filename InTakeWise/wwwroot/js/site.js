(() => {
    function initialiseDemoReadOnlyUi() {
        if (document.body?.dataset.demoMode !== "true") {
            return;
        }

        const writeForms = Array
            .from(document.querySelectorAll("form"))
            .filter(form => {
                if (form.dataset.demoAllow === "logout") {
                    return false;
                }

                const method =
                    (form.getAttribute("method") || "get")
                        .toLowerCase();

                return method !== "get" && method !== "dialog";
            });

        if (writeForms.length === 0) {
            return;
        }

        let note =
            document.querySelector(".demo-action-note");

        if (!note) {
            note = document.createElement("div");
            note.id = "demo-page-readonly-note";
            note.className =
                "menu-action demo-action-note";

            note.setAttribute("role", "note");

            note.innerHTML = `
                <div class="menu-action-title">
                    Read-only sample
                </div>
                <div class="menu-action-sub">
                    You can explore this page, but its controls
                    cannot be changed in demo mode.
                </div>
            `;

            writeForms[0].before(note);
        } else if (!note.id) {
            note.id = "demo-page-readonly-note";
        }

        const descriptionId =
            note.id || "demo-mode-message";

        const showBanner = () => {
            document
                .getElementById("demo-mode-banner")
                ?.focus();
        };

        writeForms.forEach(form => {
            form.classList.add("demo-read-only-form");

            const controls = form.querySelectorAll(
                "button, " +
                "input:not([type='hidden']), " +
                "select, " +
                "textarea, " +
                "a[href], " +
                "[onclick], " +
                "[role='button']"
            );

            controls.forEach(control => {
                control.classList.add(
                    "demo-control-disabled"
                );

                control.setAttribute(
                    "aria-disabled",
                    "true"
                );

                control.setAttribute(
                    "title",
                    "Unavailable in demo mode"
                );

                const existingDescription =
                    control.getAttribute(
                        "aria-describedby"
                    );

                control.setAttribute(
                    "aria-describedby",
                    [existingDescription, descriptionId]
                        .filter(Boolean)
                        .join(" ")
                );

                if (control.matches(
                    "button, input, select, textarea")) {
                    control.disabled = true;
                }

                if (control.matches(
                    "a, [onclick], [role='button']")) {
                    control.tabIndex = -1;
                }
            });

            form.addEventListener(
                "submit",
                event => {
                    event.preventDefault();
                    event.stopImmediatePropagation();
                    showBanner();
                },
                true
            );

            // Also catches anchor-based controls used by the
            // profile wizard.
            form.addEventListener(
                "click",
                event => {
                    if (!(event.target instanceof Element)) {
                        return;
                    }

                    const control = event.target.closest(
                        "button, input, select, textarea, " +
                        "a, [onclick], [role='button']"
                    );

                    if (!control || !form.contains(control)) {
                        return;
                    }

                    event.preventDefault();
                    event.stopImmediatePropagation();
                    showBanner();
                },
                true
            );
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            initialiseDemoReadOnlyUi
        );
    } else {
        initialiseDemoReadOnlyUi();
    }
})();