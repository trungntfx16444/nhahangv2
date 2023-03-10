//======PRODUCT PROPERTY
var _action = "";
function GetProperty(el) {
    _action = el.data("action");
    if (_action == "add") {
        $("#modalProperty-title").html("Thêm thuộc tính");
        $("#modalProperty input").not("#langCode").val("");
        $("#modalProperty .div_new_value").show();
        $("#modalProperty").modal("show");
    }
    else if (_action == "edit") {
        let propId = $("#list_parent_prop").val();
        if (propId == "") {
            msg_info("Chọn thuộc tính để chỉnh sửa", "list_parent_prop");
        }
        else {
            $.ajax({
                method: "POST",
                url: "/productman/GetProperty",
                data: {
                    propertyReId: propId,
                    langCode: $("#langCode").val()
                },
                dataType: "json"
            }).done(function (data) {
                if (data[0]) {
                    $("#modalProperty-title").html("Chỉnh sửa thuộc tính");
                    $("#modalProperty input[name='properti_ReId']").val(data[1].ReId);
                    $("#modalProperty input[name='properti_name']").val(data[1].name);
                    $("#modalProperty .div_new_value").hide();
                    $("#modalProperty").modal("show");
                }
                else {
                    msg_error(data[1], "", "top-center");
                }
            }).fail(function () {
                msg_error("Đã có sự cố xảy ra. Vui lòng thử lại", "", "top-center");
            });
        }
    }
};

function SaveProperty() {
    $.ajax({
        method: "POST",
        url: "/productman/SaveProperty",
        data: $("#frm_property").serialize(),
        dataType: "json"
    }).done(function (data) {
        if (data[0]) {
            if (data[1] != null) {
                if (_action == "add") {
                    $("#list_parent_prop").append(`<option value='${data[1].ReId}' selected>${data[1].name}</option>`);
                    let multiSelect = $('#list_prop');
                    multiSelect.empty();
                    if (data[2] != null) {
                        let opt = new Option(data[2].name, data[2].ReId, false, true);
                        multiSelect.append(opt).selectpicker('refresh');
                    }
                    multiSelect.selectpicker('refresh');
                    msg_success("Thêm thành công", "", "top-right");
                }
                else {
                    $(`#list_parent_prop option[value="${data[1].id}"]`).text(data[1].name);
                    msg_success("Lưu thành công", "", "top-right");
                }
            }
            $("#modalProperty").modal("hide");
        }
        else {
            msg_error(data[1], "", "top-center");
        }
    }).fail(function () {
        msg_error("Đã có sự cố xảy ra. Vui lòng thử lại", "", "top-center");
    });
};

function GetValue(el) {
    _action = el.data("action");
    let propId = $("#list_parent_prop").val();
    if (propId == "") {
        msg_info(`Chọn thuộc tính để ${_action == "add" ? "thêm" : "chỉnh sửa"} giá trị`, "list_parent_prop");
        return false;
    }

    if (_action == "add") {
        $("#modalValue-title").html("Thêm giá trị");
        $("#modalValue input[name='properti_ReId']").val(propId);
        $("#modalValue input[name='value_name']").val("");
        $("#modalValue .div_select_value").hide();
        $("#modalValue select[name='value_id']").html("<option value=''>-- Chọn giá trị --</option>");
        $("#modalValue").modal("show");
    }
    else {
        $.ajax({
            method: "POST",
            url: "/productman/GetValue",
            data: {
                propertyReId: propId,
                langCode: $("#langCode").val()
            },
            dataType: "json"
        }).done(function (data) {
            if (data[0]) {
                $("#modalValue-title").html("Chỉnh sửa giá trị");
                $("#modalValue input[name='properti_ReId']").val(propId);
                $("#modalValue input[name='value_name']").val("");
                $("#modalValue .div_select_value").show();

                let opt = "<option value='-1'>-- Chọn giá trị --</option>";
                $.each(data[1], function (_index, _value) {
                    opt += `<option value='${_value.id}'>${_value.name}</option>`;
                })
                $("#modalValue select[name='value_id']").html(opt);
                $("#modalValue").modal("show");
            }
            else {
                msg_error(data[1], "", "top-center");
            }
        }).fail(function () {
            msg_error("Đã có sự cố xảy ra. Vui lòng thử lại", "", "top-center");
        });
    }
};

function SaveValue() {
    if ($("#modalValue select[name='value_id']").val() == "-1") {
        msg_error("Vui lòng chọn giá trị cần chỉnh sửa", "", "top-center");
        return false;
    }

    $.ajax({
        method: "POST",
        url: "/productman/SaveValue",
        data: $("#frm_value").serialize() + `&langCode=${$("#langCode").val()}`,
        dataType: "json"
    }).done(function (data) {
        if (data[0]) {
            if (data[1] != null) {
                if (_action == "add") {
                    msg_success("Thêm thành công", "", "top-right");
                }
                else {
                    msg_success("Lưu thành công", "", "top-right");
                }
                LoadValue($("#list_parent_prop"));
            }
            $("#modalValue").modal("hide");
        }
        else {
            msg_error(data[1], "", "top-center");
        }
    }).fail(function () {
        msg_error("Đã có sự cố xảy ra. Vui lòng thử lại", "", "top-center");
    });
};

function LoadValue(el) {
    let multiSelect = $('#list_prop');
    if (el.val() == "") {
        multiSelect.empty();
        multiSelect.selectpicker('refresh');
    }
    else {
        $.ajax({
            method: "POST",
            url: "/productman/GetValue",
            data: {
                propertyReId: el.val(),
                langCode: $("#langCode").val()
            },
            dataType: "json"
        }).done(function (data) {
            if (data[0]) {
                multiSelect.empty();
                $.each(data[1], function (_index, _value) {
                    let opt = new Option(_value.name, _value.ReId, false, false);
                    multiSelect.append(opt);
                })
                multiSelect.selectpicker('refresh');
            }
            else {
                msg_error(data[1], "", "top-center");
            }
        }).fail(function () {
            msg_error("Đã có sự cố xảy ra. Vui lòng thử lại", "", "top-center");
        });
    }
};

function DelProperty(el) {
    let idDel = [];
    let isDel = false;

    let typeDel = el.data("action");
    if (typeDel == "property") {
        let id = $('#list_parent_prop').val();
        if (id == "") {
            msg_info("Chọn thuộc tính để xóa", "list_parent_prop");
            return false;
        }

        if (confirm(`Bạn có chắc chắn muốn xóa thuộc tính ${$('#list_parent_prop option:selected').text()}`)) {
            idDel.push(id);
            isDel = true;
        }
    }
    else {
        idDel = $('#list_prop').val();
        if (idDel.length <= 0) {
            msg_info("Chọn giá trị để xóa", "list_prop");
            return false;
        }

        let listText = $('#list_prop option:selected').toArray().map(item => item.text).join(", ");
        if (confirm(`Bạn có chắc chắn muốn xóa giá trị ${listText}`)) {
            isDel = true;
        }
    }

    if (isDel == true) {
        ActionDelete(typeDel, idDel);
    }
};

function ActionDelete(typeDel, idDel) {
    $.ajax({
        method: "POST",
        url: "/productman/DelProperty",
        data: { propertyReId: idDel },
        dataType: "json"
    }).done(function (data) {
        if (data[0]) {
            let _select = $("#list_parent_prop");
            if (typeDel == "property") {
                _select.val("");
                _select.find(`option[value="${idDel.toString()}"]`).remove();
            }
            LoadValue(_select);
            msg_success(data[1], "", "top-center");
        }
        else {
            msg_error(data[1], "", "top-center");
        }
    }).fail(function () {
        msg_error("Đã có sự cố xảy ra. Vui lòng thử lại", "", "top-center");
    });
};

function LoadProductProperties() {
    let parent_prop = $("#list_parent_properties").val();
    let prop = $("#list_properties").val();

    if (parent_prop != "" && prop != "") {
        $.ajax({
            method: "POST",
            url: "/productman/LoadProductProperties",
            data: {
                parent_properties: parent_prop.split(","),
                properties: prop.split(","),
                langCode: $("#langCode").val()
            },
            dataType: "html"
        }).done(function (data) {
            $("#div_product_prop").html(data);
            let err = $("#PropErr").val();
            if (err != "") {
                msg_error(err, "", "top-center");
            }
            else {
                let _select = $("#list_parent_prop");
                _select.val("");
                $.each(parent_prop.split(","), function (_index, _value) {
                    if (_value != "") {
                        _select.find(`option[value="${_value}"]`).remove();
                    }
                })
                LoadValue(_select);
            }
        }).fail(function () {
            msg_error("Đã có sự cố xảy ra. Vui lòng thử lại", "", "top-center");
        });
    }
};

function SelectProperty() {
    let _parent_prop = $("#list_parent_prop").val();
    let _prop = $('#list_prop').val();
    if (_parent_prop == "") {
        msg_info("Vui lòng chọn thuộc tính", "list_parent_prop");
        return false;
    }
    else if (_prop.length <= 0) {
        msg_info("Vui lòng chọn giá trị", "list_prop");
        return false;
    }

    $("#list_parent_properties").val(`${$("#list_parent_properties").val()},${_parent_prop}`);
    $("#list_properties").val(`${$("#list_properties").val()},${_prop.toString()}`);
    LoadProductProperties();
};

function RemoveProperty(parentId, el) {
    let idRemove = el.parent().data("remove");
    let _newParentId = $("#list_parent_properties").val().replace(`,${parentId}`, "");
    let _newChildId = $("#list_properties").val().replace(`,${idRemove}`, "");

    $("#list_parent_properties").val(_newParentId);
    $("#list_properties").val(_newChildId);
    $(`#f_${parentId}`).remove();

    let _select = $("#list_parent_prop");
    _select.append(new Option(el.data("prname"), parentId, false, false));
    _select.val("");
    LoadValue(_select);
    $("div.tooltip").remove();
};