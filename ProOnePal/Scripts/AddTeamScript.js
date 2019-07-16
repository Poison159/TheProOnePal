$(document).ready(selectedImage());

function selectedImage() {
    var teamsImage = document.getElementById("teamsImage");
    startImage();
    $('#teamId').bind('change', function () {
        var teamName = $('#teamId option:selected').text();
        $.ajax({
            url: "/Teams/GetTeamPath",
            method: "POST",
            data: { teamName: teamName },
            success: function (result) {
                teamsImage.setAttribute('src', result);
            },
            failure: function () {
                alert("failed");
            }
        });
    });
}

function startImage() {

    var teamName = $('#teamId option:selected').text();
    $.ajax({
        url: "/Teams/GetTeamPath",
        method: "POST",
        data: { teamName: teamName },
        success: function (result) {
            teamsImage.setAttribute('src', result);
        },
        failure: function () {
            alert("failed");
        }
    });

}
