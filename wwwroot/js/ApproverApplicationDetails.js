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

    console.log('Application Details page loaded - Document preview functionality only');
});
