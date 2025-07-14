// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$('.btn-add').on('click', function (e) {
    e.preventDefault();
    $.post({
        url: '/Home/AddToList',
        data: $(this).closest('form').serialize(),
        success: function (resp) {
            if (resp.success) {
                $(this).text('✓ In Your List');
            } else {
                alert(resp.message);
            }
        }
    });
});