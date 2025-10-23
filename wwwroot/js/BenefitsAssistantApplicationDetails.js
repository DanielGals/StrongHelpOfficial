document.addEventListener('DOMContentLoaded', function () {
    // Document preview
    const previewModal = new bootstrap.Modal(document.getElementById('docPreviewModal'));
    document.querySelectorAll('.doc-view-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const url = this.getAttribute('data-doc-url');
            const type = this.getAttribute('data-doc-type');
            const name = this.getAttribute('data-doc-name');
            let content = '';

            if (type.toLowerCase().includes('pdf')) {
                content = `<iframe src="${url}" width="100%" height="500px" style="border:none;"></iframe>`;
            } else if (type.toLowerCase().includes('image')) {
                content = `<img src="${url}" alt="${name}" class="img-fluid" />`;
            } else {
                content = `<a href="${url}" target="_blank">Download/Open Document</a>`;
            }

            document.getElementById('docPreviewModalLabel').textContent = name;
            document.getElementById('docPreviewBody').innerHTML = content;
            previewModal.show();
        });
    });

    // Checklist add item modal logic (scoped)
    const addItemBtn = document.getElementById('addItemBtn');
    const checklistItemsContainer = document.getElementById('checklistItemsContainer');
    const forwardBtn = document.getElementById('forwardApplicationBtn');
    const modalEl = document.getElementById('addChecklistItemModal');
    let addChecklistItemModal = null;
    const CHAR_LIMIT = 200;

    // Safe scoped query helper
    function modalQuery(selector) {
        return modalEl ? modalEl.querySelector(selector) : null;
    }

    // Ensure modal instance and wire scoped handlers
    if (modalEl) {
        addChecklistItemModal = new bootstrap.Modal(modalEl);

        // When modal is shown, reset input and ensure maxlength (scoped)
        modalEl.addEventListener('shown.bs.modal', function () {
            const conditionTextInput = modalQuery('#conditionTextInput');
            if (conditionTextInput) {
                conditionTextInput.value = '';
                conditionTextInput.setAttribute('maxlength', String(CHAR_LIMIT));
                conditionTextInput.focus();
            }
        });

        // Scoped confirm / fill buttons (attach once)
        const confirmBtn = modalQuery('#confirmAddChecklistBtn');
        const fillBtn = modalQuery('#fillPreexistingChecklistBtn');

        if (confirmBtn) {
            confirmBtn.addEventListener('click', function () {
                const conditionTextInput = modalQuery('#conditionTextInput');
                const text = (conditionTextInput?.value || '').trim();

                // Enforce CHAR_LIMIT (defensive)
                if (text.length === 0) {
                    conditionTextInput?.focus();
                    return;
                }
                if (text.length > CHAR_LIMIT) {
                    alert(`Checklist item must be ${CHAR_LIMIT} characters or fewer.`);
                    conditionTextInput.focus();
                    return;
                }

                // add near top of file (after CHAR_LIMIT or utility functions)
window.addPreexistingChecklistItems = function(items, container) {
    if (!Array.isArray(items) || !container) return;
    items.forEach((it, idx) => {
        const id = 'checklist_' + Date.now() + '_' + idx;
        const div = document.createElement('div');
        div.className = 'form-check mb-2';
        div.innerHTML = `<div class="check-left">
                                <input class="form-check-input" type="checkbox" id="${id}">
                                <label class="form-check-label checklist-item ms-2" for="${id}">${it}</label>
                            </div>
                            <button type="button" class="btn btn-link btn-sm text-danger ms-2 checklist-remove-btn" title="Remove item" style="padding:0 0.25rem;">
                                <svg xmlns="http://www.w3.org/2000/svg" width="1.2em" height="1.2em" fill="none" stroke="currentColor" class="feather feather-trash" viewBox="0 0 24 24">
                                    <polyline points="3 6 5 6 21 6" />
                                    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                                    <path d="M10 11v6" />
                                    <path d="M14 11v6" />
                                    <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                                </svg>
                            </button>`;
        container.appendChild(div);
    });
};

                const id = 'checklist_' + Date.now();
                const div = document.createElement('div');
                div.className = 'form-check mb-2';
                div.innerHTML = `<div class="check-left">
                                        <input class="form-check-input" type="checkbox" id="${id}">
                                        <label class="form-check-label checklist-item ms-2" for="${id}">${text}</label>
                                    </div>
                                    <button type="button" class="btn btn-link btn-sm text-danger ms-2 checklist-remove-btn" title="Remove item" style="padding:0 0.25rem;">
                                        <svg xmlns="http://www.w3.org/2000/svg" width="1.2em" height="1.2em" fill="none" stroke="currentColor" class="feather feather-trash" viewBox="0 0 24 24">
                                            <polyline points="3 6 5 6 21 6" />
                                            <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                                            <path d="M10 11v6" />
                                            <path d="M14 11v6" />
                                            <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                                        </svg>
                                    </button>`;
                checklistItemsContainer.appendChild(div);

                // Reset modal input then hide
                if (conditionTextInput) {
                    conditionTextInput.value = '';
                }
                addChecklistItemModal.hide();
                updateRejectButtonState();
            });
        }

        if (fillBtn) {
            fillBtn.addEventListener('click', function () {
                const items = [
                    'Completed Requirements',
                    'No derogatory legal records (civil/criminal cases)',
                    'Eligible co-maker'
                ];
                items.forEach((it, idx) => {
                    const id = 'checklist_' + Date.now() + '_' + idx;
                    const div = document.createElement('div');
                    div.className = 'form-check mb-2';
                    div.innerHTML = `<div class="check-left">
                                            <input class="form-check-input" type="checkbox" id="${id}">
                                            <label class="form-check-label checklist-item ms-2" for="${id}">${it}</label>
                                        </div>
                                        <button type="button" class="btn btn-link btn-sm text-danger ms-2 checklist-remove-btn" title="Remove item" style="padding:0 0.25rem;">
                                            <svg xmlns="http://www.w3.org/2000/svg" width="1.2em" height="1.2em" fill="none" stroke="currentColor" class="feather feather-trash" viewBox="0 0 24 24">
                                                <polyline points="3 6 5 6 21 6" />
                                                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                                                <path d="M10 11v6" />
                                                <path d="M14 11v6" />
                                                <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                                            </svg>
                                        </button>`;
                    checklistItemsContainer.appendChild(div);
                });

                addChecklistItemModal.hide();
                updateRejectButtonState();
            });
        }
    }

    if (addItemBtn) {
        addItemBtn.addEventListener('click', function () {
            if (!addChecklistItemModal && modalEl) {
                addChecklistItemModal = new bootstrap.Modal(modalEl);
            }
            addChecklistItemModal?.show();
        });
    }

    // Function to check if all checklist checkboxes are checked
    function updateRejectButtonState() {
        const checkboxes = checklistItemsContainer.querySelectorAll('input[type="checkbox"]');
        if (checkboxes.length === 0) {
            forwardBtn.disabled = true;
            return;
        }
        forwardBtn.disabled = !Array.from(checkboxes).every(cb => cb.checked);
    }

    // Listen for changes on checklist checkboxes
    checklistItemsContainer.addEventListener('change', function (e) {
        if (e.target && e.target.type === 'checkbox') {
            updateRejectButtonState();
        }
    });

    // Remove checklist item logic
    checklistItemsContainer.addEventListener('click', function (e) {
        if (e.target.closest('.checklist-remove-btn')) {
            const btn = e.target.closest('.checklist-remove-btn');
            // remove the nearest .form-check wrapper
            const wrapper = btn.closest('.form-check');
            if (wrapper) wrapper.remove();
            updateRejectButtonState();
        }
    });

    // Initial state
    updateRejectButtonState();

    // Reject Application button logic
    const rejectBtn = document.getElementById('rejectApplicationBtn');
    const rejectCommentContainer = document.getElementById('rejectCommentContainer');
    const confirmRejectBtn = document.getElementById('confirmRejectBtn');
    const rejectCommentBox = document.getElementById('rejectCommentBox');
    const rejectConfirmModal = new bootstrap.Modal(document.getElementById('rejectConfirmModal'));
    const rejectCommentPreview = document.getElementById('rejectCommentPreview');

    rejectBtn.addEventListener('click', function () {
        rejectCommentContainer.style.display = 'block';
        rejectCommentBox.focus();
    });

    confirmRejectBtn.addEventListener('click', function () {
        const comment = rejectCommentBox.value.trim();
        rejectCommentPreview.textContent = comment.length > 0 ? comment : "(No comment provided)";
        rejectConfirmModal.show();
    });

    // Reject action handler
    const finalRejectBtn = document.getElementById('finalRejectBtn');
    finalRejectBtn.addEventListener('click', function () {
        const loanId = document.getElementById('loanIdField').value;
        const remarks = document.getElementById('rejectCommentBox').value.trim();
        const rejectUrl = document.getElementById('rejectUrlField').value;

        fetch(rejectUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: `id=${encodeURIComponent(loanId)}&remarks=${encodeURIComponent(remarks)}`
        })
            .then(response => {
                if (response.redirected) {
                    window.location.href = response.url;
                } else {
                    location.reload();
                }
            });
        rejectConfirmModal.hide();
    });
});