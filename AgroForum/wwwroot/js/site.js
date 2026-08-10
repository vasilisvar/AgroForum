// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.querySelectorAll("[data-image-input]").forEach((input) => {
    input.addEventListener("change", () => {
        const picker = input.closest(".create-image-picker");
        const fileName = picker?.querySelector("[data-file-name]");
        const prompt = picker?.querySelector("[data-file-prompt]");
        const selectedFile = input.files?.[0];

        if (!fileName || !prompt) {
            return;
        }

        picker.classList.toggle("has-file", Boolean(selectedFile));
        prompt.textContent = selectedFile ? "Image selected" : "Choose a discussion image";
        fileName.textContent = selectedFile?.name ?? "JPEG, PNG, or WebP · up to 5 MB";
    });
});

document.querySelectorAll("[data-resource-disclosure]").forEach((disclosure) => {
    const hasAValue = Array.from(disclosure.querySelectorAll("input")).some((input) => input.value.trim().length > 0);
    const hasAnError = Boolean(disclosure.querySelector(".field-validation-error"));

    if (hasAValue || hasAnError) {
        disclosure.open = true;
    }
});
