(function ($) {
    "use strict";

    var $modal = $("#personelModal");
    var $title = $("#personelModalTitle");
    var $body = $("#personelModalBody");
    var modalInstance = null;

    function getModal() {
        if (!modalInstance && window.bootstrap) {
            modalInstance = bootstrap.Modal.getOrCreateInstance($modal[0]);
        }
        return modalInstance;
    }

    function showAlert(type, message) {
        var $container = $("#personelAjaxAlerts");
        $container.html(
            '<div class="alert alert-' + type + ' alert-dismissible fade show" role="alert">' +
            $("<div>").text(message).html() +
            '<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>'
        );
    }

    function setLoading() {
        $body.html('<div class="text-center py-4 text-muted">Yükleniyor...</div>');
    }

    function openPartial(title, url) {
        $title.text(title);
        setLoading();
        getModal().show();

        $.ajax({
            url: url,
            type: "GET",
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .done(function (html) {
                $body.html(html);
                var $form = $body.find("form");
                if ($form.length && $.validator && $.validator.unobtrusive) {
                    $form.removeData("validator").removeData("unobtrusiveValidation");
                    $.validator.unobtrusive.parse($form);
                }
            })
            .fail(function (xhr) {
                var msg = xhr.status === 403
                    ? "Bu işlem için yetkiniz yok."
                    : (xhr.responseText || "İçerik yüklenemedi.");
                $body.html('<div class="alert alert-danger mb-0"></div>').find(".alert").text(msg);
            });
    }

    function collectErrors(xhr) {
        if (xhr.responseJSON) {
            if (xhr.responseJSON.message) return xhr.responseJSON.message;
            if (xhr.responseJSON.errors) {
                var parts = [];
                $.each(xhr.responseJSON.errors, function (_, msgs) {
                    if ($.isArray(msgs)) parts = parts.concat(msgs);
                    else parts.push(msgs);
                });
                if (parts.length) return parts.join(" ");
            }
        }
        return "İşlem başarısız.";
    }

    function submitAjaxForm($form) {
        var $summary = $form.find("[data-valmsg-summary]");
        $summary.empty();

        $.ajax({
            url: $form.attr("action"),
            type: "POST",
            data: $form.serialize(),
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .done(function (result) {
                if (result && result.success) {
                    getModal().hide();
                    showAlert("success", result.message || "İşlem tamamlandı.");
                    window.setTimeout(function () {
                        window.location.reload();
                    }, 400);
                } else {
                    $summary.text((result && result.message) || "İşlem başarısız.");
                }
            })
            .fail(function (xhr) {
                $summary.text(collectErrors(xhr));
            });
    }

    $(function () {
        if (!$modal.length) return;

        $(document).on("click", ".js-personel-create", function (e) {
            e.preventDefault();
            openPartial("Yeni Personel", $(this).data("url"));
        });

        $(document).on("click", ".js-personel-details", function (e) {
            e.preventDefault();
            openPartial("Personel Detay", $(this).data("url"));
        });

        $(document).on("click", ".js-personel-edit", function (e) {
            e.preventDefault();
            openPartial("Personel Düzenle", $(this).data("url"));
        });

        $(document).on("click", ".js-personel-delete", function (e) {
            e.preventDefault();
            openPartial("Personel Sil", $(this).data("url"));
        });

        $(document).on("submit", "#personelModalForm, #personelDeleteForm", function (e) {
            e.preventDefault();
            var $form = $(this);
            if ($form.valid && !$form.valid()) return;
            submitAjaxForm($form);
        });
    });
})(jQuery);
