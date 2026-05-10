document.querySelectorAll(".translate-form").forEach((form) => {
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const output = form.parentElement.querySelector(".translation");
    output.textContent = "Translating...";

    try {
      const response = await fetch("/Translation/Translate", {
        method: "POST",
        body: new FormData(form)
      });

      const payload = await response.json();
      output.textContent = payload.error ?? `${payload.translatedText} (${payload.source})`;
    } catch {
      output.textContent = "Translation failed. Check the function URL and logs.";
    }
  });
});
