(() => {
    function reindex(container) {
        const rows = container.querySelectorAll("[data-row]");

        rows.forEach((row, index) => {
            row.querySelectorAll("[data-field]").forEach(field => {
                field.name = `Items[${index}].${field.dataset.field}`;
            });

            row.querySelectorAll("[data-valmsg-field]").forEach(span => {
                span.setAttribute(
                    "data-valmsg-for",
                    `Items[${index}].${span.dataset.valmsgField}`
                );
            });
        });
    }

    window.InTakeWiseItemRows = Object.freeze({ reindex });
})();
