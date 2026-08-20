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
    const activityButtons = Array.from(document.querySelectorAll(".activity-row"));
    const gymDayButtons = Array.from(document.querySelectorAll(".curved-profile-Gym-Button"));

    if (!fitnessInput || !gymDaysInput) {
        return;
    }

    function setFitness(level) {
        fitnessInput.value = level;

        if (activityLabel) {
            activityLabel.textContent = level;
        }

        activityButtons.forEach(button => {
            const isSelected = button.dataset.fitnessLevel === level;
            button.classList.toggle("is-active", isSelected);
            button.setAttribute("aria-pressed", isSelected.toString());
        });
    }

    function toggleDay(dayName) {
        const bit = dayMap[dayName];

        if (!bit) {
            return;
        }

        let current =
            Number.parseInt(gymDaysInput.value || "0", 10) || 0;

        current ^= bit;
        gymDaysInput.value = current.toString();

        const button = gymDayButtons.find(
            item => item.dataset.day === dayName
        );

        const isSelected = (current & bit) !== 0;

        if (button) {
            button.classList.toggle("is-active", isSelected);
            button.setAttribute(
                "aria-pressed",
                isSelected.toString()
            );
        }
    }

    activityButtons.forEach(button => {
        button.addEventListener("click", () => {
            setFitness(button.dataset.fitnessLevel);
        });
    });

    gymDayButtons.forEach(button => {
        button.addEventListener("click", () => {
            toggleDay(button.dataset.day);
        });
    });

    const initialFitness = activityButtons.find(button => button.getAttribute("aria-pressed") === "true")?.dataset.fitnessLevel ?? fitnessInput.value;

    setFitness(initialFitness);

    const mask = Number.parseInt(gymDaysInput.dataset.initialGymDays || "0", 10) || 0;

    gymDaysInput.value = mask.toString();

    gymDayButtons.forEach(button => {
        const bit = dayMap[button.dataset.day];
        const isSelected = Boolean(bit && (mask & bit) !== 0);

        button.classList.toggle("is-active", isSelected);
        button.setAttribute("aria-pressed", isSelected.toString());
    });
})();
