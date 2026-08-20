(() => {
    const form = document.getElementById("receiptForm");
    const container = document.getElementById("receiptRows");
    const addButton = document.getElementById("addReceiptRowButton");
    const itemRows = window.InTakeWiseItemRows;
    const maxReceiptImageBytes = 10 * 1024 * 1024;
    const receiptUploadForm = document.getElementById("receiptUploadForm");
    const receiptImageInput = document.getElementById("receiptImage");

    receiptImageInput?.addEventListener("change", function () {
        this.setCustomValidity("");
    });

    receiptUploadForm?.addEventListener("submit", function (event) {
        const file = receiptImageInput?.files?.[0];

        if (file && file.size > maxReceiptImageBytes) {
            event.preventDefault();
            receiptImageInput.setCustomValidity("The receipt photo must be 10 MB or smaller.");
            receiptImageInput.reportValidity();
            return;
        }

        const button = document.getElementById("analyseReceiptButton");

        if (!button) {
            return;
        }

        button.disabled = true;
        button.setAttribute("aria-busy", "true");
        button.querySelector(".menu-action-title").textContent = "Analysing Receipt…";
        button.querySelector(".menu-action-sub").textContent = "Reading visible grocery lines";
    });

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
                           placeholder="Food item"
                           maxlength="100" />
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
                           placeholder="Unit (g, kg, ml, items)"
                           maxlength="30" />
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

    if (form && container && itemRows) {
        addButton?.addEventListener("click", addRow);

        container.addEventListener("click", event => {
            if (!(event.target instanceof Element)) {
                return;
            }

            const deleteButton = event.target.closest(".pantry-btn-delete");

            if (deleteButton && container.contains(deleteButton))
            {
                removeRow(deleteButton);
            }
        });

        form.addEventListener("submit", () => {
            itemRows.reindex(container);
        });
    }
})();
