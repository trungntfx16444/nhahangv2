function StringToSlug(str) {
    // remove accents
    var from = ["[àáãảạăằắẳẵặâầấẩẫậ]", "[èéẻẽẹêềếểễệ]", "đ", "[ùúủũụưừứửữự]", "[òóỏõọôồốổỗộơờớởỡợ]", "[ìíỉĩị]", "[ýỳỹỵỷ]"],
        to = ["a", "e", "d", "u", "o", "i", "y"];
    for (var i = 0, l = from.length; i < l; i++) {
        str = str.replace(RegExp(from[i], "gi"), to[i]);
    }

    str = str.toLowerCase()
        .trim()
        .replace(/[^a-z0-9\-]/g, '-')
        .replace(/-+/g, '-');

    return str;
}

$(function () {
    $('.header .nav-left .desktop-toggle').on('click', function () {
        $.cookie("is-folded", $("body > div.app").hasClass("is-folded"));
        setTimeout(function () {
            diskchart_load();
        }, 200);
       
    })
    if ($.cookie("is-folded") == "true") {
        $(".app").addClass("is-folded");
    }
})