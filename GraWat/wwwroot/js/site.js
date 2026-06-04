// Global AJAX "Add to Cart" Handler using SweetAlert2
$(document).on("click", ".sepete-ekle-btn", function (e) {
    e.preventDefault();
    var urunId = $(this).data("id");
    var button = $(this);

    // Disable button during the request to prevent double clicks
    button.prop("disabled", true);

    $.ajax({
        url: "/Sepet/Ekle/" + urunId,
        type: "POST",
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        success: function (response) {
            button.prop("disabled", false);
            if (response.success) {
                // Play SweetAlert2 Toast success message
                const Toast = Swal.mixin({
                    toast: true,
                    position: 'top-end',
                    showConfirmButton: false,
                    timer: 2500,
                    timerProgressBar: true,
                    customClass: {
                        popup: 'navbar-under-toast'
                    },
                    didOpen: (toast) => {
                        toast.addEventListener('mouseenter', Swal.stopTimer);
                        toast.addEventListener('mouseleave', Swal.resumeTimer);
                    }
                });

                Toast.fire({
                    icon: 'success',
                    title: 'Ürün başarıyla sepete eklendi!'
                });

                // Update Sepet Badge
                $("#sepet-badge").text(response.cartCount);

                // Button success animation
                var originalHtml = button.html();
                var originalBg = button.css("background-color");
                var originalColor = button.css("color");
                var originalBorder = button.css("border");

                button.css({
                    "background-color": "#28a745",
                    "color": "#ffffff",
                    "border": "none"
                });
                button.html(button.hasClass("btn-lg") ? "<i class='bi bi-check-lg'></i> Eklendi" : "<i class='bi bi-check-lg fs-4'></i>");

                setTimeout(function () {
                    button.css({
                        "background-color": originalBg,
                        "color": originalColor,
                        "border": originalBorder
                    });
                    button.html(originalHtml);
                }, 1500);

            } else {
                if (response.redirectUrl) {
                    Swal.fire({
                        title: 'Giriş Gerekli',
                        text: response.message,
                        icon: 'warning',
                        showCancelButton: true,
                        confirmButtonColor: '#3b0f42',
                        cancelButtonColor: '#d33',
                        confirmButtonText: 'Giriş Yap',
                        cancelButtonText: 'İptal'
                    }).then((result) => {
                        if (result.isConfirmed) {
                            window.location.href = response.redirectUrl;
                        }
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Hata',
                        text: response.message
                    });
                }
            }
        },
        error: function (xhr, status, error) {
            button.prop("disabled", false);
            if (xhr.status === 401) {
                Swal.fire({
                    title: 'Giriş Gerekli',
                    text: 'Sepete ürün eklemek için lütfen giriş yapın.',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#3b0f42',
                    cancelButtonColor: '#d33',
                    confirmButtonText: 'Giriş Yap',
                    cancelButtonText: 'İptal'
                }).then((result) => {
                    if (result.isConfirmed) {
                        window.location.href = "/Identity/Account/Login";
                    }
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Sistem Hatası',
                    text: 'İşlem gerçekleştirilemedi.'
                });
            }
        }
    });
});

// Global AJAX "Toggle Favorite" Handler using Fetch API and SweetAlert2
$(document).on("click", ".favori-toggle-btn", function (e) {
    e.preventDefault();
    var button = $(this);
    var urunId = button.data("id");
    var icon = button.find("i");

    // Disable button to prevent double-click issues
    button.prop("disabled", true);

    fetch("/Profil/ToggleFavorite/" + urunId, {
        method: "POST",
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        }
    })
    .then(response => {
        if (!response.ok) {
            throw new Error("HTTP status " + response.status);
        }
        return response.json();
    })
    .then(data => {
        button.prop("disabled", false);
        if (data.success) {
            // Update icon style
            if (data.isAdded) {
                icon.removeClass("bi-heart text-dark").addClass("bi-heart-fill text-danger");
            } else {
                icon.removeClass("bi-heart-fill text-danger").addClass("bi-heart text-dark");
            }

            // Update badge count
            $("#favori-badge").text(data.favoritesCount);

            // Play SweetAlert2 Toast success message
            const Toast = Swal.mixin({
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 2000,
                timerProgressBar: true,
                customClass: {
                    popup: 'navbar-under-toast'
                },
                didOpen: (toast) => {
                    toast.addEventListener('mouseenter', Swal.stopTimer);
                    toast.addEventListener('mouseleave', Swal.resumeTimer);
                }
            });

            Toast.fire({
                icon: 'success',
                title: data.message
            });
        } else {
            // If redirect is needed (not logged in)
            if (data.redirectUrl) {
                Swal.fire({
                    title: 'Giriş Gerekli',
                    text: data.message,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#3b0f42',
                    cancelButtonColor: '#d33',
                    confirmButtonText: 'Giriş Yap',
                    cancelButtonText: 'İptal'
                }).then((result) => {
                    if (result.isConfirmed) {
                        window.location.href = data.redirectUrl;
                    }
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Hata',
                    text: data.message
                });
            }
        }
    })
    .catch(error => {
        button.prop("disabled", false);
        console.error("Favori işlemi hatası:", error);
        Swal.fire({
            icon: 'error',
            title: 'Sistem Hatası',
            text: 'İşlem gerçekleştirilemedi.'
        });
    });
});
