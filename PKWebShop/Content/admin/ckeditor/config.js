/**
 * @license Copyright (c) 2003-2017, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.md or http://ckeditor.com/license
 */
CKEDITOR.stylesSet.add('my_styles', [
    // Block-level styles
    { name: 'Tiêu đề', element: 'h2', styles: { "font-weight": 700, "padding": "12px 20px", "border": "1px solid #e5e5e5", "border-top": "2px solid #058005 !important","border-radius": "4px", "font-size": "14px", "text-transform": "uppercase" }},

]);
CKEDITOR.editorConfig = function (config) {
    // Define changes to default configuration here. For example:
    // config.language = 'fr';
    // config.uiColor = '#AADC6E';
    config.language = 'vi';
    config.height = 500;
    config.toolbarCanCollapse = true;

    //config.enterMode = CKEDITOR.ENTER_BR;
    //config.shiftEnterMode = CKEDITOR.ENTER_P


    config.filebrowserBrowseUrl = '/Content/admin/ckfinder/ckfinder.html';
    config.filebrowserUploadUrl = '/Content/admin/ckfinder/core/connector/aspx/connector.aspx?command=QuickUpload&type=Files';

    config.filebrowserImageUrl = '/Content/admin/ckfinder/ckfinder.html?type=Images';
    config.filebrowserImageUploadUrl = '/Content/admin/ckfinder/core/connector/aspx/connector.aspx?command=QuickUpload&type=Images';

    config.filebrowserFlashUrl = '/Content/admin/ckfinder/ckfinder.html?type=Flash';
    config.filebrowserFlashUploadUrl = '/Content/admin/ckfinder/core/connector/aspx/connector.aspx?command=QuickUpload&type=Flash';

    config.extraPlugins = 'youtube';
    config.stylesSet = 'my_styles';


    //config.youtube_responsive = true;
    //config.youtube_width = '640';
    //config.youtube_height = '480';
};