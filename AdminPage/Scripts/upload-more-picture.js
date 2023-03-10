
var img_default = "/Content/admin/img/no_image.jpg";
//[CKFinder] - function xu ly upload/change/delete hinh anh va video
function InitCKFinder() {
    let finder = new CKFinder();
    finder.BasePath = '/Content/admin/ckfinder/';
    finder.SelectFunction = this.SetFileField;
    finder.Popup();
};

function uploadpic(el) {
    InitCKFinder();
    let id_targer = el.prop("id");
    if (id_targer.search("_") != -1) {
        id_targer = id_targer.split("_")[1];
    }
    sessionStorage.setItem("IdTarget", `${id_targer}`);
};

function delpic(el) {
    let id_targer = el.prop("id");
    if (id_targer.search("_") != -1) {
        id_targer = id_targer.split("_")[1];
    }
    $(`#span_${id_targer}`).html(img_default.split("/").pop());
    $(`#img_${id_targer}`).prop("src", img_default);
    $(`#input_${id_targer}`).val(img_default).trigger('change');
};

function upload_more_pic() {
    InitCKFinder();
    sessionStorage.setItem("AddMorePic", "true");
};

function del_more_pic(el) {
    $(el).parent().remove();
    if (typeof del_more_pic_done !== "undefined") {
        del_more_pic_done();
    }
};

function SetFileField(fileUrl, data) {
    if (fileUrl.search(".mp4") != -1) {
        alert("Vui lòng chọn hình ảnh");
    }
    else {
        let id_targer = sessionStorage.getItem("IdTarget");
        if (id_targer != null && id_targer != "") {
            $(`#span_${id_targer}`).html(fileUrl.split("/").pop());
            $(`#img_${id_targer}`).prop("src", fileUrl);
            $(`#input_${id_targer}`).val(fileUrl).trigger('change');
            sessionStorage.setItem("IdTarget", "");
        }

        let new_id = sessionStorage.getItem("AddMorePic");
        if (new_id == "true") {


            let new_pic = `<p class="img_more_p">
                <img src="${fileUrl}" alt="review picture" />
                <button type="button" class="btn btn-danger btn-xs" onclick="del_more_pic($(this))">
                    <i class="anticon anticon-close"></i>
                </button>
                <input type="hidden" name="picmore" value="${fileUrl}"/>
             </p>`;
            $("#img_more").append(new_pic);
            $("#clearboth").html('<div class="clearfix"></div>');
            sessionStorage.setItem("AddMorePic", "");
        }
        if (typeof upload_more_pic_done !== "undefined") {
            upload_more_pic_done();
        }
    }
};