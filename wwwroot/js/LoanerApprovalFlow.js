document.addEventListener('DOMContentLoaded', function () {
    const loanId = document.getElementById('loanIdField').value;
    console.log("Loan ID:", loanId); //debug

    // Load approvers for this loan
    loadApprovers();

    async function loadApprovers() {
        try {
            // Correct the URL to include the area and controller
            const response = await fetch(`/Loaner/MyApplication/GetApprovers?loanId=${loanId}`);
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            const approvers = await response.json();
            console.log("Approvers data:", approvers);//debug

            if (approvers && approvers.length > 0) {
                // Display the approvers in the container
                displayApprovers(approvers);
            } else {
                // Show a message when no approvers are found
                document.getElementById('approvalCardsContainer').innerHTML = `
                    <div class="alert alert-info">
                        No approvers have been assigned to this application yet.
                    </div>
                `;
            }
        } catch (error) {
            console.error('Error loading approvers:', error);
            document.getElementById('approvalCardsContainer').innerHTML = `
                <div class="alert alert-danger">
                    <strong>Error:</strong> Unable to load approver information. Please try again later.
                </div>
            `;
        }
    }

    function displayApprovers(approvers) {
        const container = document.getElementById('approvalCardsContainer');
        container.innerHTML = '';

        // Sort approvers by order number
        approvers.sort((a, b) => a.order - b.order);

        approvers.forEach(approver => {
            const card = document.createElement('div');
            card.className = 'approval-card';

            // Determine the status icon to show
            let statusDot = '';
            
            // Special handling for Benefits Assistant - always show as reviewed (approved) with check mark
            if (approver.status === 'Approved' || 
                (approver.roleName === "Benefits Assistant" && approver.status === "Reviewed")) {
                statusDot = `
                    <span class="status-dot approved">
                        <svg viewBox="0 0 16 16"><polyline points="12 5 7 10 4 7" /></svg>
                    </span>
                `;
            } else if (approver.status === 'Rejected') {
                statusDot = `
                    <span class="status-dot rejected">
                        <svg viewBox="0 0 16 16"><line x1="5" y1="5" x2="11" y2="11"></line><line x1="11" y1="5" x2="5" y2="11"></line></svg>
                    </span>
                `;
            } else {
                // Pending or any other status
                statusDot = `
                    <span class="status-dot pending">
                        <svg viewBox="0 0 16 16"><circle cx="8" cy="8" r="6" /><path d="M8 4v4l2 2" /></svg>
                    </span>
                `;
            }

            // Special handling for the Benefits Assistant department
            const department = approver.roleName === "Benefits Assistant" ?
                "Benefits Assistant" : approver.roleName;
                
            // Display status-based subtitle instead of comment
            let subtitle = '';
            if (approver.roleName === "Benefits Assistant") {
                subtitle = 'Application reviewed and forwarded to approvers';
            } else if (approver.status === 'Approved') {
                subtitle = `Approved by ${approver.userName}`;
            } else if (approver.status === 'Rejected') {
                subtitle = `Rejected by ${approver.userName}`;
            } else {
                subtitle = `Awaiting review by ${approver.userName}`;
            }

            card.innerHTML = `
                <div class="approval-card-title">
                    ${statusDot}
                    ${approver.roleName}
                </div>
                <div class="approval-card-subtitle">${subtitle}</div>
                <div class="approval-card-content">
                    <div><strong>User:</strong> ${approver.userName}</div>
                    ${approver.roleName !== "Benefits Assistant" ? 
                        `<div><strong>Approver Order:</strong> ${approver.order}</div>` : ''}
                    <div><strong>Department:</strong> ${department}</div>
                </div>
            `;

            container.appendChild(card);
        });
    }
});
