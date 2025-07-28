document.addEventListener('DOMContentLoaded', function () {
    const sidebarToggle = document.getElementById('sidebar-toggle');
    const sidebarWrapper = document.getElementById('sidebar-wrapper');
    const bodyElement = document.body;

    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function (e) {
            e.preventDefault();
            sidebarWrapper.classList.toggle('collapsed');
            bodyElement.classList.toggle('sidebar-collapsed');

            const icon = sidebarToggle.querySelector('i');
            if (sidebarWrapper.classList.contains('collapsed')) {
                icon.classList.remove('bi-chevron-left');
                icon.classList.add('bi-chevron-right');
            } else {
                icon.classList.remove('bi-chevron-right');
                icon.classList.add('bi-chevron-left');
            }
        });
    }

    // Handle mobile responsiveness
    function checkWidth() {
        if (window.innerWidth < 768) {
            sidebarWrapper.classList.add('collapsed');
            bodyElement.classList.add('sidebar-collapsed');
            const icon = sidebarToggle.querySelector('i');
            icon.classList.remove('bi-chevron-left');
            icon.classList.add('bi-chevron-right');
        }
    }

    // Check width on page load
    checkWidth();

    // Check width on resize
    window.addEventListener('resize', checkWidth);
});