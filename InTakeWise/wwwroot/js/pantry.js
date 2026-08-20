(() => {
    const form = document.getElementById("pantryForm");
    const container = document.getElementById("pantryRows");
    const addButton = document.getElementById("addPantryRowButton");
    const itemRows = window.InTakeWiseItemRows;

    if (!form || !container || !itemRows) {
        return;
    }

    function addRow() {
        const index = container.querySelectorAll("[data-row]").length;

        const row = document.createElement("div");
        row.className = "menu-action pantry-card";
        row.setAttribute("data-row", "");

        row.innerHTML = `
            <input type="hidden" name="Items[${index}].Id" value="0" data-field="Id" />
            <input type="hidden" name="Items[${index}].FoodItemId" value="0" data-field="FoodItemId" />

            <div class="pantry-fields">
                <div class="pantry-field pantry-field-name">
                    <label class="pantry-label">Food</label>
                    <input type="text"
                           name="Items[${index}].Name"
                           value=""
                           data-field="Name"
                           class="menu-input pantry-input"
                           maxlength="100"
                           placeholder="Food item" />
                    <span class="menu-field-error" data-valmsg-field="Name"></span>
                </div>

                <div class="pantry-field pantry-field-qty">
                    <label class="pantry-label">Amount</label>
                    <input type="number"
                           name="Items[${index}].Quantity"
                           value=""
                           data-field="Quantity"
                           class="menu-input pantry-input"
                           placeholder="Amount"
                           min="0.01"
                           max="100000"
                           step="0.01" />
                    <span class="menu-field-error" data-valmsg-field="Quantity"></span>
                </div>

                <div class="pantry-field pantry-field-unit">
                    <label class="pantry-label">Unit</label>
                    <input type="text"
                           name="Items[${index}].Unit"
                           value=""
                           data-field="Unit"
                           class="menu-input pantry-input"
                           maxlength="30"
                           placeholder="Unit (g, kg, ml, tins)" />
                    <span class="menu-field-error" data-valmsg-field="Unit"></span>
                </div>

                <div class="pantry-field pantry-field-delete">
                    <label class="pantry-label pantry-label-ghost">Action</label>
                    <button type="button"
                            class="pantry-btn pantry-btn-delete">
                        Delete
                    </button>
                </div>
            </div>
        `;

        container.appendChild(row);
        itemRows.reindex(container);
    }

    function removeRow(button) {
        const row = button.closest("[data-row]");
        if (row) {
            row.remove();
            itemRows.reindex(container);
        }
    }

    addButton?.addEventListener("click", addRow);

    container.addEventListener("click", event => {
        if (!(event.target instanceof Element)) {
            return;
        }

        const deleteButton = event.target.closest(".pantry-btn-delete");

        if (deleteButton && container.contains(deleteButton)) {
            removeRow(deleteButton);
        }
    });

    form.addEventListener("submit", () => {
        itemRows.reindex(container);
    });
})();
