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

// jQuery Validation Override and Translation for Turkish Locale
if (typeof $.validator !== 'undefined') {
    // 1. Türkçe Hata Mesajları Çevirisi
    jQuery.extend(jQuery.validator.messages, {
        required: "Bu alanın doldurulması zorunludur.",
        remote: "Lütfen bu alanı düzeltin.",
        email: "Lütfen geçerli bir e-posta adresi giriniz.",
        url: "Lütfen geçerli bir web adresi (URL) giriniz.",
        date: "Lütfen geçerli bir tarih giriniz.",
        dateISO: "Lütfen geçerli bir tarih giriniz (ISO formatında).",
        number: "Lütfen geçerli bir sayı giriniz.",
        digits: "Lütfen sadece sayısal karakterler giriniz.",
        creditcard: "Lütfen geçerli bir kredi kartı numarası giriniz.",
        equalTo: "Lütfen aynı değeri tekrar giriniz.",
        extension: "Lütfen geçerli uzantıya sahip bir dosya seçiniz.",
        maxlength: jQuery.validator.format("Lütfen en fazla {0} karakter uzunluğunda bir değer giriniz."),
        minlength: jQuery.validator.format("Lütfen en az {0} karakter uzunluğunda bir değer giriniz."),
        rangelength: jQuery.validator.format("Lütfen en az {0} ve en fazla {1} karakter uzunluğunda bir değer giriniz."),
        range: jQuery.validator.format("Lütfen {0} ile {1} arasında bir değer giriniz."),
        max: jQuery.validator.format("Lütfen {0} değerine eşit veya daha küçük bir değer giriniz."),
        min: jQuery.validator.format("Lütfen {0} değerine eşit veya daha büyük bir değer giriniz.")
    });

    // 2. Ondalık Ayracı Olarak Virgül Desteği (Decimal/Number Parser Override)
    $.validator.methods.number = function (value, element) {
        return this.optional(element) || /^-?(?:\d+|\d{1,3}(?:[\s\.,]\d{3})+)(?:[\.,]\d+)?$/.test(value);
    };
    $.validator.methods.range = function (value, element, param) {
        var globalizedValue = value.replace(",", ".");
        return this.optional(element) || (globalizedValue >= param[0] && globalizedValue <= param[1]);
    };
}


