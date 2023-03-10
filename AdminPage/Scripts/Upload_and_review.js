/*
HTML REVIEW IMAGE

 <div style="padding-left:0px;display:none" class="col-md-12" id="review_pic0">
    <p class="col-md-6" style="border:1px dotted red; padding:3px">
        <img id="img_pic0" style="height:100px;margin-left:0" alt="xem trước" />
        <i style="padding-left:5px" id="fname_pic0"></i>
        <a onclick="upload_delete('pic0')" href="#" data-toggle="tooltip" class="pull-right" title="Xóa file này">
            <i class="glyphicon glyphicon-trash"></i>
        </a>
    </p>

</div>

*/

/**
 * Xem truoc image duoc chon
 * @param {any} input: doi tuong input file
 */
function reviewUpload(input) {

    if (input.files && input.files[0]) {

        var inputid = $(input).attr("id");

        var reader = new FileReader();

        reader.onload = function (e) {

            var fname = $(input).val().split('\\').pop();
            var extfile = fname.substring(fname.lastIndexOf(".") + 1);
            var strCheck = new RegExp(/jpg|png|jpeg|gif/igm);
            var imgSrc = e.target.result;

            if (strCheck.test(extfile) == false) {
                //default image                             
                imgSrc = "/Content/admin/img/no_image.jpg";
            }
            $("#img_" + inputid).attr("src", imgSrc);
            $("#fname_" + inputid).html(fname);
            $("#review_" + inputid).show();
            
            if ($("#rv_" + inputid) != null) {
                $("#rv_" + inputid).hide();
            }
        }
        reader.readAsDataURL(input.files[0]);
    }
    else {
        $("#img_" + inputid).attr("src", "");
        $("#fname_" + inputid).html("");
        $("#review_" + inputid).hide();
    }
    
}

/**
 * huy bo file da chon
 * @param {any} inputid : id cua input file
 */
function upload_delete(inputid) {
    $("#" + inputid).val("");
    $("#img_" + inputid).attr("src", "");
    $("#review_" + inputid).hide();
}

/**
 * xoa tap tin tu database
 * @param {any} id : uploadid
 */
function fdelete(id) {
    if (confirm("Xác nhận muốn xóa tập tin này?")) {
        $("#floading_" + id).show();
        var obj = {};
        obj.url = "/upload/morefiledelete/";
        obj.type = "post";
        obj.datatype = "json"
        obj.contenttype = "application/json";
        obj.data = { "id": id };
        obj.success = function (data) {
            if (data == true) {
                $("#f_" + id).hide(500);
                success("Đã xóa hình ảnh");
            }
        };
        obj.error = function () { success("Không thể xóa hình ảnh"); $("#floading_" + id).hide(); };
        obj.complete = function () { $("#floading_" + id).hide(); };
        jQuery.ajax(obj);
    }

}



