document.addEventListener('DOMContentLoaded', function () {
    // Search functionality
    const searchInput = document.getElementById('searchLogs');
    searchInput.addEventListener('keyup', function () {
        const searchTerm = this.value.toLowerCase();
        const rows = document.querySelectorAll('.table tbody tr');

        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            row.style.display = text.includes(searchTerm) ? '' : 'none';
        });
    });

    // Status dropdown functionality
    const filterForm = document.getElementById('statusFilterForm');
    const filterByInput = document.getElementById('filterByInput');
    const statusDropdown = document.getElementById('statusDropdown');

    // Add event listeners for dropdown items
    document.querySelectorAll('.dropdown-item').forEach(item => {
        item.addEventListener('click', function (e) {
            e.preventDefault();

            // Update displayed text
            const value = this.getAttribute('data-value');
            const displayText = this.textContent.trim();
            statusDropdown.querySelector('span').textContent = displayText;

            // Update form input
            filterByInput.value = value;

            // Submit form
            filterForm.submit();
        });
    });

    // Date filter form submission
    const dateFilterForm = document.getElementById('dateFilterForm');
    dateFilterForm.addEventListener('submit', function (e) {
        const dateInput = document.getElementById('filterDate');
        // Only submit if a date is selected
        if (!dateInput.value) {
            e.preventDefault();
            showToast('Please select a date before applying the filter.');
        }
    });

    // Initialize Bootstrap toast
    const toastElement = document.getElementById('filterToast');
    const toast = new bootstrap.Toast(toastElement);

    // Check if we need to show a toast notification for filtering with no results
    const showFilterNoResultsToast = window.showFilterNoResultsToast || false;
    if (showFilterNoResultsToast) {
        showToast('No logs found for the selected filters.');
    }

    // Function to show toast messages
    function showToast(message) {
        document.getElementById('toastMessage').textContent = message;
        toast.show();
    }

    // Expose the showToast function globally for Razor to use
    window.showLogsToast = showToast;
});
