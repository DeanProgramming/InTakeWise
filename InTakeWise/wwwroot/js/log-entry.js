(() => {
    const form = document.querySelector(".log-form");
    const textArea = form?.querySelector('textarea[name="userInput"]');

    if (!form || !textArea) {
        return;
    }

    const key = form.dataset.logDraftKey;

    if (!key) {
        return;
    }

    const draft = localStorage.getItem(key);

    if (draft !== null && !textArea.value) {
        textArea.value = draft;
    }

    textArea.addEventListener("input", () => {
        localStorage.setItem(key, textArea.value);
    });

    form.addEventListener("submit", () => {
        localStorage.removeItem(key);
    });
})();
