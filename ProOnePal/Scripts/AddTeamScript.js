$(document).ready(selectedImage());

function selectedImage() {
    var teamsImage = document.getElementById("teamsImage");
    teamsImage.setAttribute('src', "/Content/imgs/FCB.png");
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