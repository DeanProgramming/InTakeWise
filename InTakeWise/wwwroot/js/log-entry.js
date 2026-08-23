(() => {
    const form = document.getElementById("logEntryForm");
    const textArea = form?.querySelector('textarea[name="userInput"]');

    if (!form || !textArea) {
        return;
    }

    const key = form.dataset.logDraftKey;

    if (key) {
        const draft = localStorage.getItem(key);

        if (draft !== null && !textArea.value) {
            textArea.value = draft;
        }

        textArea.addEventListener("input", () => {
            localStorage.setItem(key, textArea.value);
        });
    }

    const button = document.getElementById("logSubmitBtn");
    const buttonText = document.getElementById("logSubmitBtnText");
    const spinner = document.getElementById("logSubmitSpinner");
    const loadingPanel = document.getElementById("logLoadingPanel");

    form.addEventListener("submit", () => {
        if (key) {
            localStorage.removeItem(key);
        }

        if (!button || !buttonText || !spinner || !loadingPanel) {
            return;
        }

        button.disabled = true;
        button.classList.add("is-loading");
        spinner.classList.add("show");
        loadingPanel.hidden = false;
        form.setAttribute("aria-busy", "true");

        const title = buttonText.querySelector(".menu-action-title");
        const sub = buttonText.querySelector(".menu-action-sub");
        const logKind = form.dataset.logKind === "workout" ? "workout" : "meal";

        if (title) {
            title.textContent = "Analysing...";
        }

        if (sub) {
            sub.textContent = `Please wait while your ${logKind} is processed.`;
        }
    });
})();
