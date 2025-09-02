document.addEventListener('DOMContentLoaded', function () {
    // Ensure Bootstrap is loaded
    if (typeof bootstrap === 'undefined') {
        console.error('Bootstrap is not loaded. Make sure bootstrap.bundle.min.js is included.');
        return;
    }

    // Document preview (only if modal exists)
    const docPreviewModalElement = document.getElementById('docPreviewModal');
    if (docPreviewModalElement) {
        const previewModal = new bootstrap.Modal(docPreviewModalElement);
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
    }

    // Checklist add item modal logic
    const addItemBtn = document.getElementById('addItemBtn');
    const checklistItemsContainer = document.getElementById('checklistItemsContainer');
    const approveBtn = document.getElementById('approveApplicationBtn');

    let addChecklistItemModal = null;
    let confirmAddChecklistBtn = null;
    let conditionTextInput = null;

    if (addItemBtn) {
        console.log('Add Item Button found, setting up event listener');
        addItemBtn.addEventListener('click', function () {
            console.log('Add Item Button clicked');
            if (!addChecklistItemModal) {
                const modalElement = document.getElementById('addChecklistItemModal');
                if (!modalElement) {
                    console.error('Add Checklist Item Modal not found in DOM');
                    return;
                }
                addChecklistItemModal = new bootstrap.Modal(modalElement);
                confirmAddChecklistBtn = document.getElementById('confirmAddChecklistBtn');
                conditionTextInput = document.getElementById('conditionTextInput');
                
                if (!confirmAddChecklistBtn) {
                    console.error('Confirm Add Checklist Button not found');
                }
                if (!conditionTextInput) {
                    console.error('Condition Text Input not found');
                }
            }
            if (conditionTextInput) {
                conditionTextInput.value = '';
            }
            if (addChecklistItemModal) {
                addChecklistItemModal.show();
            }
        });
    } else {
        console.log('Add Item Button not found - this is expected if Application Status is not "Submitted"');
    }

    // Function to check if all checklist checkboxes are checked
    function updateRejectButtonState() {
        if (!checklistItemsContainer || !approveBtn) {
            console.log('Missing elements for button state update:', {
                checklistContainer: !!checklistItemsContainer,
                approveBtn: !!approveBtn
            });
            return;
        }
        
        const checkboxes = checklistItemsContainer.querySelectorAll('input[type="checkbox"]');
        console.log('Found checkboxes:', checkboxes.length);
        
        if (checkboxes.length === 0) {
            approveBtn.disabled = true;
            console.log('No checkboxes found, approve button disabled');
            return;
        }
        
        const allChecked = Array.from(checkboxes).every(cb => cb.checked);
        approveBtn.disabled = !allChecked;
        console.log('All checkboxes checked:', allChecked, 'Approve button disabled:', approveBtn.disabled);
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
        console.log('Confirm Add Checklist Button found, setting up event listener');
        confirmAddChecklistBtnElement.addEventListener('click', function () {
            console.log('Confirm Add Checklist Button clicked');
            if (!conditionTextInput) {
                conditionTextInput = document.getElementById('conditionTextInput');
                console.log('Re-queried condition text input:', !!conditionTextInput);
            }
            if (!conditionTextInput || !checklistItemsContainer) {
                console.error('Missing required elements:', {
                    conditionTextInput: !!conditionTextInput,
                    checklistItemsContainer: !!checklistItemsContainer
                });
                return;
            }
            
            const text = conditionTextInput.value.trim();
            console.log('Input text:', text);
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
                console.log('Added new checklist item:', text);
                if (addChecklistItemModal) {
                    addChecklistItemModal.hide();
                }
                updateRejectButtonState();
            } else {
                console.log('Empty text, focusing input');
                conditionTextInput.focus();
            }
        });
    } else {
        console.log('Confirm Add Checklist Button not found - this is expected if Application Status is not "Submitted"');
    }

    // Initial state
    updateRejectButtonState();

    // Pre-populate checklist with default items (only if container exists and is empty)
    if (checklistItemsContainer) {
        console.log('Checklist Items Container found');
        
        // Only add default items if the container is empty
        if (checklistItemsContainer.children.length === 0) {
            console.log('Adding default checklist items');
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
            console.log('Default checklist items added');
        } else {
            console.log('Checklist container already has items, skipping default items');
        }
        
        // Update button state after adding default items
        updateRejectButtonState();
    } else {
        console.log('Checklist Items Container not found - this is expected if Application Status is not "Submitted"');
    }

    // Approve Application button logic
    if (approveBtn) {
        approveBtn.addEventListener('click', function () {
            if (!this.disabled) {
                const loanIdField = document.getElementById('loanIdField');
                const approveUrlField = document.getElementById('approveUrlField');
                if (!loanIdField || !approveUrlField) return;
                
                const loanId = loanIdField.value;
                const approveUrl = approveUrlField.value;
                
                // Create an approval request
                const approveRequest = {
                    LoanId: parseInt(loanId),
                    Title: "Application Approved",
                    Description: "Validation checklist completed - application approved by approver"
                };

                // Disable button during request
                this.disabled = true;
                this.textContent = 'Processing...';

                fetch(approveUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                    },
                    body: JSON.stringify(approveRequest)
                })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        alert('Application approved successfully!');
                        location.reload();
                    } else {
                        alert('Error approving application: ' + (data.message || 'Unknown error'));
                        // Re-enable button on error
                        this.disabled = false;
                        this.textContent = 'Approve Application';
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    alert('Error approving application');
                    // Re-enable button on error
                    this.disabled = false;
                    this.textContent = 'Approve Application';
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
