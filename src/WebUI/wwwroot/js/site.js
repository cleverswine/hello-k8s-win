"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/statusHub").build();

function checknull(s, d) {
    if (s == null || s == "") return d
    return s
}

connection.on("StatusUpdate", function (status) {
    if ($("#tr" + status.id).length) {
        $("#" + status.id + "worker").text(checknull(status.worker, "[in queue]"));
        $("#" + status.id + "statusbar").css('width', status.status + '%').attr('aria-valuenow', status.status);
        $("#" + status.id + "statusbar").text(status.status + "%");
        if (status.status == 100) {
            $("#" + status.id + "statusbar").removeClass("progress-bar-striped");
            $("#" + status.id + "statusbar").removeClass("progress-bar-animated");
            $("#" + status.id + "statusbar").addClass("bg-success");
        }
    } else {
        var statusMarkup = "<div class='progress'>";
        statusMarkup += "<div id='" + status.id + "statusbar' class='progress-bar progress-bar-striped progress-bar-animated' role='progressbar' aria-valuenow='" + status.status + "' aria-valuemin='0' aria-valuemax='100' style='width: 0%;'>";
        statusMarkup += status.status + "%</div></div>";
        var markup = "<tr id='tr" + status.id + "'>";
        markup += "<td id='" + status.id + "id'>" + status.id + "</td>";
        markup += "<td id='" + status.id + "worker'>" + checknull(status.worker, "[in queue]") + "</td>";
        markup += "<td id='" + status.id + "status'>" + statusMarkup + "</td>";
        markup += "</tr>";
        $("#statusTable").append(markup);
    }
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event) {
    var message = document.getElementById("messageInput").value;
    if (message == "") return;
    $.ajax({
        type: "POST",
        url: "/api/calc",
        data: JSON.stringify({ id: 0, input: message }),
        dataType: "json",
        contentType: "application/json"
    });
    event.preventDefault();
});