(() => {
    const modalElement = document.getElementById("app-modal");
    const modalContent = document.getElementById("app-modal-content");

    if (!modalElement || !modalContent) {
        return;
    }

    const requestHeaders = {
        "X-Requested-With": "XMLHttpRequest"
    };

    async function responseMessage(response) {
        const contentType = response.headers.get("content-type") || "";

        if (contentType.includes("application/json") || contentType.includes("application/problem+json")) {
            const problem = await response.json();
            return problem.detail || problem.title || `Request failed with status ${response.status}.`;
        }

        return `Request failed with status ${response.status}.`;
    }

    function parseValidation() {
        if (window.jQuery?.validator?.unobtrusive) {
            window.jQuery.validator.unobtrusive.parse(modalContent);
        }
    }

    function showModalError(message) {
        let error = modalContent.querySelector("[data-modal-error]");

        if (!error) {
            error = document.createElement("div");
            error.className = "alert alert-danger m-3";
            error.dataset.modalError = "";
            error.setAttribute("role", "alert");
            modalContent.prepend(error);
        }

        error.textContent = message;
        error.classList.remove("d-none");
    }

    function renderModal(html) {
        modalContent.innerHTML = html;
        parseValidation();
        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }

    async function openModal(url) {
        const response = await fetch(url, {
            headers: requestHeaders,
            credentials: "same-origin"
        });

        if (response.redirected) {
            window.location.assign(response.url);
            return;
        }

        if (!response.ok) {
            throw new Error(await responseMessage(response));
        }

        renderModal(await response.text());
    }

    async function refreshRegion(name) {
        const regions = [...document.querySelectorAll("[data-refresh-region]")]
            .filter(region => region.dataset.refreshRegion === name);

        if (regions.length === 0) {
            throw new Error(`Refresh region '${name}' was not found.`);
        }

        await Promise.all(regions.map(async region => {
            const url = region.dataset.refreshUrl;
            if (!url) {
                throw new Error(`Refresh region '${name}' does not have a refresh URL.`);
            }

            const response = await fetch(url, {
                headers: requestHeaders,
                credentials: "same-origin"
            });

            if (!response.ok) {
                throw new Error(await responseMessage(response));
            }

            region.innerHTML = await response.text();
            region.dispatchEvent(new CustomEvent("app:refreshed", { bubbles: true }));
        }));
    }

    document.addEventListener("click", async event => {
        const trigger = event.target.closest("[data-modal-url]");
        if (!trigger) {
            return;
        }

        event.preventDefault();
        trigger.setAttribute("aria-disabled", "true");

        try {
            await openModal(trigger.dataset.modalUrl);
        } catch (error) {
            window.alert(error.message || "Unable to load the modal.");
        } finally {
            trigger.removeAttribute("aria-disabled");
        }
    });

    modalElement.addEventListener("submit", async event => {
        const form = event.target.closest("form[data-modal-form]");
        if (!form) {
            return;
        }

        event.preventDefault();
        const submitButtons = form.querySelectorAll("button[type='submit'], input[type='submit']");
        submitButtons.forEach(button => {
            button.disabled = true;
        });

        try {
            const response = await fetch(form.action, {
                method: (form.method || "POST").toUpperCase(),
                body: new FormData(form),
                headers: requestHeaders,
                credentials: "same-origin"
            });

            if (response.redirected) {
                window.location.assign(response.url);
                return;
            }

            const contentType = response.headers.get("content-type") || "";

            if (response.ok && contentType.includes("application/json")) {
                const result = await response.json();
                if (!result.success) {
                    throw new Error(result.message || "The request did not complete successfully.");
                }

                await refreshRegion(result.refresh);
                bootstrap.Modal.getOrCreateInstance(modalElement).hide();
                return;
            }

            if (response.ok && contentType.includes("text/html")) {
                modalContent.innerHTML = await response.text();
                parseValidation();
                return;
            }

            throw new Error(await responseMessage(response));
        } catch (error) {
            showModalError(error.message || "Unable to submit the form.");
        } finally {
            submitButtons.forEach(button => {
                button.disabled = false;
            });
        }
    });
})();
