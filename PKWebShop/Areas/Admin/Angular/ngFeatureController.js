(function () {
    'use strict';

    angular
        .module('app', [])
        .controller('ngFeatureController', ngFeatureController)
        .controller('ngFeatureDetailController', ngFeatureDetailController);

    ngFeatureController.$inject = ['$scope', '$http'];
    ngFeatureDetailController.$inject = ['$scope', '$http'];

    function ngFeatureController($scope, $http) {
        $scope.title = 'ngFeatureController';

        activate();

        function activate() {
            //$scope.loading = true;
            //$http({
            //    method: "GET",
            //    url: "/admin/feature/getfeature"
            //    //params: { mn: 0 }
            //})
            //    .then(
            //        function (response) {
            //            $scope.secfeature = response.data;
            //            $scope.loading = false;

            //        }, function (response) {
            //            var optionsErr = $.parseJSON('{"text":"' + response.statusText +'","layout":"bottom","type":"error","timeout": "3000"}');
            //            noty(optionsErr);
            //            $scope.loading = false;
            //        });

        }


        /**
          * chon feature de sua
          * @param {any} code sectionfeature code
          */
        $scope.EditFeature_Show = function (code) {
            $('#myModal').modal('show');

            $scope.loading = true;
            $http({
                method: "GET",
                url: "/admin/feature/getfeaturebycode",
                params: { code: code }
            })
                .then(
                    function (response) {
                        $scope.SecFeature = response.data;
                        $scope.loading = false;
                        // alert($scope.SecFeature.title);
                    }, function (response) {

                        var optionsErr = $.parseJSON('{"text":"' + response.statusText + '","layout":"bottom","type":"error","timeout": "3000"}');
                        noty(optionsErr);
                        $scope.loading = false;

                    });

        };

        /**
         * sua section feature
         * */
        $scope.EditFeature_Submit = function () {

            if ($scope.SecFeature.desc === "") {
                $scope.require_sfdesc = true;
            }
            else {
                $scope.require_sfdesc = false;
            }

            if ($scope.require_sfdesc === false) {
                $scope.loading = true;

                $http({
                    method: "GET",
                    url: "/admin/feature/save",
                    params: { code: $scope.SecFeature.code, title: $scope.SecFeature.title, subtitle: $scope.SecFeature.subtitle, desc: $scope.SecFeature.desc, on_off: $scope.SecFeature.status }
                })
                    .then(
                        function (response) {
                            $scope.loading = false;
                            $("#" + $scope.SecFeature.code + "-title").html($scope.SecFeature.title);
                            $("#" + $scope.SecFeature.code + "-desc").html($scope.SecFeature.desc);
                            if ($scope.SecFeature.status === true) {
                                $("#" + $scope.SecFeature.code + "-status").html('<span class="label label-primary">On</span>');
                            }
                            else {
                                $("#" + $scope.SecFeature.code + "-status").html('<span class="label label-default">Off</span>');
                            }

                            var optionsNoty = $.parseJSON('{"text":"Lưu thành công","layout":"bottom","type":"success","timeout": "3000"}');
                            noty(optionsNoty);

                            $('#myModal').modal('hide');

                        }, function (response) {
                            var optionsErr = $.parseJSON('{"text":"' + response.statusText + '","layout":"bottom","type":"error","timeout": "3000"}');
                            noty(optionsErr);
                            $scope.loading = false;

                        });
            }



        };





    }




    function ngFeatureDetailController($scope, $http) {
        $scope.title = 'ngFeatureDetailController';

        activate();

        function activate() {
            var _code = $("select[name=secfeature] option:selected").val();

            $scope.loading = true;
            $http({
                method: "GET",
                url: "/admin/feature/GetSFDByCode",
                params: { code: _code }
            })
                .then(
                    function (response) {
                        $scope.sfd_list = response.data;
                        $scope.loading = false;

                    }, function (response) {
                        var optionsErr = $.parseJSON('{"text":"' + response.statusText + '","layout":"bottom","type":"error","timeout": "3000"}');
                        noty(optionsErr);
                        $scope.loading = false;
                    });

        }


        $scope.ChangesSectionFeature = function() {

            activate();
           
        };

        /**
          * chon feature de sua
          * @param {any} sfdObj sfd obj
          */
        $scope.SaveFeatureDetail_Show = function (sfdObj) {
            debugger;
            $('#myModal').modal('show');
            if (!sfdObj) {
                $scope.addnew = true;
            }
            else {
                $scope.addnew = false;
            }
            if ($scope.addnew === true) {
                var sfd = {
                    "seccode": $("select[name=secfeature]").val(),
                    "secname": $("select[name=secfeature] option:selected").text(),
                    "id": "",
                    "title": "",
                    "url": "",
                    "pic": "/Content/admin/img/no_image.jpg",
                    "volume": ""
                };
                $scope.sfd = sfd;
                console.log($scope.sfd);
            }
            else {

                $scope.sfd = sfdObj;
                $("#hdPicture").val($scope.sfd.pic);
                //CKEDITOR.instances['ck-editor'].setData(sfdObj.desc);
            }


        };

        /**
         * sua section feature
         * */
        $scope.SaveFeatureDetail_Submit = function () {
            $scope.save_loading = true;

            //alert("js:" + $("#hdPicture").val());
            //alert("ng:" + $scope.sfd.pic);
            //ly do: khong the nhan dang sfd.pic khi dung ham javascript xu ly ckfinder
            //de load url picture vao sfd.pic.
            $scope.sfd.pic = $("#hdPicture").val();
            $scope.sfd.desc = CKEDITOR.instances["ck-editor"].getData();

            $http({
                method: "post",
                url: "/admin/feature/savedetail",
                data: $scope.sfd
            })
                .then(
                    function (response) {
                        $scope.save_loading = false;

                        if ($scope.sfd.id === "") {
                            //them moi
                            $scope.sfd_list = response.data;
                        }
                        else {
                            //sua noi dung
                            $scope.sfd_list = response.data;
                        }



                        var optionsNoty = $.parseJSON('{"text":"Lưu thành công","layout":"bottom","type":"success","timeout": "3000"}');
                        noty(optionsNoty);

                        $('#myModal').modal('hide');

                    }, function (response) {
                        var optionsErr = $.parseJSON('{"text":"' + response.statusText + '","layout":"bottom","type":"error","timeout": "3000"}');
                        noty(optionsErr);
                        $scope.save_loading = false;

                    });

        };

        /**
         * delete chuc nang
         * @param {any} id
         */
        $scope.DeleteFeatureDetail = function (id) {
            if (confirm('Xác nhận bạn muốn xóa?')) {
                $http({
                    method: "GET",
                    url: "/admin/feature/DeleteDetail",
                    params: { id: id }
                })
                    .then(
                        function (response) {
                            $("#panel_" + id).hide(500);

                            var optionsNoty = $.parseJSON('{"text":"Xóa thành công","layout":"bottom","type":"success","timeout": "3000"}');
                            noty(optionsNoty);

                        }, function (response) {
                            var optionsErr = $.parseJSON('{"text":"' + response.statusText + '","layout":"bottom","type":"error","timeout": "3000"}');
                            noty(optionsErr);
                            $scope.loading = false;
                        });
            }
            
        };



    }


    
})();

