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

// Highlight active navigation item based on current path
$(document).ready(function () {
    var currentPath = window.location.pathname.toLowerCase();
    $('.navbar-nav .nav-link').each(function () {
        var linkPath = $(this).attr('href').toLowerCase();
        if (currentPath === linkPath || currentPath.startsWith(linkPath + '/')) {
            $('.navbar-nav .nav-link').removeClass('active');
            $(this).addClass('active');
            return false; // Stop after first match
        }
    });
});