
LoadProvince2();

function LoadProvince2() {
    let _provinceSelect = $("#Province2").val();

    $.ajax({
        method: "GET",
        url: "/home/LoadProvince",
        dataType: "json"
    }).done(function (data) {
        if (data[0] == true) {
            let _opt = "<option value=''>-- Chọn Tỉnh/Thành phố --</option>";
            $.each(data[1], function (_index, _value) {
                _opt += `<option ${_value.provinceid == _provinceSelect ? "selected" : ""} value='${_value.provinceid}'>${_value.type} ${_value.name}</option>`;
            });
            $("#Province2").html(_opt);

            LoadDistrict();
        }
        else {
            msg_error(data[1], "", "top center");
        }
    }).fail(function () {
        msg_error("Đã có lỗi xảy ra", "", "top center");
    }).always(function () {
        //...
    });
};

function LoadDistrict2() {
    let _districceSelect = $("#District2").val();

    let _province = $("#Province2").val();
    if (_province == "") {
        $("#District2").html("<option value=''>-- Chọn Quận/Huyện --</option>");
        $("#Ward2").html("<option value=''>-- Chọn Xã/Phường --</option>");
    }
    else {
        $.ajax({
            method: "GET",
            url: "/home/LoadDistrict",
            data: { province: _province},
            dataType: "json"
        }).done(function (data) {
            if (data[0] == true) {
                let _opt = "<option value=''>-- Chọn Quận/Huyện --</option>";
                $.each(data[1], function (_index, _value) {
                    _opt += `<option ${_value.districtid == _districceSelect ? "selected" : ""} value='${_value.districtid}'>${_value.type} ${_value.name}</option>`;
                });
                $("#District2").html(_opt);

                LoadWard2();
            }
            else {
                msg_error(data[1], "", "top center");
            }
        }).fail(function () {
            msg_error("Đã có lỗi xảy ra", "", "top center");
        }).always(function () {
            //...
        });
    }
};

function LoadWard2() {
    let _wardSelect = $("#Ward2").val();

    let _district = $("#District2").val();
    if (_district == "") {
        $("#Ward2").html("<option value=''>-- Chọn Xã/Phường --</option>");
    }
    else {
        $.ajax({
            method: "GET",
            url: "/home/LoadWard",
            data: { district: _district },
            dataType: "json"
        }).done(function (data) {
            if (data[0] == true) {
                let _opt = "<option value=''>-- Chọn Xã/Phường --</option>";
                $.each(data[1], function (_index, _value) {
                    _opt += `<option ${_value.wardid == _wardSelect ? "selected" : ""} value='${_value.wardid}'>${_value.type} ${_value.name}</option>`;
                });
                $("#Ward2").html(_opt);
            }
            else {
                msg_error(data[1], "", "top center");
            }
        }).fail(function () {
            msg_error("Đã có lỗi xảy ra", "", "top center");
        }).always(function () {
            //...
        });
    }
};