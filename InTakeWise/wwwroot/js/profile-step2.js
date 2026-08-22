(() => {
    const dayMap = {
        Monday: 1,
        Tuesday: 2,
        Wednesday: 4,
        Thursday: 8,
        Friday: 16,
        Saturday: 32,
        Sunday: 64
    };

    const fitnessInput = document.getElementById("EveryDayFitnessLevel");
    const gymDaysInput = document.getElementById("ChosenGymDays");
    const activityLabel = document.getElementById("activityLabel");
    const noGymButton = document.getElementById("noGymDaysButton");
    const activityButtons = Array.from(document.querySelectorAll(".activity-row"));
    const gymDayButtons = Array.from(document.querySelectorAll(".curved-profile-Gym-Button"));

    if (!fitnessInput || !gymDaysInput) {
        return;
    }

    function renderFitness(level) {
        const selectedLevel = level || "";

        fitnessInput.value = selectedLevel;

        if (activityLabel) {
            activityLabel.textContent = selectedLevel || "Not selected";
        }

        activityButtons.forEach(button => {
            const isSelected = button.dataset.fitnessLevel === selectedLevel;
            button.classList.toggle("is-active", isSelected);
            button.setAttribute("aria-pressed", isSelected.toString());
        });
    }

    function renderGymDays(mask) {
        gymDaysInput.value = mask.toString();

        gymDayButtons.forEach(button => {
            const dayName = button.dataset.day;
            const bit = dayMap[dayName];
            const isSelected = Boolean(bit && (mask & bit) !== 0);
            button.classList.toggle("is-active", isSelected);
            button.setAttribute("aria-pressed", isSelected.toString());
        });

        if (noGymButton) {
            const noGymSelected = mask === 0;
            noGymButton.classList.toggle("is-active", noGymSelected);
            noGymButton.setAttribute("aria-pressed", noGymSelected.toString());
        }
    }

    function toggleDay(dayName) {
        const bit = dayMap[dayName];

        if (!bit) {
            return;
        }

        let current = Number.parseInt(gymDaysInput.value || "0", 10) || 0;

        current ^= bit;
        renderGymDays(current);
    }

    activityButtons.forEach(button => {
        button.addEventListener("click", () => {
            renderFitness(
                button.dataset.fitnessLevel
            );
        });
    });

    gymDayButtons.forEach(button => {
        button.addEventListener("click", () => {
            toggleDay(button.dataset.day);
        });
    });

    if (noGymButton) {
        noGymButton.addEventListener("click", () => {
            renderGymDays(0);
        });
    }

    const initialFitness = activityButtons.find(button => button.getAttribute("aria-pressed") === "true")?.dataset.fitnessLevel ?? fitnessInput.value;

    renderFitness(initialFitness);

    const initialGymDays = Number.parseInt(gymDaysInput.dataset.initialGymDays || "0",) || 0;

    renderGymDays(initialGymDays);
})();