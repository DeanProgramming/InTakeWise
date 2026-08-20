(function () {
    const form = document.getElementById("generatePlanForm");
    const button = document.getElementById("generatePlanBtn");
    const buttonText = document.getElementById("generatePlanBtnText");
    const spinner = document.getElementById("generatePlanSpinner");
    const loadingPanel = document.getElementById("loadingPanel");

    if (!form || !button || !buttonText || !spinner || !loadingPanel) return;

    form.addEventListener("submit", function () {
        button.disabled = true;
        button.classList.add("is-loading");
        spinner.classList.add("show");
        loadingPanel.hidden = false;

        const title = buttonText.querySelector(".menu-action-title");
        const sub = buttonText.querySelector(".menu-action-sub");

        if (title) title.textContent = "Generating...";
        if (sub) sub.textContent = "Please wait while your weekly plan is prepared.";
    });
})();
