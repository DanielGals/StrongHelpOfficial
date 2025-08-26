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

    // Checklist add item modal logic
    const addItemBtn = document.getElementById('addItemBtn');
    const checklistItemsContainer = document.getElementById('checklistItemsContainer');
    const forwardBtn = document.getElementById('forwardApplicationBtn');

    let addChecklistItemModal = null;
    let confirmAddChecklistBtn = null;
    let conditionTextInput = null;

    if (addItemBtn) {
        addItemBtn.addEventListener('click', function () {
            if (!addChecklistItemModal) {
                addChecklistItemModal = new bootstrap.Modal(document.getElementById('addChecklistItemModal'));
                confirmAddChecklistBtn = document.getElementById('confirmAddChecklistBtn');
                conditionTextInput = document.getElementById('conditionTextInput');
            }
            conditionTextInput.value = '';
            addChecklistItemModal.show();
        });
    }

    // Function to check if all checklist checkboxes are checked
    function updateRejectButtonState() {
        if (!checklistItemsContainer || !forwardBtn) return;
        
        const checkboxes = checklistItemsContainer.querySelectorAll('input[type="checkbox"]');
        if (checkboxes.length === 0) {
            forwardBtn.disabled = true;
            return;
        }
        forwardBtn.disabled = !Array.from(checkboxes).every(cb => cb.checked);
    }

    // Listen for changes on checklist checkboxes
    if (checklistItemsContainer) {
        checklistItemsContainer.addEventListener('change', function (e) {
            if (e.target && e.target.type === 'checkbox') {
                updateRejectButtonState();
            }
        });

        // Remove checklist item logic
        checklistItemsContainer.addEventListener('click', function (e) {
            if (e.target.closest('.checklist-remove-btn')) {
                const btn = e.target.closest('.checklist-remove-btn');
                btn.parentElement.parentElement.remove();
                updateRejectButtonState();
            }
        });
    }

    // Confirm add checklist item
    const confirmAddChecklistBtnElement = document.getElementById('confirmAddChecklistBtn');
    if (confirmAddChecklistBtnElement) {
        confirmAddChecklistBtnElement.addEventListener('click', function () {
            if (!conditionTextInput) {
                conditionTextInput = document.getElementById('conditionTextInput');
            }
            if (!conditionTextInput || !checklistItemsContainer) return;
            
            const text = conditionTextInput.value.trim();
            if (text.length > 0) {
                const id = 'checklist_' + Date.now();
                const div = document.createElement('div');
                div.className = 'form-check mb-2';
                div.innerHTML = `<div class="form-check mb-2 d-flex align-items-center justify-content-between">
                                    <div class="d-flex align-items-center flex-grow-1">
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
                                    </button>
                                </div>`;
                checklistItemsContainer.appendChild(div);
                if (addChecklistItemModal) {
                    addChecklistItemModal.hide();
                }
                updateRejectButtonState();
            } else {
                conditionTextInput.focus();
            }
        });
    }

    // Initial state
    updateRejectButtonState();

    // Pre-populate checklist with default items (similar to BenefitsAssistant)
    if (checklistItemsContainer) {
        const checklistItems = [
            'No existing active loans',
            'Not a co-maker for another active loan',
            'No derogatory legal records (civil/criminal cases)'
        ];

        checklistItems.forEach((item, index) => {
            const id = 'checklist_' + Date.now() + '_' + index;
            const div = document.createElement('div');
            div.className = 'form-check mb-2';
            div.innerHTML = `<div class="form-check mb-2 d-flex align-items-center justify-content-between">
                            <div class="d-flex align-items-center flex-grow-1">
                                <input class="form-check-input" type="checkbox" id="${id}">
                                <label class="form-check-label checklist-item ms-2" for="${id}">${item}</label>
                            </div>
                            <button type="button" class="btn btn-link btn-sm text-danger ms-2 checklist-remove-btn" title="Remove item" style="padding:0 0.25rem;">
                                <svg xmlns="http://www.w3.org/2000/svg" width="1.2em" height="1.2em" fill="none" stroke="currentColor" class="feather feather-trash" viewBox="0 0 24 24">
                                    <polyline points="3 6 5 6 21 6" />
                                    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                                    <path d="M10 11v6" />
                                    <path d="M14 11v6" />
                                    <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                                </svg>
                            </button>
                        </div>`;
            checklistItemsContainer.appendChild(div);
        });
        
        // Update button state after adding default items
        updateRejectButtonState();
    }

    // Forward Application button logic
    if (forwardBtn) {
        forwardBtn.addEventListener('click', function () {
            if (!this.disabled) {
                const loanIdField = document.getElementById('loanIdField');
                const forwardUrlField = document.getElementById('forwardUrlField');
                if (!loanIdField || !forwardUrlField) return;
                
                const loanId = loanIdField.value;
                const forwardUrl = forwardUrlField.value;
                
                // Create a simple forward request - you may want to customize this
                const forwardRequest = {
                    LoanId: parseInt(loanId),
                    Title: "Application Review Completed",
                    Description: "Validation checklist completed - forwarding for next approval phase",
                    Approvers: [] // Add approvers if needed
                };

                fetch(forwardUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                    },
                    body: JSON.stringify(forwardRequest)
                })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        location.reload();
                    } else {
                        alert('Error forwarding application: ' + (data.message || 'Unknown error'));
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    alert('Error forwarding application');
                });
            }
        });
    }

    // Reject Application button logic
    const rejectBtn = document.getElementById('rejectApplicationBtn');
    const rejectCommentContainer = document.getElementById('rejectCommentContainer');
    const confirmRejectBtn = document.getElementById('confirmRejectBtn');
    const rejectCommentBox = document.getElementById('rejectCommentBox');
    const rejectConfirmModalElement = document.getElementById('rejectConfirmModal');
    const rejectCommentPreview = document.getElementById('rejectCommentPreview');

    if (rejectBtn && rejectCommentContainer && confirmRejectBtn && rejectCommentBox && rejectConfirmModalElement && rejectCommentPreview) {
        const rejectConfirmModal = new bootstrap.Modal(rejectConfirmModalElement);

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
        if (finalRejectBtn) {
            finalRejectBtn.addEventListener('click', function () {
                const loanIdField = document.getElementById('loanIdField');
                const rejectUrlField = document.getElementById('rejectUrlField');
                
                if (!loanIdField || !rejectUrlField) return;
                
                const loanId = loanIdField.value;
                const remarks = rejectCommentBox.value.trim();
                const rejectUrl = rejectUrlField.value;

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
        }
    }
});
