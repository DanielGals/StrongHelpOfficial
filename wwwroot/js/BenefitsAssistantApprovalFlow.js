document.addEventListener('DOMContentLoaded', function () {
    // Add Approver Modal Functionality
    let approversList = [];
    let rolesData = [];
    let usersData = [];
    const loanId = document.getElementById('loanIdField').value;

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
    }

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
    }

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
    }

    window.handleApproverChange = function () {
        const approverSelect = document.getElementById('approverSelect');
        const emailField = document.getElementById('emailField');
        const selectedOption = approverSelect.options[approverSelect.selectedIndex];

        if (selectedOption.dataset.email) {
            emailField.value = selectedOption.dataset.email;
        } else {
            emailField.value = '';
        }
    }

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
                // Refresh the dropdown to show current available orders
                await loadOrderDropdown();
                return;
            }
        } catch (error) {
            console.error('Error validating order:', error);
            alert('Error validating order number. Please try again.');
            return;
        }

        try {
            const saveButton = document.querySelector('.btn-save');
            saveButton.classList.add('loading');
            saveButton.textContent = 'Saving...';

            const response = await fetch('/BenefitsAssistantApplicationDetails/SaveApprover', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    loanId: loanId,
                    roleId: parseInt(roleId),
                    userId: parseInt(userId),
                    phaseOrder: parseInt(phaseOrder),
                    description: description
                })
            });

            const result = await response.json();

            if (result.success) {
                createApprovalCard(result.approverData);
                approversList.push(result.approverData); // Save to local list
                closeModal();
                await loadOrderDropdown(); // Refresh the dropdown to remove the used order
            } else {
                alert(result.message);
            }

            saveButton.classList.remove('loading');
            saveButton.textContent = 'Save';
        } catch (error) {
            console.error('Error saving approver:', error);
            alert('Error saving approver. Please try again.');
            saveButton.classList.remove('loading');
            saveButton.textContent = 'Save';
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

    // Checklist functionality - Same as in BenefitsAssistantApplicationDetails.js
    // This is duplicate code but might be needed in both places
    // ...

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

    // Confirmation Modal Functions
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

        setTimeout(() => {
            modal.classList.add('show');
        }, 10);
    }

    window.closeConfirmationModal = function () {
        const modal = document.getElementById('confirmationModal');
        modal.classList.remove('show');

        setTimeout(() => {
            modal.style.display = 'none';
            document.body.style.overflow = 'auto';
        }, 300);
    }

    // Forward application action
    window.proceedWithForward = async function () {
        const loanTitle = document.getElementById('loanTitle').value.trim();
        const messageToApprovers = document.getElementById('messageToApprovers').value.trim();
        const confirmButton = document.getElementById('confirmForwardBtn');
        const originalText = confirmButton.textContent;

        try {
            // Disable the button and show loading state
            confirmButton.disabled = true;
            confirmButton.textContent = 'Forwarding...';

            const response = await fetch('/BenefitsAssistantApplicationDetails/ForwardApplication', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    loanId: loanId,
                    title: loanTitle,
                    description: messageToApprovers,
                    approvers: approversList.map(a => ({
                        userId: a.userId,
                        order: a.order
                    }))
                })
            });

            const result = await response.json();

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
    }

    // Close confirmation modal when clicking outside
    const confirmationModal = document.getElementById('confirmationModal');
    if (confirmationModal) {
        confirmationModal.addEventListener('click', function (e) {
            if (e.target === this) {
                closeConfirmationModal();
            }
        });
    }

    // Close confirmation modal with Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && document.getElementById('confirmationModal').classList.contains('show')) {
            closeConfirmationModal();
        }
    });
});
