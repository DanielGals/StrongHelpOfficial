// wwwroot/js/BenefitsAssistantLoanInformation.js
document.addEventListener('DOMContentLoaded', function () {
    const originalDocuments = [];
    const documentChanges = {
        added: [],
        deleted: []
    };

    document.querySelectorAll('.loan-details-document').forEach(doc => {
        originalDocuments.push(doc.dataset.document);
    });

    function handleEditInformation() {
        document.querySelectorAll('.delete-doc-btn').forEach(btn => {
            btn.style.display = 'block';
        });

        const addDocBtn = document.getElementById('addDocumentBtn');
        addDocBtn.style.display = 'inline-flex';

        const saveBtn = document.createElement('button');
        saveBtn.type = 'button';
        saveBtn.className = 'save-changes-btn';
        saveBtn.innerHTML = '<i class="bi bi-check"></i> Save Changes';
        saveBtn.id = 'saveChangesBtn';

        const buttonContainer = this.parentNode;
        buttonContainer.replaceChild(saveBtn, this);

        saveBtn.addEventListener('click', handleSaveChanges);
    }

    function handleSaveChanges() {
        const documentList = document.getElementById('documentList');
        const currentDocuments = [];
        document.querySelectorAll('.loan-details-document:not(.is-deleted) .doc-title').forEach(el => {
            currentDocuments.push(el.textContent);
        });

        fetch('/BenefitsAssistant/UpdateLoanInformation', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({ requiredDocuments: currentDocuments })
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    document.querySelectorAll('.delete-doc-btn').forEach(btn => {
                        btn.style.display = 'none';
                    });

                    const addDocBtn = document.getElementById('addDocumentBtn');
                    addDocBtn.style.display = 'none';

                    const editBtn = document.createElement('button');
                    editBtn.type = 'button';
                    editBtn.className = 'btn btn-primary-action';
                    editBtn.innerHTML = '<i class="bi bi-pencil"></i> Edit Information';
                    editBtn.id = 'editInfoBtn';

                    const buttonContainer = this.parentNode;
                    buttonContainer.replaceChild(editBtn, this);

                    editBtn.addEventListener('click', handleEditInformation);

                    const alertDiv = document.createElement('div');
                    alertDiv.className = 'alert alert-success mt-3';
                    alertDiv.textContent = 'Document list updated successfully';
                    documentList.parentNode.insertBefore(alertDiv, documentList.nextSibling);

                    setTimeout(() => {
                        alertDiv.remove();
                    }, 3000);
                } else {
                    alert(data.message || 'Error saving changes');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('Failed to save changes');
            });
    }

    function handleAddDocument() {
        const documentName = prompt('Enter document name:');
        if (documentName && documentName.trim() !== '') {
            const newDoc = document.createElement('div');
            newDoc.className = 'loan-details-document mb-3';
            newDoc.dataset.document = documentName.trim();
            newDoc.innerHTML = `
                <div>
                    <div class="doc-title">${documentName.trim()}</div>
                    <div class="doc-type">Required Document</div>
                </div>
                <button type="button" class="delete-doc-btn"><i class="bi bi-trash"></i></button>
            `;

            documentChanges.added.push(documentName.trim());

            const deleteBtn = newDoc.querySelector('.delete-doc-btn');
            deleteBtn.addEventListener('click', function () {
                newDoc.classList.add('is-deleted');
                const docIndex = documentChanges.added.indexOf(documentName.trim());
                if (docIndex !== -1) {
                    documentChanges.added.splice(docIndex, 1);
                } else {
                    documentChanges.deleted.push(documentName.trim());
                }
            });

            const documentList = document.getElementById('documentList');
            documentList.appendChild(newDoc);
        }
    }

    function initializeDeleteButtons() {
        document.querySelectorAll('.delete-doc-btn').forEach(button => {
            button.addEventListener('click', function () {
                const docElement = button.closest('.loan-details-document');
                const docName = docElement.dataset.document;
                docElement.classList.add('is-deleted');
                documentChanges.deleted.push(docName);
            });
        });
    }

    const editInfoBtn = document.getElementById('editInfoBtn');
    editInfoBtn.addEventListener('click', handleEditInformation);

    const addDocumentBtn = document.getElementById('addDocumentBtn');
    addDocumentBtn.addEventListener('click', handleAddDocument);

    initializeDeleteButtons();
});
