// wwwroot/js/profile.js
document.addEventListener('DOMContentLoaded', function () {
    const avatarUploadInput = document.getElementById('avatarUploadInput');
    const avatarPreview = document.getElementById('avatarPreview');
    const avatarSizeError = document.getElementById('avatarSizeError');
    const avatarTypeError = document.getElementById('avatarTypeError');
    const maxSize = 3 * 1024 * 1024; // 3MB in bytes
    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif'];

    if (avatarUploadInput && avatarPreview) {
        avatarUploadInput.addEventListener('change', function (event) {
            avatarSizeError.style.display = 'none'; // Reset errors
            avatarTypeError.style.display = 'none';

            const file = event.target.files[0];
            if (file) {
                // Check size
                if (file.size > maxSize) {
                    avatarSizeError.style.display = 'block';
                    avatarUploadInput.value = ''; // Clear selection
                    avatarPreview.src = avatarPreview.dataset.originalSrc || '/images/default-avatar.png'; // Revert preview
                    return;
                }

                // Check type
                if (!allowedTypes.includes(file.type.toLowerCase())) {
                     avatarTypeError.style.display = 'block';
                     avatarUploadInput.value = ''; // Clear selection
                     avatarPreview.src = avatarPreview.dataset.originalSrc || '/images/default-avatar.png'; // Revert preview
                     return;
                }


                // Show preview
                const reader = new FileReader();
                reader.onload = function (e) {
                    // Store original src if not already stored
                    if (!avatarPreview.dataset.originalSrc) {
                        avatarPreview.dataset.originalSrc = avatarPreview.src;
                    }
                    avatarPreview.src = e.target.result;
                }
                reader.readAsDataURL(file);
            } else {
                 // No file selected, revert preview if needed
                 avatarPreview.src = avatarPreview.dataset.originalSrc || '/images/default-avatar.png';
            }
        });
    }
});