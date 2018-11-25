$(document).ready(selectedPlayerImage());

function selectedPlayerImage() {
    var playersImage = $('#playersImage');
    playersImage.attr('src', '/Content/user.png');
    $('#homePlayersId').bind('focus', changeHomePreview(playersImage));
    $('#awayPlayersId').bind('focus', changeAwayPreview(playersImage));
}

function changeHomePreview(playersImage) {
    var playerName = $('#homePlayersId option:selected').text();
    $.ajax({
        url: "/Players/GetPlayerPath",
        method: "POST",
        data: { playerName: playerName },
        success: function (result) {
            playersImage.attr('src', result);
        },
        failure: function () {
            alert("failed");
        }
    })
}

function changeAwayPreview(playersImage) {

    var playerName = $('#awayPlayersId option:selected').text();
    $.ajax({
        url: "/Players/GetPlayerPath",
        method: "POST",
        data: { playerName: playerName },
        success: function (result) {
            playersImage.attr('src', result);
        },
        failure: function () {
            alert("failed");
        }
    })

}
        

