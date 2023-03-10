
LoadProvince();

function LoadProvince() {
    let _provinceSelect = $("#Province").val();

    $.ajax({
        method: "GET",
        url: "/admin/home/LoadProvince",
        dataType: "json"
    }).done(function (data) {
        if (data[0] == true) {
            let _opt = "<option value=''>-- Chọn Tỉnh/Thành phố --</option>";
            $.each(data[1], function (_index, _value) {
                _opt += `<option ${_value.provinceid == _provinceSelect ? "selected" : ""} value='${_value.provinceid}'>${_value.type} ${_value.name}</option>`;
            });
            $("#Province").html(_opt);

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

function LoadDistrict() {
    let _districceSelect = $("#District").val();

    let _province = $("#Province").val();
    if (_province == "") {
        $("#District").html("<option value=''>-- Chọn Quận/Huyện --</option>");
        $("#Ward").html("<option value=''>-- Chọn Xã/Phường --</option>");
    }
    else {
        $.ajax({
            method: "GET",
            url: "/admin/home/LoadDistrict",
            data: { province: _province},
            dataType: "json"
        }).done(function (data) {
            if (data[0] == true) {
                let _opt = "<option value=''>-- Chọn Quận/Huyện --</option>";
                $.each(data[1], function (_index, _value) {
                    _opt += `<option ${_value.districtid == _districceSelect ? "selected" : ""} value='${_value.districtid}'>${_value.type} ${_value.name}</option>`;
                });
                $("#District").html(_opt);

                LoadWard();
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

function LoadWard() {
    let _wardSelect = $("#Ward").val();

    let _district = $("#District").val();
    if (_district == "") {
        $("#Ward").html("<option value=''>-- Chọn Xã/Phường --</option>");
    }
    else {
        $.ajax({
            method: "GET",
            url: "/admin/home/LoadWard",
            data: { district: _district },
            dataType: "json"
        }).done(function (data) {
            if (data[0] == true) {
                let _opt = "<option value=''>-- Chọn Xã/Phường --</option>";
                $.each(data[1], function (_index, _value) {
                    _opt += `<option ${_value.wardid == _wardSelect ? "selected" : ""} value='${_value.wardid}'>${_value.type} ${_value.name}</option>`;
                });
                $("#Ward").html(_opt);
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