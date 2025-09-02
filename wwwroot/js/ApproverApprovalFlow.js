// --- READ-ONLY APPROVAL FLOW VIEW ---
// This file provides read-only functionality for the Approver's view of the approval flow

document.addEventListener('DOMContentLoaded', function () {
    // Initialize PDF preview functionality
    initializePdfPreview();
});

function initializePdfPreview() {
    // Add event listeners for PDF preview links
    document.addEventListener('click', function(e) {
        if (e.target.closest('[onclick*="openPdfPreview"]')) {
            e.preventDefault();
            const element = e.target.closest('[onclick*="openPdfPreview"]');
            const onclick = element.getAttribute('onclick');
            
            // Extract URL and title from onclick attribute
            const urlMatch = onclick.match(/openPdfPreview\('([^']+)',\s*'([^']*)'\)/);
            if (urlMatch) {
                openPdfPreview(urlMatch[1], urlMatch[2]);
            }
        }
    });
}

function openPdfPreview(url, title) {
    const modal = document.getElementById('pdfPreviewModal');
    const frame = document.getElementById('pdfPreviewFrame');
    const titleElement = document.getElementById('pdfPreviewTitle');
    
    titleElement.textContent = title || 'Document Preview';
    frame.src = url;
    modal.style.display = 'block';
    document.body.style.overflow = 'hidden';
    
    setTimeout(() => {
        modal.classList.add('show');
    }, 10);
}

function closePdfPreview() {
    const modal = document.getElementById('pdfPreviewModal');
    modal.classList.remove('show');
    
    setTimeout(() => {
        modal.style.display = 'none';
        document.body.style.overflow = '';
        document.getElementById('pdfPreviewFrame').src = '';
    }, 300);
}

// Make functions global for compatibility
window.openPdfPreview = openPdfPreview;
window.closePdfPreview = closePdfPreview;
