//https://notifyjs.jpillora.com/

function msg_success(msg, _elemetId = "", _position = "bottom") {
    if (_elemetId != null && _elemetId != "") {
        $(`#${_elemetId}`).notify(msg, { position: _position, className: "success" });
    }
    else {
        $.notify(msg, { position: _position, className: "success" });
    }
};

function msg_info(msg, _elemetId = "", _position = "bottom") {
    if (_elemetId != null && _elemetId != "") {
        $(`#${_elemetId}`).notify(msg, { position: _position, className: "info" });
    }
    else {
        $.notify(msg, { position: _position, className: "info" });
    }
};

function msg_warning(msg, _elemetId = "", _position = "bottom") {
    if (_elemetId != null && _elemetId != "") {
        $(`#${_elemetId}`).notify(msg, { position: _position, className: "warn" });
    }
    else {
        $.notify(msg, { position: _position, className: "warn" });
    }
};

function msg_error(msg, _elemetId = "", _position = "bottom") {
    if (_elemetId != null && _elemetId != "") {
        $(`#${_elemetId}`).notify(msg, { position: _position, className: "error", autoHideDelay: 10000, });
    }
    else {
        $.notify(msg, { position: _position, className: "error" });
    }
};

function success(msg) {
    msg_success(msg, _elemetId = "", _position = "top-right");
};
function error(msg) {
    msg_error(msg, _elemetId = "", _position = "top-right");
};
function warning(msg) {
    msg_warning(msg, _elemetId = "", _position = "top-right");
};
function cnoty(data, layout = "top-right") {
    $.notify(data[1], { position: layout, className: data[0] ? "success":"error" });
    return data[0];
}
