// --- GLOBAL SHARED VARIABLES ---
let approversList = [];
let rolesData = [];
let usersData = [];
let selectedFiles = [];
let loanId;

// --- GLOBAL FUNCTIONS (must be outside DOMContentLoaded) ---

window.openModal = async function () {
    const modal = document.getElementById('approverModal');
    modal.style.display = 'block';
    document.body.style.overflow = 'hidden';

    setTimeout(() => {
        modal.classList.add('show');
    }, 10);

    // Load roles and order dropdown
    await loadRoles();
    await loadOrderDropdown();
};

window.closeModal = function () {
    const modal = document.getElementById('approverModal');
    modal.classList.remove('show');

    setTimeout(() => {
        modal.style.display = 'none';
        document.body.style.overflow = 'auto';
    }, 300);

    document.getElementById('roleSelect').value = '';
    document.getElementById('approverSelect').value = '';
    document.getElementById('emailField').value = '';
    document.getElementById('descriptionField').value = '';

    const approverGroup = document.getElementById('approverDropdownGroup');
    approverGroup.classList.remove('show');
};

window.setUploadTarget = function ({ loanId = '', loanApprovalId = '' }) {
    document.getElementById('loanIdFieldForm').value = loanId;
    document.getElementById('loanApprovalIdField').value = loanApprovalId;
};

window.handleRoleChange = async function () {
    const roleId = document.getElementById('roleSelect').value;
    const approverDropdown = document.getElementById('approverSelect');
    const approverGroup = document.getElementById('approverDropdownGroup');
    const emailField = document.getElementById('emailField');

    if (roleId) {
        try {
            approverGroup.style.display = 'block';
            setTimeout(() => {
                approverGroup.classList.add('show');
            }, 10);

            approverDropdown.classList.add('loading');
            approverDropdown.innerHTML = '<option value="">Loading...</option>';

            const response = await fetch(`/BenefitsAssistantApplicationDetails/GetUsersByRole?roleId=${roleId}`);
            usersData = await response.json();

            approverDropdown.innerHTML = '<option value="">Select an approver...</option>';

            usersData.forEach(user => {
                const option = document.createElement('option');
                option.value = user.userId;
                option.textContent = user.name;
                option.dataset.email = user.email;
                approverDropdown.appendChild(option);
            });

            approverDropdown.classList.remove('loading');
            emailField.value = '';
        } catch (error) {
            console.error('Error loading users:', error);
            alert('Error loading users. Please try again.');
            approverDropdown.classList.remove('loading');
        }
    } else {
        approverGroup.classList.remove('show');
        approverDropdown.innerHTML = '<option value="">Select an approver...</option>';
        emailField.value = '';
    }
};

window.handleApproverChange = function () {
    const approverSelect = document.getElementById('approverSelect');
    const emailField = document.getElementById('emailField');
    const selectedOption = approverSelect.options[approverSelect.selectedIndex];

    if (selectedOption.dataset.email) {
        emailField.value = selectedOption.dataset.email;
    } else {
        emailField.value = '';
    }
};

window.saveApprover = async function () {
    const roleId = document.getElementById('roleSelect').value;
    const userId = document.getElementById('approverSelect').value;
    const email = document.getElementById('emailField').value;
    const description = document.getElementById('descriptionField').value;
    const phaseOrder = document.getElementById('phaseOrderField').value;

    if (!roleId || !userId || !email || !phaseOrder) {
        alert('Please fill in all required fields');
        return;
    }

    // Get current order information from server
    try {
        const orderResponse = await fetch(`/BenefitsAssistantApplicationDetails/GetNextPhaseOrder?loanId=${loanId}`);
        const orderResult = await orderResponse.json();
        const databaseUsedOrders = orderResult.usedOrders || [];
        const sessionUsedOrders = approversList.map(approver => approver.order);
        const allUsedOrders = [...databaseUsedOrders, ...sessionUsedOrders];

        // Check if the selected order is already used
        if (allUsedOrders.includes(parseInt(phaseOrder))) {
            alert('This order number is already assigned to another approver. Please select a different order.');
            await loadOrderDropdown();
            return;
        }
    } catch (error) {
        console.error('Error validating order:', error);
        alert('Error validating order number. Please try again.');
        return;
    }

    const saveButton = document.querySelector('.btn-save');
    try {
        saveButton.classList.add('loading');
        saveButton.textContent = 'Saving...';

        // --- Store both file names and File objects ---
        const approverData = {
            userId: parseInt(userId),
            roleName: rolesData.find(r => r.roleId == roleId)?.roleName || '',
            userName: usersData.find(u => u.userId == userId)?.name || '',
            email: email,
            order: parseInt(phaseOrder),
            description: description,
            isSaved: false,
            attachedFiles: selectedFiles.map(f => f.name),
            attachedFileObjects: [...selectedFiles] // <-- Store File objects here
        };

        createApprovalCard(approverData);
        approversList.push(approverData);

        // Clear file selection
        selectedFiles = [];
        document.getElementById('selected-files-list').innerHTML = '';

        closeModal();
        await loadOrderDropdown();
    } finally {
        saveButton.classList.remove('loading');
        saveButton.textContent = 'Save';
    }
};

window.openConfirmationModal = function (loanTitle, messageToApprovers) {
    const modal = document.getElementById('confirmationModal');
    modal.style.display = 'block';
    document.body.style.overflow = 'hidden';

    // Populate confirmation details
    document.getElementById('confirmLoanTitle').textContent = loanTitle || 'No title provided';
    document.getElementById('confirmMessage').textContent = messageToApprovers || 'No message provided';
    document.getElementById('confirmApproversCount').textContent = approversList.length;

    // Populate approvers list
    const approversListElement = document.getElementById('confirmApproversList');
    if (approversList.length > 0) {
        approversListElement.innerHTML = approversList
            .sort((a, b) => a.order - b.order)
            .map(approver => `
                <div class="approver-sequence-item">
                    <div class="approver-order">${approver.order}</div>
                    <div class="approver-info">
                        <div class="approver-name">${approver.userName}</div>
                        <div class="approver-role">${approver.roleName}</div>
                    </div>
                </div>
            `).join('');
    } else {
        approversListElement.innerHTML = '<div class="confirmation-value empty">No approvers selected</div>';
    }

    // --- Populate attached documents ---
    const attachedDocsList = document.getElementById('confirmAttachedDocuments');
    attachedDocsList.innerHTML = '';

    let allFiles = [];
    approversList.forEach(a => {
        if (a.attachedFiles && a.attachedFiles.length > 0) {
            allFiles = allFiles.concat(a.attachedFiles);
        }
    });
    if (allFiles.length > 0) {
        allFiles.forEach(name => {
            const li = document.createElement('li');
            li.textContent = name;
            attachedDocsList.appendChild(li);
        });
    } else {
        attachedDocsList.innerHTML = '<li>No documents attached.</li>';
    }

    setTimeout(() => {
        modal.classList.add('show');
    }, 10);
};

window.closeConfirmationModal = function () {
    const modal = document.getElementById('confirmationModal');
    modal.classList.remove('show');

    setTimeout(() => {
        modal.style.display = 'none';
        document.body.style.overflow = 'auto';
    }, 300);
};

window.proceedWithForward = async function () {
    const loanTitle = document.getElementById('loanTitle').value.trim();
    const messageToApprovers = document.getElementById('messageToApprovers').value.trim();
    const confirmButton = document.getElementById('confirmForwardBtn');
    const originalText = confirmButton.textContent;

    try {
        // Disable the button and show loading state
        confirmButton.disabled = true;
        confirmButton.textContent = 'Forwarding...';

        const approversToForward = approversList.filter(a => (!a.status || a.status === "Pending") && !a.isSaved);

        // 1. Forward the application and get the mapping of new LoanApprovalIds
        const response = await fetch('/BenefitsAssistantApplicationDetails/ForwardApplication', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                loanId: loanId,
                title: loanTitle,
                description: messageToApprovers,
                approvers: approversToForward.map(a => ({
                    userId: a.userId,
                    order: a.order
                }))
            })
        });

        const result = await response.json();

        // 2. Upload attached files for each approver using the returned mapping
        if (result.success && result.approverIds) {
            for (const approver of approversToForward) {
                const files = approver.attachedFileObjects || [];
                if (files.length > 0) {
                    const mapping = result.approverIds.find(x => x.userId === approver.userId && x.order === approver.order);
                    if (mapping && mapping.loanApprovalId) {
                        const formData = new FormData();
                        files.forEach(file => formData.append('pdfFiles', file));
                        formData.append('loanId', loanId);
                        formData.append('loanApprovalId', mapping.loanApprovalId);

                        await fetch('/BenefitsAssistantApplicationDetails/UploadApproverDocuments', {
                            method: 'POST',
                            body: formData
                        });
                    }
                }
            }
        }

        if (result.success) {
            // Close the modal first
            closeConfirmationModal();

            // Show success message
            alert('Application forwarded successfully!');

            // Refresh the page to show updated status
            window.location.reload();
        } else {
            alert(result.message || 'Error forwarding application.');
            // Re-enable the button if there was an error
            confirmButton.disabled = false;
            confirmButton.textContent = originalText;
        }
    } catch (error) {
        console.error('Error forwarding application:', error);
        alert('Error forwarding application. Please try again.');
        // Re-enable the button if there was an error
        confirmButton.disabled = false;
        confirmButton.textContent = originalText;
    }
};

// --- END GLOBAL FUNCTIONS ---

// --- BEGIN DOMContentLoaded LOGIC ---
document.addEventListener('DOMContentLoaded', function () {
    loanId = document.getElementById('loanIdField').value;
    // Initialize approversList from server model if available
    if (window.initialApprovers && Array.isArray(window.initialApprovers)) {
        approversList = window.initialApprovers.map(a => ({ ...a, isSaved: true }));
    }

    // File selection and validation
    document.getElementById('pdfFileInput').addEventListener('change', function () {
        const files = Array.from(this.files);
        const errorDiv = document.getElementById('pdfFileError');
        const fileList = document.getElementById('selected-files-list');
        fileList.innerHTML = '';
        let valid = true;
        selectedFiles = [];

        files.forEach(file => {
            if (file.type !== "application/pdf" && !file.name.toLowerCase().endsWith('.pdf')) {
                valid = false;
            } else {
                selectedFiles.push(file);
                const li = document.createElement('li');
                li.textContent = file.name;
                fileList.appendChild(li);
            }
        });

        if (!valid) {
            errorDiv.textContent = "Only PDF files are allowed.";
            errorDiv.style.display = "block";
            this.value = "";
            selectedFiles = [];
            fileList.innerHTML = '';
        } else {
            errorDiv.textContent = "";
            errorDiv.style.display = "none";
        }
    });

    // Ensure only one of loanId or loanApprovalId is set before submit
    const uploadDocsForm = document.getElementById('uploadDocsForm');
    if (uploadDocsForm) {
        uploadDocsForm.addEventListener('submit', function (e) {
            const loanIdVal = document.getElementById('loanIdFieldForm').value;
            const loanApprovalId = document.getElementById('loanApprovalIdField').value;
            if ((loanIdVal && loanApprovalId) || (!loanIdVal && !loanApprovalId)) {
                e.preventDefault();
                alert('Please select a valid upload target (loan or approver).');
            }
        });
    }

    // Close modal when clicking outside
    document.getElementById('approverModal').addEventListener('click', function (e) {
        if (e.target === this) {
            closeModal();
        }
    });

    // Close modal with Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && document.getElementById('approverModal').classList.contains('show')) {
            closeModal();
        }
    });

    // Forward Application button logic
    const forwardBtn = document.getElementById('forwardApplicationBtn');
    if (forwardBtn) {
        forwardBtn.addEventListener('click', async function () {
            if (approversList.length === 0) {
                alert('No approvers to forward.');
                return;
            }

            // Validate that all approvers have userId
            const invalidApprovers = approversList.filter(a => !a.userId);
            if (invalidApprovers.length > 0) {
                alert('Some approvers are missing user ID information. Please remove and re-add them.');
                return;
            }

            // Get the loan title and message from the form fields
            const loanTitle = document.getElementById('loanTitle').value.trim();
            const messageToApprovers = document.getElementById('messageToApprovers').value.trim();

            // Validation for required fields
            if (!loanTitle) {
                alert('Please enter a loan title before forwarding the application.');
                document.getElementById('loanTitle').focus();
                return;
            }

            if (!messageToApprovers) {
                alert('Please enter a message to approvers before forwarding the application.');
                document.getElementById('messageToApprovers').focus();
                return;
            }

            // Show confirmation modal instead of simple confirm dialog
            openConfirmationModal(loanTitle, messageToApprovers);
        });
    }

    // Confirmation Modal: close on outside click
    const confirmationModal = document.getElementById('confirmationModal');
    if (confirmationModal) {
        confirmationModal.addEventListener('click', function (e) {
            if (e.target === this) {
                closeConfirmationModal();
            }
        });
    }

    // Confirmation Modal: close on Escape
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && document.getElementById('confirmationModal').classList.contains('show')) {
            closeConfirmationModal();
        }
    });

    // jQuery upload PDF logic
    if (window.$) {
        $(document).ready(function () {
            $('#uploadPdfBtn').on('click', function () {
                var files = $('#pdfFileInput')[0].files;
                if (!files.length) {
                    $('#pdfFileError').text('Please select at least one PDF file.').show();
                    return;
                }
                $('#pdfFileError').hide();

                var formData = new FormData();
                for (var i = 0; i < files.length; i++) {
                    formData.append('pdfFiles', files[i]);
                }
                var loanIdVal = $('#loanIdField').val();
                formData.append('loanId', loanIdVal);

                $.ajax({
                    url: '/BenefitsAssistant/BenefitsAssistantApplicationDetails/UploadDocuments',
                    type: 'POST',
                    data: formData,
                    processData: false,
                    contentType: false,
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: function (response) {
                        if (response.success) {
                            // Update the attached documents list
                            var $list = $('#attached-documents-list');
                            $list.empty();
                            response.files.forEach(function (file) {
                                $list.append(
                                    `<li><a href="${file.Url}" target="_blank">${file.Name}</a></li>`
                                );
                            });
                            $('#pdfFileInput').val('');
                        } else {
                            $('#pdfFileError').text(response.message).show();
                        }
                    },
                    error: function () {
                        $('#pdfFileError').text('Error uploading files.').show();
                    }
                });
            });
        });
    }
});

// --- HELPER FUNCTIONS (must be global for modal logic) ---

async function loadOrderDropdown() {
    try {
        const response = await fetch(`/BenefitsAssistantApplicationDetails/GetNextPhaseOrder?loanId=${loanId}`);
        const result = await response.json();
        const dropdown = document.getElementById('phaseOrderField');
        dropdown.innerHTML = '';

        // Get orders from database (already saved approvers)
        const databaseUsedOrders = result.usedOrders || [];

        // Get orders from current session (newly added approvers)
        const sessionUsedOrders = approversList.map(approver => approver.order);

        // Combine both database and session orders
        const allUsedOrders = [...databaseUsedOrders, ...sessionUsedOrders];

        // Generate options from 1 to 7, excluding all used orders
        for (let i = 1; i <= 7; i++) {
            if (!allUsedOrders.includes(i)) {
                const option = document.createElement('option');
                option.value = i;
                option.textContent = i;
                dropdown.appendChild(option);
            }
        }

        // If no options are available, show a message
        if (dropdown.options.length === 0) {
            const option = document.createElement('option');
            option.value = '';
            option.textContent = 'No available order numbers';
            option.disabled = true;
            dropdown.appendChild(option);
            dropdown.disabled = true;
        } else {
            dropdown.disabled = false;
        }

        // Add visual feedback
        dropdown.classList.add('order-updated');
        setTimeout(() => {
            dropdown.classList.remove('order-updated');
        }, 300);

        // Update the order info display
        updateOrderDropdownStyle(allUsedOrders);
    } catch (error) {
        console.error('Error loading order:', error);
        const dropdown = document.getElementById('phaseOrderField');
        dropdown.innerHTML = '<option value="1">1</option>';
    }
}

function updateOrderDropdownStyle(allUsedOrders = null) {
    const dropdown = document.getElementById('phaseOrderField');

    // If no orders provided, calculate them
    if (allUsedOrders === null) {
        // This is for when we call it from other places - we need to get database orders
        // For now, just use session orders
        allUsedOrders = approversList.map(approver => approver.order);
    }

    // Remove existing order info
    const existingOrderInfo = document.getElementById('orderInfo');
    if (existingOrderInfo) {
        existingOrderInfo.remove();
    }

    // Add order group styling
    const orderGroup = dropdown.closest('.form-group');
    if (orderGroup) {
        orderGroup.classList.add('order-selection');
    }

    // Add visual indicator showing which orders are used
    if (allUsedOrders.length > 0) {
        const infoDiv = document.createElement('div');
        infoDiv.id = 'orderInfo';
        infoDiv.className = 'order-info';
        infoDiv.innerHTML = `
            <small class="text-muted">
                <i class="fas fa-info-circle"></i> 
                Used order numbers: ${allUsedOrders.sort((a, b) => a - b).join(', ')}
            </small>
        `;
        dropdown.parentNode.appendChild(infoDiv);
    }
}

async function loadRoles() {
    try {
        const roleSelect = document.getElementById('roleSelect');
        roleSelect.classList.add('loading');

        const response = await fetch('/BenefitsAssistantApplicationDetails/GetRoles');
        rolesData = await response.json();

        roleSelect.innerHTML = '<option value="">Select a role...</option>';

        // Filter out unwanted roles
        const excludedRoles = ['Employee', 'Benefits Assistant', 'Admin'];

        rolesData.forEach(role => {
            // Check if the role name contains any of the excluded terms
            const shouldExclude = excludedRoles.some(excludedRole =>
                role.roleName.includes(excludedRole)
            );

            // Only add the role if it's not excluded
            if (!shouldExclude) {
                const option = document.createElement('option');
                option.value = role.roleId;
                option.textContent = role.roleName;
                roleSelect.appendChild(option);
            }
        });

        roleSelect.classList.remove('loading');
    } catch (error) {
        console.error('Error loading roles:', error);
        alert('Error loading roles. Please try again.');
    }
}

function createApprovalCard(approverData) {
    const container = document.getElementById('approvalCardsContainer');
    const card = document.createElement('div');
    card.className = 'approval-card';
    card.dataset.userName = approverData.userName;
    card.dataset.order = approverData.order;
    card.dataset.roleName = approverData.roleName;

    card.innerHTML = `
        <div class="approval-card-title">${approverData.roleName}</div>
        <div class="approval-card-subtitle">${approverData.description || 'No description provided'}</div>
        <div class="approval-card-content">
            <div><strong>User:</strong> ${approverData.userName}</div>
            <div><strong>Order:</strong> ${approverData.order}</div>
            <div><strong>Department:</strong> ${approverData.roleName}</div>
        </div>
        <button type="button" class="btn btn-outline-danger btn-sm mt-2 remove-approver-btn">
            Remove
        </button>
    `;

    // Attach remove event
    card.querySelector('.remove-approver-btn').addEventListener('click', function () {
        removeApprovalCard(card, approverData);
    });

    // Insert the card in the correct position based on order
    const existingCards = Array.from(container.children);
    let insertPosition = existingCards.length;

    for (let i = 0; i < existingCards.length; i++) {
        const existingOrder = parseInt(existingCards[i].dataset.order);
        if (approverData.order < existingOrder) {
            insertPosition = i;
            break;
        }
    }

    if (insertPosition === existingCards.length) {
        container.appendChild(card);
    } else {
        container.insertBefore(card, existingCards[insertPosition]);
    }
}

function openPdfPreview(url, title) {
    document.getElementById('pdfPreviewFrame').src = url;
    document.getElementById('pdfPreviewTitle').textContent = title || '';
    document.getElementById('pdfPreviewModal').style.display = 'block';
    setTimeout(() => {
        document.getElementById('pdfPreviewModal').classList.add('show');
    }, 10);
    document.body.style.overflow = 'hidden';
}

function closePdfPreview() {
    document.getElementById('pdfPreviewModal').classList.remove('show');
    setTimeout(() => {
        document.getElementById('pdfPreviewModal').style.display = 'none';
        document.getElementById('pdfPreviewFrame').src = '';
        document.body.style.overflow = 'auto';
    }, 300);
}

function removeApprovalCard(cardElement, approverData) {
    // Remove from DOM
    cardElement.remove();

    // Remove from approversList
    approversList = approversList.filter(item =>
        !(item.userName === approverData.userName &&
            item.order === approverData.order &&
            item.roleName === approverData.roleName)
    );

    // Refresh the order dropdown to make the removed order available again
    // Only refresh if the modal is currently open
    if (document.getElementById('approverModal').classList.contains('show')) {
        loadOrderDropdown();
    }
}