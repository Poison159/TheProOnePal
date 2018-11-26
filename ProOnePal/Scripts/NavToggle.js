$(document).ready(function () {
    $('li').hover(function () {
        $(this).find('ul > li').fadeToggle(2000);
    }); 
});