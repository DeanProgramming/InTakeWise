(() => {
    const goalInput = document.getElementById("ChosenFitnessGoal");
    const goalLabel = document.getElementById("goalLabel");
    const rows = Array.from(document.querySelectorAll(".fitness-goal-tab"));

    if (!goalInput || rows.length === 0) {
        return;
    }

    function setGoal(goal) {
        goalInput.value = goal;

        rows.forEach(row => {
            const isSelected = row.dataset.goal === goal;

            row.classList.toggle("is-selected", isSelected);
            row.setAttribute("aria-pressed", isSelected.toString());
        });

        const selected = rows.find(row => row.dataset.goal === goal);

        if (selected && goalLabel) {
            const title = selected.querySelector(".menu-action-title") ?.textContent ?? goal;
            goalLabel.textContent = title;
        }
    }

    rows.forEach(row => {
        row.addEventListener("click", () => {
            setGoal(row.dataset.goal);
        });
    });

    const initialGoal = rows.find( row => row.getAttribute("aria-pressed") === "true")?.dataset.goal ?? goalInput.value;

    setGoal(initialGoal);
})();
